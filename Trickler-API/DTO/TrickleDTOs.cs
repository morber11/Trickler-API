namespace Trickler_API.DTO
{
    public record AnswerDto(int Id, string Answer);

    public record AvailabilityDto(
        string Type,
        DateOnly? From,
        DateOnly? Until,
        List<DateOnly>? Dates,
        List<string>? DaysOfWeek
    );

    public record TrickleWithAnswersDto(
        int Id,
        string Title,
        string Text,
        IEnumerable<AnswerDto> Answers,
        int Score,
        string RewardText,
        AvailabilityDto? Availability,
        int AttemptsPerTrickle
    );

    public record HydratedTrickleDto(
        int Id,
        string Title,
        string Text,
        int Score,
        string RewardText,
        AvailabilityDto? Availability,
        int AttemptsPerTrickle,
        int AttemptsLeft,
        bool IsSolved,
        int CurrentScore
    );

    public record UserTrickleProgressDto(
        string UserId,
        HydratedTrickleDto? Trickle,
        string? Result = null
    );

    public record SolvedTrickleDto(
        int Id,
        string Title,
        DateTime? SolvedAt,
        string? RewardCode,
        int CurrentScore
    );

    public record CreateTrickleRequest(
        string Title,
        string Text,
        IEnumerable<AnswerDto>? Answers,
        AvailabilityDto? Availability,
        int Score = 0,
        string? RewardText = null,
        int AttemptsPerTrickle = -1
    );

    public record UpdateTrickleRequest(
        int Id,
        string Title,
        string Text,
        IEnumerable<AnswerDto>? Answers,
        AvailabilityDto? Availability,
        int Score = 0,
        string? RewardText = null,
        int AttemptsPerTrickle = -1
    );
}
