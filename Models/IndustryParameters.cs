using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace PoleBarnGenerator.Models
{
    public enum MilkingParlorType
    {
        Herringbone,
        Parallel,
        Rotary
    }

    public enum BuildingSpecialtyType
    {
        Standard,
        Dairy,
        EquipmentStorage,
        GrainStorage,
        Machinery
    }

    public enum LargeDoorType
    {
        Bifold,
        Hydraulic
    }

    public class DairyBarnModuleParameters
    {
        public bool IsEnabled { get; set; } = false;
        public MilkingParlorType ParlorType { get; set; } = MilkingParlorType.Herringbone;
        public int HerdSize { get; set; } = 120;
        public double FeedAlleyWidth { get; set; } = 14.0;
        public double BunkLineOffset { get; set; } = 2.0;
        public double ManureAlleyWidth { get; set; } = 10.0;
        public double FreestallWidth { get; set; } = 4.0;
        public double FreestallLength { get; set; } = 8.0;
        public bool ShowCowTrafficFlow { get; set; } = true;

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);
            if (HerdSize < 10) return (false, "Dairy herd size must be at least 10.");
            if (FeedAlleyWidth < 10) return (false, "Dairy feed alley width should be at least 10'.");
            if (ManureAlleyWidth < 8) return (false, "Dairy manure alley width should be at least 8'.");
            if (FreestallWidth < 3.5 || FreestallLength < 7.0)
                return (false, "Freestall dimensions are below typical dairy minimums (3.5' x 7').");
            if (barn.BuildingWidth < 36)
                return (false, "Dairy layout requires at least 36' building width.");

            return (true, null);
        }
    }

    /// <summary>
    /// Specialized dairy barn model extending base BarnParameters.
    /// This is used for workflows that need a complete, dairy-specific parameter object.
    /// </summary>
    public class DairyBarnParameters : BarnParameters
    {
        public DairyBarnModuleParameters Dairy { get; set; } = new DairyBarnModuleParameters { IsEnabled = true };

        public static DairyBarnParameters FromBarn(BarnParameters source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return new DairyBarnParameters
            {
                BuildingWidth = source.BuildingWidth,
                BuildingLength = source.BuildingLength,
                EaveHeight = source.EaveHeight,
                RoofPitchRise = source.RoofPitchRise,
                BaySpacing = source.BaySpacing,
                PostSize = source.PostSize,
                GirtSpacing = source.GirtSpacing,
                PurlinSpacing = source.PurlinSpacing,
                TrussType = source.TrussType,
                OverhangEave = source.OverhangEave,
                OverhangGable = source.OverhangGable,
                Doors = source.Doors?.Select(CloneDoor).ToList() ?? new List<DoorOpening>(),
                Windows = source.Windows?.Select(CloneWindow).ToList() ?? new List<WindowOpening>(),
                Dairy = CloneDairy(source.DairyBarn)
            };
        }

        private static DoorOpening CloneDoor(DoorOpening door)
        {
            if (door == null) return null;
            return new DoorOpening
            {
                Wall = door.Wall,
                Type = door.Type,
                Width = door.Width,
                Height = door.Height,
                CenterOffset = door.CenterOffset,
                SwingDirection = door.SwingDirection,
                HandingDirection = door.HandingDirection,
                TrackType = door.TrackType,
                HasLite = door.HasLite,
                SplitHeight = door.SplitHeight
            };
        }

        private static WindowOpening CloneWindow(WindowOpening window)
        {
            if (window == null) return null;
            return new WindowOpening
            {
                Wall = window.Wall,
                Width = window.Width,
                Height = window.Height,
                SillHeight = window.SillHeight,
                CenterOffset = window.CenterOffset,
                Type = window.Type,
                HasGrid = window.HasGrid,
                GridPattern = window.GridPattern
            };
        }

        private static DairyBarnModuleParameters CloneDairy(DairyBarnModuleParameters dairy)
        {
            if (dairy == null) return new DairyBarnModuleParameters { IsEnabled = true };
            return new DairyBarnModuleParameters
            {
                IsEnabled = dairy.IsEnabled,
                ParlorType = dairy.ParlorType,
                HerdSize = dairy.HerdSize,
                FeedAlleyWidth = dairy.FeedAlleyWidth,
                BunkLineOffset = dairy.BunkLineOffset,
                ManureAlleyWidth = dairy.ManureAlleyWidth,
                FreestallWidth = dairy.FreestallWidth,
                FreestallLength = dairy.FreestallLength,
                ShowCowTrafficFlow = dairy.ShowCowTrafficFlow
            };
        }
    }

    public class CraneRailParameters
    {
        public bool IsEnabled { get; set; } = false;
        public double RailHeight { get; set; } = 16.0;
        public double CapacityTons { get; set; } = 5.0;
        public double BeamSpacing { get; set; } = 24.0;

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);
            if (RailHeight <= 0 || RailHeight >= barn.EaveHeight)
                return (false, "Crane rail height must be above floor and below eave height.");
            if (CapacityTons <= 0)
                return (false, "Crane rail capacity must be positive.");
            if (BeamSpacing < 10)
                return (false, "Crane support beam spacing should be at least 10'.");

            return (true, null);
        }
    }

    public class LargeDoorParameters
    {
        public bool IsEnabled { get; set; } = false;
        public LargeDoorType DoorType { get; set; } = LargeDoorType.Bifold;
        public double Width { get; set; } = 30.0;
        public double Height { get; set; } = 16.0;
        public WallSide Wall { get; set; } = WallSide.Front;

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);
            if (Width < 12 || Width > 40)
                return (false, "Large door width must be between 12' and 40'.");
            if (Height <= 0 || Height > barn.EaveHeight)
                return (false, "Large door height must be positive and not exceed eave height.");

            return (true, null);
        }
    }

    public class EquipmentStorageParameters
    {
        public bool IsEnabled { get; set; } = false;
        public bool RequireClearSpan { get; set; } = true;
        public string HeavyDutySlabSpec { get; set; } = "8\" reinforced slab, 4500 psi concrete";
        public double ClearanceZoneRadius { get; set; } = 12.0;
        public CraneRailParameters CraneRail { get; set; } = new CraneRailParameters();
        public LargeDoorParameters LargeDoor { get; set; } = new LargeDoorParameters();

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);
            if (ClearanceZoneRadius < 6)
                return (false, "Equipment clearance zone radius should be at least 6'.");

            var (craneValid, craneError) = CraneRail.Validate(barn);
            if (!craneValid) return (false, craneError);

            var (doorValid, doorError) = LargeDoor.Validate(barn);
            if (!doorValid) return (false, doorError);

            return (true, null);
        }
    }

    public class VentilationParameters
    {
        public bool IsEnabled { get; set; } = false;
        public bool RidgeVentEnabled { get; set; } = true;
        public int CupolaCount { get; set; } = 1;
        public double CupolaAirflowCfmEach { get; set; } = 2500.0;
        public int WallLouverCount { get; set; } = 4;

        public (bool IsValid, string Error) Validate()
        {
            if (!IsEnabled) return (true, null);
            if (CupolaCount < 0) return (false, "Cupola count cannot be negative.");
            if (CupolaAirflowCfmEach < 0) return (false, "Cupola airflow must be non-negative.");
            if (WallLouverCount < 0) return (false, "Louver count cannot be negative.");
            return (true, null);
        }
    }

    public class DrainageParameters
    {
        public bool IsEnabled { get; set; } = false;
        public double FloorSlopePercent { get; set; } = 1.5;
        public bool FrenchDrainEnabled { get; set; } = true;
        public List<Point2d> DrainLocations { get; set; } = new();

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);
            if (FloorSlopePercent < 0.5 || FloorSlopePercent > 4.0)
                return (false, "Drainage floor slope should be between 0.5% and 4.0%.");

            foreach (var pt in DrainLocations)
            {
                if (pt.X < 0 || pt.X > barn.BuildingWidth || pt.Y < 0 || pt.Y > barn.BuildingLength)
                    return (false, "Drain locations must be inside the building footprint.");
            }

            return (true, null);
        }
    }

    public class GrainStorageParameters
    {
        public bool IsEnabled { get; set; } = false;
        public bool FlatStorageEnabled { get; set; } = true;
        public bool AerationFloorEnabled { get; set; } = true;
        public int BinPadCount { get; set; } = 2;
        public double BinPadDiameter { get; set; } = 24.0;

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);
            if (BinPadCount < 1) return (false, "Grain storage requires at least one bin pad.");
            if (BinPadDiameter < 8) return (false, "Bin pad diameter must be at least 8'.");
            if (barn.EaveHeight < 14) return (false, "Grain storage building eave height should be at least 14'.");
            return (true, null);
        }
    }

    public class MachineryBuildingParameters
    {
        public bool IsEnabled { get; set; } = false;
        public double ClearSpanBayWidth { get; set; } = 40.0;
        public double PreferredEaveHeight { get; set; } = 20.0;
        public double LargestEquipmentHeight { get; set; } = 14.0;

        public double RecommendedDoorHeight => Math.Max(12.0, Math.Ceiling((LargestEquipmentHeight + 2.0) * 2.0) / 2.0);

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);
            if (ClearSpanBayWidth < 24) return (false, "Machinery clear-span bay width should be at least 24'.");
            if (PreferredEaveHeight < 18 || PreferredEaveHeight > 24)
                return (false, "Machinery building eave height must be between 18' and 24'.");
            if (barn.EaveHeight < 18)
                return (false, "Machinery building mode requires building eave height of at least 18'.");
            return (true, null);
        }
    }
}
