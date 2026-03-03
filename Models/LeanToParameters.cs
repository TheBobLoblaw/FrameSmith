using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    /// <summary>
    /// Defines a lean-to structure attached to one wall of the main building.
    /// Lean-tos are single-slope roofs that tie into the main building wall.
    /// </summary>
    public class LeanToParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool _enabled = false;
        /// <summary>Whether this lean-to is active</summary>
        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(); }
        }

        private WallSide _attachmentWall = WallSide.Left;
        /// <summary>Which main building wall this lean-to attaches to</summary>
        public WallSide AttachmentWall
        {
            get => _attachmentWall;
            set { _attachmentWall = value; OnPropertyChanged(); }
        }

        private double _width = 12.0;
        /// <summary>Depth of lean-to perpendicular to attachment wall, in feet (4-30')</summary>
        public double Width
        {
            get => _width;
            set { _width = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieInHeight)); }
        }

        private double _startPosition = 0.0;
        /// <summary>Start position along attachment wall in feet (0 = beginning of wall)</summary>
        public double StartPosition
        {
            get => _startPosition;
            set { _startPosition = value; OnPropertyChanged(); }
        }

        private double _endPosition = 0.0;
        /// <summary>End position along attachment wall in feet (0 = full wall length)</summary>
        public double EndPosition
        {
            get => _endPosition;
            set { _endPosition = value; OnPropertyChanged(); }
        }

        private double _eaveHeight = 10.0;
        /// <summary>Eave height at the outer edge of the lean-to in feet</summary>
        public double EaveHeight
        {
            get => _eaveHeight;
            set { _eaveHeight = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieInHeight)); }
        }

        private double _roofPitch = 3.0;
        /// <summary>Roof pitch as rise/12 (typical: 2/12 to 4/12)</summary>
        public double RoofPitch
        {
            get => _roofPitch;
            set { _roofPitch = value; OnPropertyChanged(); OnPropertyChanged(nameof(TieInHeight)); }
        }

        private LeanToType _type = LeanToType.Open;
        /// <summary>Enclosure type</summary>
        public LeanToType Type
        {
            get => _type;
            set { _type = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Which of the 3 outer walls are enclosed.
        /// Index 0 = outer wall (parallel to attachment), 1 = left end, 2 = right end.
        /// </summary>
        public bool[] EnclosedWalls { get; set; } = new bool[3];

        private List<DoorOpening> _doors = new List<DoorOpening>();
        public List<DoorOpening> Doors
        {
            get => _doors;
            set { _doors = value; OnPropertyChanged(); }
        }

        private List<WindowOpening> _windows = new List<WindowOpening>();
        public List<WindowOpening> Windows
        {
            get => _windows;
            set { _windows = value; OnPropertyChanged(); }
        }

        // ── Computed Properties ──

        /// <summary>Height where lean-to roof meets the main building wall</summary>
        public double TieInHeight => EaveHeight + Width * (RoofPitch / 12.0);

        /// <summary>Roof pitch display string</summary>
        public string RoofPitchDisplay => $"{RoofPitch}/12";

        /// <summary>
        /// Gets the effective length along the attachment wall.
        /// EndPosition=0 means full wall length.
        /// </summary>
        public double GetEffectiveLength(BarnParameters main)
        {
            double wallLen = GetWallLength(main);
            double end = EndPosition <= 0 ? wallLen : Math.Min(EndPosition, wallLen);
            return end - Math.Max(0, StartPosition);
        }

        /// <summary>Full length of the attachment wall on the main building.</summary>
        public double GetWallLength(BarnParameters main)
        {
            return (AttachmentWall == WallSide.Left || AttachmentWall == WallSide.Right)
                ? main.BuildingLength
                : main.BuildingWidth;
        }

        /// <summary>Effective start position (clamped to 0).</summary>
        public double GetEffectiveStart() => Math.Max(0, StartPosition);

        /// <summary>Effective end position.</summary>
        public double GetEffectiveEnd(BarnParameters main)
        {
            double wallLen = GetWallLength(main);
            return EndPosition <= 0 ? wallLen : Math.Min(EndPosition, wallLen);
        }

        // ── Validation ──

        public (bool IsValid, string Error) Validate(BarnParameters main)
        {
            if (!Enabled) return (true, null);

            if (Width < 4) return (false, $"Lean-to on {AttachmentWall} wall: width must be at least 4 feet.");
            if (Width > 30) return (false, $"Lean-to on {AttachmentWall} wall: width exceeds 30 feet maximum.");
            if (EaveHeight < 6) return (false, $"Lean-to on {AttachmentWall} wall: eave height must be at least 6 feet.");
            if (RoofPitch < 0.5 || RoofPitch > 12)
                return (false, $"Lean-to on {AttachmentWall} wall: roof pitch must be between 0.5/12 and 12/12.");

            if (TieInHeight > main.EaveHeight)
                return (false, $"Lean-to on {AttachmentWall} wall: tie-in height ({TieInHeight:F1}') exceeds main eave ({main.EaveHeight}')." +
                       " Reduce lean-to width, pitch, or eave height.");

            double effLength = GetEffectiveLength(main);
            if (effLength <= 0)
                return (false, $"Lean-to on {AttachmentWall} wall: effective length is zero.");

            double wallLen = GetWallLength(main);
            if (StartPosition < 0 || StartPosition >= wallLen)
                return (false, $"Lean-to on {AttachmentWall} wall: start position out of range.");
            if (EndPosition > wallLen)
                return (false, $"Lean-to on {AttachmentWall} wall: end position exceeds wall length ({wallLen}').");

            return (true, null);
        }
    }

    public enum LeanToType
    {
        Open,
        PartiallyEnclosed,
        FullyEnclosed
    }
}
