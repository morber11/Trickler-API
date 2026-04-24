namespace Trickler_API.Services
{
    public class ScoringService
    {
        public int ApplyWrongAttempt(int currentScore, int baseScore)
        {
            if (currentScore <= 0) return 0;

            var reducedScore = (currentScore * 9) / 10;
            var minimumScore = (int)Math.Floor(baseScore / 2.0);

            return Math.Max(reducedScore, minimumScore);
        }

        public int CalculateFromBase(int baseScore, int wrongAttempts)
        {
            var score = baseScore;
            for (int i = 0; i < wrongAttempts; i++)
            {
                score = ApplyWrongAttempt(score, baseScore);
            }
            return score;
        }
    }
}
