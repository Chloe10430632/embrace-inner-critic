using System.Security.Claims;
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
[Route("api/strength-entries")]
public sealed class StrengthEntriesController(ApplicationDbContext database) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException();

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        if (page is < 1 or > 10 || pageSize is < 1 or > 50) return BadRequest();
        var entries = await database.StrengthEntries.AsNoTracking().Where(entry => entry.UserId == UserId)
            .OrderByDescending(entry => entry.CreatedAt).ThenByDescending(entry => entry.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(HttpContext.RequestAborted);
        return Ok(entries.Select(ToResponse));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var entry = await Find(id);
        return entry is null ? NotFound() : Ok(ToResponse(entry));
    }

    [HttpPost]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> Create(UpsertStrengthRequest request)
    {
        if (!Valid(request)) return BadRequest(new { error = "請填寫 1 至 2000 字的事件；補充最多 4000 字、標籤最多 10 個，每個 1 至 24 字。" });
        await using var transaction = await database.Database.BeginTransactionAsync();
        var reserved = await database.Users.Where(user => user.Id == UserId && user.StrengthEntryCount < 500)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.StrengthEntryCount, user => user.StrengthEntryCount + 1));
        if (reserved == 0) return StatusCode(429, new { error = "每個帳號最多可儲存 500 筆力量紀錄。" });
        var entry = new StrengthEntry { Id = Guid.NewGuid(), UserId = UserId, CreatedAt = DateTimeOffset.UtcNow };
        Apply(entry, request);
        database.StrengthEntries.Add(entry);
        await database.SaveChangesAsync();
        await transaction.CommitAsync();
        return CreatedAtAction(nameof(Get), new { id = entry.Id }, ToResponse(entry));
    }

    [HttpPut("{id:guid}")]
    [RequestSizeLimit(32 * 1024)]
    public async Task<IActionResult> Update(Guid id, UpsertStrengthRequest request)
    {
        if (!Valid(request)) return BadRequest(new { error = "事件或標籤格式不正確，請檢查字數與標籤數量。" });
        var entry = await Find(id);
        if (entry is null) return NotFound();
        Apply(entry, request);
        await database.SaveChangesAsync();
        return Ok(ToResponse(entry));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await using var transaction = await database.Database.BeginTransactionAsync();
        // Reserve the deletion in the database so simultaneous requests decrement the quota only once.
        var deleted = await database.StrengthEntries.Where(entry => entry.Id == id && entry.UserId == UserId).ExecuteDeleteAsync();
        if (deleted == 0) return NotFound();
        await database.Users.Where(user => user.Id == UserId && user.StrengthEntryCount > 0)
            .ExecuteUpdateAsync(setters => setters.SetProperty(user => user.StrengthEntryCount, user => user.StrengthEntryCount - 1));
        await transaction.CommitAsync();
        return NoContent();
    }

    private Task<StrengthEntry?> Find(Guid id) => database.StrengthEntries.SingleOrDefaultAsync(
        entry => entry.Id == id && entry.UserId == UserId, HttpContext.RequestAborted);
    private static bool Valid(UpsertStrengthRequest request) =>
        request.Event?.Trim().Length is >= 1 and <= 2000 && (request.Notes?.Length ?? 0) <= 4000
        && (request.Tags?.Length ?? 0) <= 10
        && (request.Tags is null || request.Tags.All(tag => tag?.Trim().Length is >= 1 and <= 24));
    private static void Apply(StrengthEntry entry, UpsertStrengthRequest request)
    {
        entry.Event = request.Event!.Trim();
        entry.Notes = request.Notes?.Trim() ?? "";
        entry.Tags = request.Tags?.Select(tag => tag!.Trim()).Distinct().ToArray() ?? [];
        entry.UpdatedAt = DateTimeOffset.UtcNow;
    }
    private static StrengthResponse ToResponse(StrengthEntry entry) =>
        new(entry.Id, entry.Event, entry.Notes, entry.Tags, entry.CreatedAt, entry.UpdatedAt);
}
