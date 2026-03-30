namespace Trickler_API.DTO
{
    public record ChangePasswordRequest(string OldPassword, string NewPassword);

    public record MessageResponse(string Message);

    public record ErrorResponse(string Message, string? Errors);

    public record RegisterResponse(string Message, string? UserId);

    public record LoginResponse(string Message, string? UserId, string? Email, string AuthenticationMethod, string ReturnUrl, IEnumerable<string>? Roles);
}
