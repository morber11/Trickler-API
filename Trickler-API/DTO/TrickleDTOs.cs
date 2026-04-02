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
        AvailabilityDto? Availability
    );

    public record AvailableTrickleDto(
        int Id,
        string Title,
        string Text,
        int Score,
        AvailabilityDto? Availability
    );

    public record CreateTrickleRequest(
        string Title,
        string Text,
        IEnumerable<AnswerDto>? Answers,
        AvailabilityDto? Availability,
        int Score = 0,
        string? RewardText = null
    );

    public record UpdateTrickleRequest(
        int Id,
        string Title,
        string Text,
        IEnumerable<AnswerDto>? Answers,
        AvailabilityDto? Availability,
        int Score = 0,
        string? RewardText = null
    );
}
