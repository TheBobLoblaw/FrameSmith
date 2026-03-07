using PoleBarnGenerator.Generators;
using Xunit;

namespace PoleBarnGenerator.Tests
{
    public class GridBubbleLabelGeneratorTests
    {
        [Theory]
        [InlineData(0, "A")]
        [InlineData(25, "Z")]
        [InlineData(26, "AA")]
        [InlineData(27, "AB")]
        [InlineData(51, "AZ")]
        [InlineData(52, "BA")]
        [InlineData(701, "ZZ")]
        [InlineData(702, "AAA")]
        public void GetBayLabel_HandlesBeyondTwentySixBays(int index, string expected)
        {
            Assert.Equal(expected, GridLabelGenerator.GetBayLabel(index));
        }
    }
}
