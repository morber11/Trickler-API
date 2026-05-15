namespace Trickler_API.DTO
{
    public enum SubmitAttemptResultType
    {
        Correct,
        Incorrect,
        AlreadySolved,
        Locked
    }

    public record SubmitAttemptRequest(string Answer);
}
