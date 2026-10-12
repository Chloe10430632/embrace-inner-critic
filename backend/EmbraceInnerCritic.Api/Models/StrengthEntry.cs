namespace EmbraceInnerCritic.Api.Models;

public sealed class StrengthEntry
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public string Event { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string[] Tags { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
