namespace Trickler_API.DTO
{
    public record ChangePasswordRequest(string OldPassword, string NewPassword);
}
