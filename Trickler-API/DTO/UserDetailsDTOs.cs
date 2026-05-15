namespace Trickler_API.DTO
{
    public record UpdateUserVisibilityRequest(bool IsPrivate);

    public record UserDetailsResponseDto(
        int Id,
        string UserId,
        int TotalScore,
        bool IsPrivate,
        string Username,
        int CurrentScore);
}
