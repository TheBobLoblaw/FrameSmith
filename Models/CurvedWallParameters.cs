namespace PoleBarnGenerator.Models
{
    public enum CurvedWallMode
    {
        ArcLengthDriven,
        ChordDriven
    }

    public class CurvedWallParameters
    {
        public bool Enabled { get; set; } = false;
        public double Radius { get; set; } = 120.0;
        public double ArcAngleDegrees { get; set; } = 45.0;
        public CurvedWallMode Mode { get; set; } = CurvedWallMode.ArcLengthDriven;
    }
}
