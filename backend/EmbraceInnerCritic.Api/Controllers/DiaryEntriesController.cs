using System.Security.Claims;
using System.Text.Json;
using EmbraceInnerCritic.Api.Contracts;
using EmbraceInnerCritic.Api.Data;
using EmbraceInnerCritic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace EmbraceInnerCritic.Api.Controllers;

[ApiController]
[Authorize]
[EnableRateLimiting("diary")]
[Route("api/diary-entries")]
public sealed class DiaryEntriesController(ApplicationDbContext database) : ControllerBase
{
    private const int MaxRequestBodyBytes = 64 * 1024;
    private const int MaxAnswersBytes = 32 * 1024;
    private const int MaxEntriesPerUser = 500;
    private const int DefaultPageSize = 50;
    private const int MaxPageSize = 50;
    private const int MaxPageNumber = 1_000;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DiaryResponse>>> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize)
    {
        if (page is < 1 or > MaxPageNumber || pageSize is < 1 or > MaxPageSize)
        {
            return BadRequest(new { error = $"page 需介於 1 至 {MaxPageNumber}，pageSize 需介於 1 至 {MaxPageSize}。" });
        }

        var userId = CurrentUserId();
        var entries = await database.DiaryEntries
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return Ok(entries.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DiaryResponse>> Get(Guid id)
    {
        var entry = await FindOwnedEntry(id);
        return entry is null ? NotFound() : Ok(ToResponse(entry));
    }

    [HttpPost]
    [RequestSizeLimit(MaxRequestBodyBytes)]
    public async Task<ActionResult<DiaryResponse>> Create(UpsertDiaryRequest request)
    {
        if (!IsValid(request, out var error))
        {
            return BadRequest(new { error });
        }

        var userId = CurrentUserId();
        await using var transaction = await database.Database.BeginTransactionAsync();
        var quotaReserved = await database.Users
            .Where(user => user.Id == userId && user.DiaryEntryCount < MaxEntriesPerUser)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.DiaryEntryCount, user => user.DiaryEntryCount + 1));
        if (quotaReserved == 0)
        {
            return StatusCode(
                StatusCodes.Status429TooManyRequests,
                new { error = $"每個帳號最多可儲存 {MaxEntriesPerUser} 篇日記。" });
        }

        var now = DateTimeOffset.UtcNow;
        var entry = new DiaryEntry
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CriticName = request.CriticName.Trim(),
            AnswersJson = request.Answers.GetRawText(),
            IsComplete = request.IsComplete,
            CreatedAt = now,
            UpdatedAt = now
        };
        database.DiaryEntries.Add(entry);
        await database.SaveChangesAsync();
        await transaction.CommitAsync();

        return CreatedAtAction(nameof(Get), new { id = entry.Id }, ToResponse(entry));
    }

    [HttpPut("{id:guid}")]
    [RequestSizeLimit(MaxRequestBodyBytes)]
    public async Task<ActionResult<DiaryResponse>> Update(Guid id, UpsertDiaryRequest request)
    {
        if (!IsValid(request, out var error))
        {
            return BadRequest(new { error });
        }

        var entry = await FindOwnedEntry(id);
        if (entry is null)
        {
            return NotFound();
        }

        entry.CriticName = request.CriticName.Trim();
        entry.AnswersJson = request.Answers.GetRawText();
        entry.IsComplete = request.IsComplete;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        await database.SaveChangesAsync();

        return Ok(ToResponse(entry));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var entry = await FindOwnedEntry(id);
        if (entry is null)
        {
            return NotFound();
        }

        await using var transaction = await database.Database.BeginTransactionAsync();
        database.DiaryEntries.Remove(entry);
        await database.SaveChangesAsync();
        await database.Users
            .Where(user => user.Id == entry.UserId && user.DiaryEntryCount > 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(user => user.DiaryEntryCount, user => user.DiaryEntryCount - 1));
        await transaction.CommitAsync();
        return NoContent();
    }

    private string CurrentUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    private Task<DiaryEntry?> FindOwnedEntry(Guid id) =>
        database.DiaryEntries.SingleOrDefaultAsync(entry =>
            entry.Id == id && entry.UserId == CurrentUserId());

    private static bool IsValid(UpsertDiaryRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.CriticName) || request.CriticName.Length > 64)
        {
            error = "批評者名稱需介於 1 至 64 個字元。";
            return false;
        }

        if (request.Answers.ValueKind != JsonValueKind.Object)
        {
            error = "日記內容格式不正確。";
            return false;
        }

        if (System.Text.Encoding.UTF8.GetByteCount(request.Answers.GetRawText()) > MaxAnswersBytes)
        {
            error = $"日記內容不可超過 {MaxAnswersBytes / 1024} KB。";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static DiaryResponse ToResponse(DiaryEntry entry)
    {
        using var document = JsonDocument.Parse(entry.AnswersJson);
        return new DiaryResponse(
            entry.Id,
            entry.CreatedAt,
            entry.UpdatedAt,
            entry.CriticName,
            document.RootElement.Clone(),
            entry.IsComplete);
    }
}
