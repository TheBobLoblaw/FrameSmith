using PoleBarnGenerator.Models.StructuralCalculations;
using Xunit;

namespace PoleBarnGenerator.Tests
{
    public class StructuralCalculationsHeaderSizingTests
    {
        [Fact]
        public void CalculateHeaderSize_Roof_AtTenFeet_ReturnsDouble2x12()
        {
            var header = HeaderSizing.CalculateHeaderSize(10.0, LoadType.Roof);
            Assert.False(header.IsLVL);
            Assert.Equal(2, header.Plies);
            Assert.Equal(12, header.NominalDepth);
        }

        [Fact]
        public void CalculateHeaderSize_Roof_AtTwelveFeet_ReturnsTriple2x12()
        {
            var header = HeaderSizing.CalculateHeaderSize(12.0, LoadType.Roof);
            Assert.False(header.IsLVL);
            Assert.Equal(3, header.Plies);
            Assert.Equal(12, header.NominalDepth);
        }

        [Fact]
        public void CalculateHeaderSize_Floor_AtTwelveFeet_ReturnsLvl()
        {
            var header = HeaderSizing.CalculateHeaderSize(12.0, LoadType.Floor);
            Assert.True(header.IsLVL);
            Assert.Equal(3.5, header.ActualWidth);
            Assert.Equal(11.875, header.ActualDepth);
        }
    }
}
