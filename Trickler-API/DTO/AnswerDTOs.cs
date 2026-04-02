namespace Trickler_API.DTO
{
    public record SubmitAnswerRequest(int TrickleId, string Answer);
    public record SubmitAnswerResponse(bool IsSolved, DateTime? SolvedAt, string? RewardCode, int AttemptsLeft);
}
