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
        AvailabilityDto? Availability
    );

    public record AvailableTrickleDto(
        int Id,
        string Title,
        string Text,
        AvailabilityDto? Availability
    );

    public record CreateTrickleRequest(
        string Title,
        string Text,
        IEnumerable<AnswerDto>? Answers,
        AvailabilityDto? Availability
    );

    public record UpdateTrickleRequest(
        int Id,
        string Title,
        string Text,
        IEnumerable<AnswerDto> Answers,
        AvailabilityDto? Availability
    );
}
