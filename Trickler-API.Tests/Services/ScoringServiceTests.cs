using Trickler_API.Services;

namespace Trickler_API.Tests.Services
{
    public class ScoringServiceTests
    {
        private readonly ScoringService _service = new();

        [Fact]
        public void ApplyWrongAttempt_100_90()
        {
            var v1 = _service.ApplyWrongAttempt(100);
            Assert.Equal(90, v1);

            var v2 = _service.ApplyWrongAttempt(v1);
            Assert.Equal(81, v2);

            var v3 = _service.ApplyWrongAttempt(v2);
            Assert.Equal(72, v3);
        }

        [Fact]
        public void CalculateFromBase_MultipleAttempts()
        {
            var result = _service.CalculateFromBase(100, 3);
            Assert.Equal(72, result);
        }
    }
}
