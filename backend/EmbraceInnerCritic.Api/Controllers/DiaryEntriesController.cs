using System.Security.Claims;
using System.Text.Json;
using EmbraceInnerCritic.Api.Contracts;
using EmbraceInnerCritic.Api.Data;
using EmbraceInnerCritic.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmbraceInnerCritic.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/diary-entries")]
public sealed class DiaryEntriesController(ApplicationDbContext database) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DiaryResponse>>> List()
    {
        var userId = CurrentUserId();
        var entries = await database.DiaryEntries
            .Where(entry => entry.UserId == userId)
            .OrderByDescending(entry => entry.UpdatedAt)
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
    public async Task<ActionResult<DiaryResponse>> Create(UpsertDiaryRequest request)
    {
        if (!IsValid(request, out var error))
        {
            return BadRequest(new { error });
        }

        var userId = CurrentUserId();
        var userExists = await database.Users.AnyAsync(user => user.Id == userId);
        if (!userExists)
        {
            return Unauthorized();
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

        return CreatedAtAction(nameof(Get), new { id = entry.Id }, ToResponse(entry));
    }

    [HttpPut("{id:guid}")]
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

        database.DiaryEntries.Remove(entry);
        await database.SaveChangesAsync();
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
