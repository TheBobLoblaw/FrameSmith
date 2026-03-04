using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace PoleBarnGenerator.Models
{
    public class LoftParameters
    {
        public bool IsEnabled { get; set; } = false;
        public List<int> SpanBays { get; set; } = new();
        public LoftDepth Depth { get; set; } = LoftDepth.FullWidth;
        public double FloorHeight { get; set; } = 8.0;
        public double RailingHeight { get; set; } = 3.5;
        public LoftAccess AccessType { get; set; } = LoftAccess.Stairs;
        public Point2d AccessLocation { get; set; }
        public bool HasFloorJoists { get; set; } = true;
        public double JoistSpacing { get; set; } = 16.0;
        public bool HasLighting { get; set; } = false;

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);

            if (FloorHeight < 7.0)
                return (false, "Loft floor height must be at least 7' for headroom clearance.");

            if (FloorHeight >= barn.EaveHeight)
                return (false, $"Loft floor height ({FloorHeight:F1}') must be below eave height ({barn.EaveHeight:F1}').");

            double clearanceAbove = barn.PeakHeight - FloorHeight;
            if (clearanceAbove < 4.0)
                return (false, $"Insufficient clearance above loft ({clearanceAbove:F1}'). Need at least 4'.");

            foreach (int bay in SpanBays)
            {
                if (bay < 0 || bay >= barn.NumberOfBays)
                    return (false, $"Loft bay index {bay} is out of range (0-{barn.NumberOfBays - 1}).");
            }

            if (JoistSpacing != 12.0 && JoistSpacing != 16.0 && JoistSpacing != 24.0)
                return (false, "Joist spacing must be 12\", 16\", or 24\" on center.");

            return (true, null);
        }
    }

    public enum LoftDepth { FullWidth, Partial }
    public enum LoftAccess { Stairs, StraightLadder, ShipLadder }
}
