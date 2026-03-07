using System.Collections.Generic;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using Xunit;

namespace PoleBarnGenerator.Tests
{
    public class OpeningValidatorTests
    {
        [Fact]
        public void ValidateOpenings_DetectsDoorAndWindowOverlap()
        {
            var p = new BarnParameters
            {
                BuildingWidth = 40,
                BuildingLength = 60,
                Doors = new List<DoorOpening>
                {
                    new DoorOpening { Wall = WallSide.Front, Type = DoorType.Overhead, Width = 12, Height = 10, CenterOffset = 12 }
                },
                Windows = new List<WindowOpening>
                {
                    new WindowOpening { Wall = WallSide.Front, Type = WindowType.Fixed, Width = 4, Height = 4, SillHeight = 3, CenterOffset = 12 }
                }
            };

            var posts = new Dictionary<WallSide, List<double>>
            {
                [WallSide.Front] = new List<double> { 0, 40 },
                [WallSide.Back] = new List<double> { 0, 40 },
                [WallSide.Left] = new List<double> { 0, 60 },
                [WallSide.Right] = new List<double> { 0, 60 }
            };

            var errors = OpeningValidator.ValidateOpenings(p, posts);
            Assert.Contains(errors, e => e.Contains("overlap or are too close"));
        }

        [Fact]
        public void ValidateOpenings_DetectsPostConflict()
        {
            var p = new BarnParameters
            {
                BuildingWidth = 30,
                BuildingLength = 40,
                Doors = new List<DoorOpening>
                {
                    new DoorOpening { Wall = WallSide.Front, Type = DoorType.Walk, Width = 4, Height = 8, CenterOffset = 10 }
                }
            };

            var posts = new Dictionary<WallSide, List<double>>
            {
                [WallSide.Front] = new List<double> { 10 },
                [WallSide.Back] = new List<double>(),
                [WallSide.Left] = new List<double>(),
                [WallSide.Right] = new List<double>()
            };

            var errors = OpeningValidator.ValidateOpenings(p, posts);
            Assert.Contains(errors, e => e.Contains("conflicts with structural post"));
        }
    }
}
