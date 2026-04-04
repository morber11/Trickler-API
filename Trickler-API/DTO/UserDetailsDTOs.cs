namespace Trickler_API.DTO
{
    public record UserDetailsResponseDto(
        int Id,
        string UserId,
        int TotalScore,
        bool IsPrivate,
        string Username,
        int CurrentScore);
}
