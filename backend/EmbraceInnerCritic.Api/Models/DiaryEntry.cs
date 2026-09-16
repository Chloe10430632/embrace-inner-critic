namespace EmbraceInnerCritic.Api.Models;

public sealed class DiaryEntry
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public ApplicationUser? User { get; set; }
    public required string CriticName { get; set; }
    public required string AnswersJson { get; set; }
    public bool IsComplete { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
