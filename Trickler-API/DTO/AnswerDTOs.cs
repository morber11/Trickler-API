namespace Trickler_API.DTO
{
    public record VerifyAnswerRequest(int TrickleId, string Answer);
    public record SubmitAnswerRequest(int TrickleId, string Answer);
    public record SubmitAnswerResponse(bool Correct, string? Reward, int AttemptsLeft);
    public record VerifyAnswerResponse(bool Correct);
}
