using PoleBarnGenerator.Models;
using Xunit;

namespace PoleBarnGenerator.Tests
{
    public class BarnGeometryPostPlacementTests
    {
        [Theory]
        [InlineData(false, FootprintShape.Rectangle, 24.0, false)]
        [InlineData(false, FootprintShape.Rectangle, 24.1, true)]
        [InlineData(true, FootprintShape.Rectangle, 40.0, false)]
        [InlineData(false, FootprintShape.LShape, 40.0, false)]
        public void ShouldAddEndwallCenterPosts_UsesExpectedPlacementRules(bool curved, FootprintShape shape, double width, bool expected)
        {
            bool result = BarnGeometryPostPlacement.ShouldAddEndwallCenterPosts(curved, shape, width);
            Assert.Equal(expected, result);
        }
    }
}
