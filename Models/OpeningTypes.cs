namespace PoleBarnGenerator.Models
{
    public enum DoorType
    {
        Overhead,
        Sliding,
        Walk,
        Dutch,
        Double
    }

    public enum SwingDirection
    {
        Out,
        In
    }

    public enum HandingDirection
    {
        Left,
        Right
    }

    public enum TrackType
    {
        StandardLift,
        HighLift,
        VerticalLift
    }

    public class DoorOpening
    {
        public WallSide Wall { get; set; } = WallSide.Front;
        public DoorType Type { get; set; } = DoorType.Overhead;
        public double Width { get; set; } = 10.0;
        public double Height { get; set; } = 10.0;
        public double CenterOffset { get; set; } = 15.0;

        public SwingDirection SwingDirection { get; set; } = SwingDirection.Out;
        public HandingDirection HandingDirection { get; set; } = HandingDirection.Left;
        public TrackType TrackType { get; set; } = TrackType.StandardLift;
        public bool HasLite { get; set; }
        public double SplitHeight { get; set; } = 3.5;

        public string HeaderSize => StructuralCalculations.HeaderSizing.GetHeaderDescription(
            StructuralCalculations.HeaderSizing.CalculateHeaderSize(Width, StructuralCalculations.LoadType.Roof));
    }

    public enum WindowType
    {
        Fixed,
        SingleHung,
        DoubleHung,
        Sliding,
        BarnSash,
        Awning,
        Casement
    }

    public enum GridPattern
    {
        None,
        Colonial,
        Prairie,
        Custom
    }

    public class WindowOpening
    {
        public WallSide Wall { get; set; } = WallSide.Left;
        public double Width { get; set; } = 3.0;
        public double Height { get; set; } = 3.0;
        public double SillHeight { get; set; } = 4.0;
        public double CenterOffset { get; set; } = 10.0;

        public WindowType Type { get; set; } = WindowType.Fixed;
        public bool HasGrid { get; set; }
        public GridPattern GridPattern { get; set; } = GridPattern.None;
    }
}
