using System.Collections.Generic;

namespace PoleBarnGenerator.Models
{
    public class HorseStallParameters
    {
        public bool IsEnabled { get; set; } = false;
        public int NumberOfStalls { get; set; } = 6;
        public StallSize StandardSize { get; set; } = StallSize.Size10x12;
        public double AisleWidth { get; set; } = 12.0;
        public StallSide StallSide { get; set; } = StallSide.Both;
        public List<CustomStall> CustomStalls { get; set; } = new();
        public TackRoomLocation TackRoom { get; set; } = new();
        public FeedRoomLocation FeedRoom { get; set; } = new();
        public WashBayLocation WashBay { get; set; } = new();

        /// <summary>Gets stall width in feet based on StandardSize</summary>
        public double StallWidth => StandardSize switch
        {
            StallSize.Size10x10 => 10.0,
            StallSize.Size10x12 => 10.0,
            StallSize.Size12x12 => 12.0,
            StallSize.Size12x14 => 12.0,
            _ => 10.0
        };

        /// <summary>Gets stall depth in feet based on StandardSize</summary>
        public double StallDepth => StandardSize switch
        {
            StallSize.Size10x10 => 10.0,
            StallSize.Size10x12 => 12.0,
            StallSize.Size12x12 => 12.0,
            StallSize.Size12x14 => 14.0,
            _ => 12.0
        };

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);

            double requiredWidth = StallSide == StallSide.Both
                ? AisleWidth + 2 * StallDepth
                : AisleWidth + StallDepth;

            if (requiredWidth > barn.BuildingWidth)
                return (false, $"Horse stall layout requires {requiredWidth:F1}' width but building is only {barn.BuildingWidth:F1}' wide.");

            double requiredLength = NumberOfStalls * StallWidth;
            if (StallSide == StallSide.Both)
                requiredLength = (NumberOfStalls / 2 + NumberOfStalls % 2) * StallWidth;

            int specialtyBays = 0;
            if (TackRoom.IsEnabled) specialtyBays += TackRoom.BaySpan;
            if (FeedRoom.IsEnabled) specialtyBays += FeedRoom.BaySpan;
            if (WashBay.IsEnabled) specialtyBays += 1;

            if (requiredLength + specialtyBays * barn.ActualBaySpacing > barn.BuildingLength)
                return (false, "Horse stall layout exceeds building length.");

            if (AisleWidth < 10.0)
                return (false, "Center aisle must be at least 10' wide for horse safety.");

            return (true, null);
        }
    }

    public enum StallSize { Size10x10, Size10x12, Size12x12, Size12x14 }
    public enum StallSide { Left, Right, Both }

    public class CustomStall
    {
        public string Name { get; set; } = "Custom Stall";
        public double Width { get; set; } = 12.0;
        public double Depth { get; set; } = 12.0;
        public int BayStart { get; set; }
        public int BaySpan { get; set; } = 1;
        public StallFrontType FrontType { get; set; } = StallFrontType.Mesh;
        public bool HasWaterBucket { get; set; } = true;
        public bool HasFeedBucket { get; set; } = true;
    }

    public enum StallFrontType { Mesh, Bars, Solid, Dutch }

    public class TackRoomLocation
    {
        public bool IsEnabled { get; set; } = false;
        public int BayStart { get; set; }
        public int BaySpan { get; set; } = 2;
        public TackRoomSide Side { get; set; } = TackRoomSide.Left;
    }

    public class FeedRoomLocation
    {
        public bool IsEnabled { get; set; } = false;
        public int BayStart { get; set; }
        public int BaySpan { get; set; } = 1;
        public FeedRoomSide Side { get; set; } = FeedRoomSide.Left;
    }

    public class WashBayLocation
    {
        public bool IsEnabled { get; set; } = false;
        public int BayPosition { get; set; }
        public bool HasDrain { get; set; } = true;
        public bool HasHotWater { get; set; } = false;
    }

    public enum TackRoomSide { Left, Right }
    public enum FeedRoomSide { Left, Right }
}
