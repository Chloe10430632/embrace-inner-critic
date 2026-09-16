using Microsoft.AspNetCore.Identity;

namespace EmbraceInnerCritic.Api.Models;

public sealed class ApplicationUser : IdentityUser
{
    public string? CriticName { get; set; }
    public int DiaryEntryCount { get; set; }
    public ICollection<DiaryEntry> DiaryEntries { get; } = new List<DiaryEntry>();
}
