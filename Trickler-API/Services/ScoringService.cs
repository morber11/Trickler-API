namespace Trickler_API.Services
{
    public class ScoringService
    {
        public int ApplyWrongAttempt(int currentScore)
        {
            if (currentScore <= 0) return 0;
            return (currentScore * 9) / 10;
        }

        public int CalculateFromBase(int baseScore, int wrongAttempts)
        {
            var score = baseScore;
            for (int i = 0; i < wrongAttempts; i++)
            {
                score = ApplyWrongAttempt(score);
            }
            return score;
        }
    }
}
