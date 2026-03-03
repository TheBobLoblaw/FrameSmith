using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    /// <summary>
    /// Defines a porch structure attached to one wall of the main building.
    /// Porches use shed-style roofs (like lean-tos) but add columns, railings,
    /// and residential finish details.
    /// </summary>
    public class PorchParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool _isEnabled = false;
        public bool IsEnabled
        {
            get => _isEnabled;
            set { _isEnabled = value; OnPropertyChanged(); }
        }

        private WallSide _attachmentWall = WallSide.Front;
        public WallSide AttachmentWall
        {
            get => _attachmentWall;
            set { _attachmentWall = value; OnPropertyChanged(); }
        }

        private double _depth = 8.0;
        /// <summary>Porch depth (projection from wall) in feet. Typical 6-12'.</summary>
        public double Depth
        {
            get => _depth;
            set { _depth = value; OnPropertyChanged(); }
        }

        private double _startPosition = 0.0;
        public double StartPosition
        {
            get => _startPosition;
            set { _startPosition = value; OnPropertyChanged(); }
        }

        private double _endPosition = 0.0;
        public double EndPosition
        {
            get => _endPosition;
            set { _endPosition = value; OnPropertyChanged(); }
        }

        private PorchRoofType _roofType = PorchRoofType.Shed;
        public PorchRoofType RoofType
        {
            get => _roofType;
            set { _roofType = value; OnPropertyChanged(); }
        }

        private double _roofPitch = 3.0;
        public double RoofPitch
        {
            get => _roofPitch;
            set { _roofPitch = value; OnPropertyChanged(); }
        }

        private double _columnSpacing = 8.0;
        public double ColumnSpacing
        {
            get => _columnSpacing;
            set { _columnSpacing = value; OnPropertyChanged(); }
        }

        private ColumnType _columnType = ColumnType.Square;
        public ColumnType ColumnType
        {
            get => _columnType;
            set { _columnType = value; OnPropertyChanged(); }
        }

        private bool _hasRailing = true;
        public bool HasRailing
        {
            get => _hasRailing;
            set { _hasRailing = value; OnPropertyChanged(); }
        }

        private double _railingHeight = 3.0;
        public double RailingHeight
        {
            get => _railingHeight;
            set { _railingHeight = value; OnPropertyChanged(); }
        }

        private bool _hasCeiling = false;
        public bool HasCeiling
        {
            get => _hasCeiling;
            set { _hasCeiling = value; OnPropertyChanged(); }
        }

        // ── Computed Properties ──

        /// <summary>Height where porch roof ties into main building wall</summary>
        public double GetTieInHeight(double mainEaveHeight)
        {
            return mainEaveHeight - 0.5; // 6" below main eave
        }

        /// <summary>Outer eave height (lower edge of porch roof)</summary>
        public double GetOuterEaveHeight(double mainEaveHeight)
        {
            return GetTieInHeight(mainEaveHeight) - Depth * (RoofPitch / 12.0);
        }

        public double GetWallLength(BarnParameters main)
        {
            return (AttachmentWall == WallSide.Left || AttachmentWall == WallSide.Right)
                ? main.BuildingLength
                : main.BuildingWidth;
        }

        public double GetEffectiveStart() => Math.Max(0, StartPosition);

        public double GetEffectiveEnd(BarnParameters main)
        {
            double wallLen = GetWallLength(main);
            return EndPosition <= 0 ? wallLen : Math.Min(EndPosition, wallLen);
        }

        public double GetEffectiveLength(BarnParameters main)
        {
            return GetEffectiveEnd(main) - GetEffectiveStart();
        }

        public (bool IsValid, string Error) Validate(BarnParameters main)
        {
            if (!IsEnabled) return (true, null);

            if (Depth < 4) return (false, $"Porch on {AttachmentWall} wall: depth must be at least 4 feet.");
            if (Depth > 16) return (false, $"Porch on {AttachmentWall} wall: depth exceeds 16 feet maximum.");
            if (RoofPitch < 1 || RoofPitch > 8)
                return (false, $"Porch on {AttachmentWall} wall: roof pitch must be between 1/12 and 8/12.");

            double outerEave = GetOuterEaveHeight(main.EaveHeight);
            if (outerEave < 7.0)
                return (false, $"Porch on {AttachmentWall} wall: outer eave height ({outerEave:F1}') is below 7'. Reduce depth or pitch.");

            double effLength = GetEffectiveLength(main);
            if (effLength <= 0)
                return (false, $"Porch on {AttachmentWall} wall: effective length is zero.");

            if (ColumnSpacing < 4 || ColumnSpacing > 16)
                return (false, $"Porch on {AttachmentWall} wall: column spacing must be 4-16 feet.");

            foreach (var lt in main.LeanTos)
            {
                if (lt.Enabled && lt.AttachmentWall == AttachmentWall)
                    return (false, $"Porch on {AttachmentWall} wall conflicts with lean-to on same wall.");
            }

            return (true, null);
        }
    }

    public enum PorchRoofType { Shed, Gable, Hip }
    public enum ColumnType { Square, Round, Decorative }
}
