using Trickler_API.Services;

namespace Trickler_API.Tests.Services
{
    public class ScoringServiceTests
    {
        private readonly ScoringService _service = new();

        [Fact]
        public void ApplyWrongAttempt_100_90()
        {
            var v1 = _service.ApplyWrongAttempt(100, 100);
            Assert.Equal(90, v1);

            var v2 = _service.ApplyWrongAttempt(v1, 100);
            Assert.Equal(81, v2);

            var v3 = _service.ApplyWrongAttempt(v2, 100);
            Assert.Equal(72, v3);
        }

        [Fact]
        public void ApplyWrongAttempt_75_DoesNotGoBelow37()
        {
            var result = _service.ApplyWrongAttempt(38, 75);
            Assert.Equal(37, result);
        }

        [Fact]
        public void ApplyWrongAttempt_NeverDropsBelowHalfBase()
        {
            var score = 75;
            for (int i = 0; i < 10; i++)
            {
                score = _service.ApplyWrongAttempt(score, 75);
            }

            Assert.Equal(37, score);
        }

        [Fact]
        public void CalculateFromBase_MultipleAttempts()
        {
            var result = _service.CalculateFromBase(100, 3);
            Assert.Equal(72, result);
        }
    }
}
