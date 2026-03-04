using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace PoleBarnGenerator.Models
{
    public class WorkshopParameters
    {
        public bool IsEnabled { get; set; } = false;
        public bool HasWorkbench { get; set; } = false;
        public WorkbenchLocation WorkbenchLocation { get; set; } = new();
        public bool HasCompressorArea { get; set; } = false;
        public Point2d CompressorLocation { get; set; }
        public bool HasToolStorage { get; set; } = false;
        public List<WorkshopPowerLocation> PowerOutlets { get; set; } = new();
        public bool HasFloorDrain { get; set; } = false;
        public List<Point2d> DrainLocations { get; set; } = new();
        public bool HasOverheadCrane { get; set; } = false;
        public double CraneCapacity { get; set; } = 1.0;

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);

            if (HasWorkbench)
            {
                var wb = WorkbenchLocation;
                if (wb.Length < 4.0 || wb.Length > 16.0)
                    return (false, "Workbench length must be between 4' and 16'.");
                if (wb.Depth < 1.5 || wb.Depth > 4.0)
                    return (false, "Workbench depth must be between 1.5' and 4'.");
            }

            if (HasOverheadCrane && barn.EaveHeight < 14.0)
                return (false, "Overhead crane requires minimum 14' eave height.");

            if (HasOverheadCrane && CraneCapacity <= 0)
                return (false, "Crane capacity must be positive.");

            return (true, null);
        }
    }

    public class WorkbenchLocation
    {
        public Point2d Position { get; set; }
        public double Length { get; set; } = 8.0;
        public double Depth { get; set; } = 2.0;
        public bool HasVise { get; set; } = true;
        public bool HasDrawers { get; set; } = false;
    }

    public class WorkshopPowerLocation
    {
        public Point2d Position { get; set; }
        public PowerType Type { get; set; } = PowerType.Standard110;
        public bool IsGFCI { get; set; } = false;
    }

    public enum PowerType { Standard110, Heavy220, Welding, CompressedAir }
}
