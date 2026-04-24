namespace Trickler_API.DTO
{
    public enum SubmitAttemptResultType
    {
        Correct,
        Incorrect,
        AlreadySolved,
        Locked
    }

    public record SubmitAnswerRequest(int TrickleId, string Answer);
    public record SubmitAttemptRequest(string Answer);
    public record SubmitAnswerResponse(bool IsSolved, DateTime? SolvedAt, string? RewardCode, int AttemptsLeft, int CurrentScore);
}
