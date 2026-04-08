namespace Trickler_API.DTO
{
    public record RegisterRequest(string Username, string Email, string Password);

    public record LoginRequest(string Identifier, string Password);

    public record ChangePasswordRequest(string OldPassword, string NewPassword);

    public record MessageResponse(string Message);

    public record ErrorResponse(string Message, string? Errors);
    public record LoginResponse(string Message, string? UserId, string? UserName, string? Email, string AuthenticationMethod, string ReturnUrl, IEnumerable<string>? Roles);

    public record SignedInUserDto(string Id, string? UserName, string? Email);

    public record SignInResultDto(bool Succeeded, string? ErrorMessage, SignedInUserDto? User, IEnumerable<string> Roles);
}
