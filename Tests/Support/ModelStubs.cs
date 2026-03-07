using System.Collections.Generic;

namespace FrameSmith.Tests.Stubs
{
    public enum WallSide { Front, Back, Left, Right }

    public enum DoorType { Overhead, Sliding, Walk, Dutch, Double }

    public enum WindowType { Fixed, SingleHung, DoubleHung, Sliding, BarnSash, Awning, Casement }

    public enum FootprintShape { Rectangle, LShape, TShape, UShape, CustomPolygon }

    public class CurvedWallParameters
    {
        public bool Enabled { get; set; }
    }

    public class DoorOpening
    {
        public WallSide Wall { get; set; }
        public DoorType Type { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double CenterOffset { get; set; }
    }

    public class WindowOpening
    {
        public WallSide Wall { get; set; }
        public WindowType Type { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double SillHeight { get; set; }
        public double CenterOffset { get; set; }
    }

    public class PostLocation
    {
        public bool IsPlanInstance { get; set; }
        public WallSide Wall { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class BarnGeometry
    {
        public List<PostLocation> Posts { get; set; } = new List<PostLocation>();
    }

    public class BarnParameters
    {
        public double BuildingWidth { get; set; }
        public double BuildingLength { get; set; }
        public double PostWidthInches { get; set; } = 6;
        public double ActualBaySpacing { get; set; } = 10;
        public int NumberOfBays { get; set; } = 4;
        public CurvedWallParameters CurvedWall { get; set; } = new CurvedWallParameters();
        public FootprintShape FootprintShape { get; set; } = FootprintShape.Rectangle;

        public List<DoorOpening> Doors { get; set; } = new List<DoorOpening>();
        public List<WindowOpening> Windows { get; set; } = new List<WindowOpening>();
    }
}
