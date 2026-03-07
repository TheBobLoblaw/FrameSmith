using System;
using System.Collections.Generic;
using System.Linq;

namespace PoleBarnGenerator.Models
{
    /// <summary>
    /// Composed dairy module parameters attached to BarnParameters.
    /// </summary>
    public class DairyBarnParameters
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
    /// Backward-compatible alias for previous dairy module type name.
    /// </summary>
    public class DairyBarnModuleParameters : DairyBarnParameters
    {
    }

    /// <summary>
    /// Specialized full-building dairy preset model.
    /// </summary>
    public class DairyBarnProjectParameters : BarnParameters
    {
        public DairyBarnParameters Dairy { get; set; } = new DairyBarnParameters { IsEnabled = true };

        public static DairyBarnProjectParameters FromBarn(BarnParameters source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return new DairyBarnProjectParameters
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

        private static DairyBarnParameters CloneDairy(DairyBarnParameters dairy)
        {
            if (dairy == null) return new DairyBarnParameters { IsEnabled = true };
            return new DairyBarnParameters
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
}
