using System.Text.Json;

namespace EmbraceInnerCritic.Api.Contracts;

public sealed record UpsertDiaryRequest(string CriticName, JsonElement Answers, bool IsComplete);

public sealed record DiaryResponse(
    Guid Id,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string CriticName,
    JsonElement Answers,
    bool IsComplete);
