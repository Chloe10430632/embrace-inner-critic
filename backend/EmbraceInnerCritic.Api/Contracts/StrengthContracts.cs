namespace EmbraceInnerCritic.Api.Contracts;

public sealed record UpsertStrengthRequest(string? Event, string? Notes, string?[]? Tags);
public sealed record StrengthResponse(Guid Id, string Event, string Notes, string[] Tags,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
