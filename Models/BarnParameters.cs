using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PoleBarnGenerator.Generators.TrussProfiles;

namespace PoleBarnGenerator.Models
{
    /// <summary>
    /// All user-supplied parameters for generating a pole barn structure.
    /// Implements INotifyPropertyChanged for WPF data binding.
    /// </summary>
    public class BarnParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ───────────────────────────────────────────────
        // Primary Dimensions
        // ───────────────────────────────────────────────

        private double _buildingWidth = 30.0;
        /// <summary>Overall width in feet (sidewall to sidewall)</summary>
        public double BuildingWidth
        {
            get => _buildingWidth;
            set { _buildingWidth = value; OnPropertyChanged(); OnPropertyChanged(nameof(PeakHeight)); }
        }

        private double _buildingLength = 40.0;
        /// <summary>Overall length in feet (endwall to endwall)</summary>
        public double BuildingLength
        {
            get => _buildingLength;
            set { _buildingLength = value; OnPropertyChanged(); }
        }

        private double _eaveHeight = 12.0;
        /// <summary>Height at eave in feet (ground to top of wall plate)</summary>
        public double EaveHeight
        {
            get => _eaveHeight;
            set { _eaveHeight = value; OnPropertyChanged(); OnPropertyChanged(nameof(PeakHeight)); }
        }

        private double _roofPitchRise = 4.0;
        /// <summary>Roof pitch as rise over 12 (e.g., 4 means 4/12)</summary>
        public double RoofPitchRise
        {
            get => _roofPitchRise;
            set { _roofPitchRise = value; OnPropertyChanged(); OnPropertyChanged(nameof(PeakHeight)); OnPropertyChanged(nameof(RoofPitchDisplay)); }
        }

        /// <summary>Display string for roof pitch (e.g., "4/12")</summary>
        public string RoofPitchDisplay => $"{RoofPitchRise}/12";

        /// <summary>Computed peak height in feet (uses truss profile strategy)</summary>
        public double PeakHeight => TrussFactory.GetTrussProfile(TrussType).CalculatePeakHeight(EaveHeight, BuildingWidth, RoofPitchRise);

        /// <summary>Computed roof rise in feet (from eave to peak)</summary>
        public double RoofRise => (BuildingWidth / 2.0) * (RoofPitchRise / 12.0);

        /// <summary>Roof slope angle in degrees</summary>
        public double RoofAngleDegrees => Math.Atan(RoofPitchRise / 12.0) * (180.0 / Math.PI);

        private double _baySpacing = 10.0;
        /// <summary>On-center distance between bays in feet</summary>
        public double BaySpacing
        {
            get => _baySpacing;
            set { _baySpacing = value; OnPropertyChanged(); OnPropertyChanged(nameof(NumberOfBays)); }
        }

        /// <summary>Computed number of bays</summary>
        public int NumberOfBays => Math.Max(1, (int)Math.Round(BuildingLength / BaySpacing));

        /// <summary>Actual bay spacing after rounding to whole bays</summary>
        public double ActualBaySpacing => BuildingLength / NumberOfBays;

        // ───────────────────────────────────────────────
        // Structural Options
        // ───────────────────────────────────────────────

        private string _postSize = "6x6";
        public string PostSize
        {
            get => _postSize;
            set { _postSize = value; OnPropertyChanged(); }
        }

        /// <summary>Post width in inches (parsed from PostSize)</summary>
        public double PostWidthInches
        {
            get
            {
                var parts = PostSize.Split('x');
                return parts.Length >= 1 && double.TryParse(parts[0], out double w) ? w : 6.0;
            }
        }

        /// <summary>Post depth in inches (parsed from PostSize)</summary>
        public double PostDepthInches
        {
            get
            {
                var parts = PostSize.Split('x');
                return parts.Length >= 2 && double.TryParse(parts[1], out double d) ? d : PostWidthInches;
            }
        }

        private double _girtSpacing = 24.0;
        /// <summary>Vertical spacing of wall girts in inches</summary>
        public double GirtSpacing
        {
            get => _girtSpacing;
            set { _girtSpacing = value; OnPropertyChanged(); }
        }

        private double _purlinSpacing = 24.0;
        /// <summary>On-center purlin spacing in inches</summary>
        public double PurlinSpacing
        {
            get => _purlinSpacing;
            set { _purlinSpacing = value; OnPropertyChanged(); }
        }

        private TrussType _trussType = TrussType.Common;
        public TrussType TrussType
        {
            get => _trussType;
            set { _trussType = value; OnPropertyChanged(); }
        }

        private double _overhangEave = 1.0;
        /// <summary>Eave overhang in feet</summary>
        public double OverhangEave
        {
            get => _overhangEave;
            set { _overhangEave = value; OnPropertyChanged(); }
        }

        private double _overhangGable = 1.0;
        /// <summary>Gable overhang in feet</summary>
        public double OverhangGable
        {
            get => _overhangGable;
            set { _overhangGable = value; OnPropertyChanged(); }
        }

        // ───────────────────────────────────────────────
        // Openings
        // ───────────────────────────────────────────────

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

        // ───────────────────────────────────────────────
        // Porches
        // ───────────────────────────────────────────────

        private PorchParameters _frontPorch = new PorchParameters { AttachmentWall = WallSide.Front };
        public PorchParameters FrontPorch
        {
            get => _frontPorch;
            set { _frontPorch = value; OnPropertyChanged(); }
        }

        private PorchParameters _backPorch = new PorchParameters { AttachmentWall = WallSide.Back };
        public PorchParameters BackPorch
        {
            get => _backPorch;
            set { _backPorch = value; OnPropertyChanged(); }
        }

        private PorchParameters _leftPorch = new PorchParameters { AttachmentWall = WallSide.Left };
        public PorchParameters LeftPorch
        {
            get => _leftPorch;
            set { _leftPorch = value; OnPropertyChanged(); }
        }

        private PorchParameters _rightPorch = new PorchParameters { AttachmentWall = WallSide.Right };
        public PorchParameters RightPorch
        {
            get => _rightPorch;
            set { _rightPorch = value; OnPropertyChanged(); }
        }

        /// <summary>All porch parameters for iteration</summary>
        public PorchParameters[] AllPorches => new[] { FrontPorch, BackPorch, LeftPorch, RightPorch };

        // ───────────────────────────────────────────────
        // Exterior Details
        // ───────────────────────────────────────────────

        private WainscotParameters _wainscot = new WainscotParameters();
        public WainscotParameters Wainscot
        {
            get => _wainscot;
            set { _wainscot = value; OnPropertyChanged(); }
        }

        private CupolaParameters _cupola = new CupolaParameters();
        public CupolaParameters Cupola
        {
            get => _cupola;
            set { _cupola = value; OnPropertyChanged(); }
        }

        private GutterParameters _gutters = new GutterParameters();
        public GutterParameters Gutters
        {
            get => _gutters;
            set { _gutters = value; OnPropertyChanged(); }
        }

        // ───────────────────────────────────────────────
        // Lean-Tos
        // ───────────────────────────────────────────────

        private List<LeanToParameters> _leanTos = new List<LeanToParameters>();
        /// <summary>Lean-to structures attached to building walls (up to 4, one per wall)</summary>
        public List<LeanToParameters> LeanTos
        {
            get => _leanTos;
            set { _leanTos = value; OnPropertyChanged(); }
        }

        // ───────────────────────────────────────────────
        // Interior Features
        // ───────────────────────────────────────────────

        private HorseStallParameters _horseStalls = new HorseStallParameters();
        public HorseStallParameters HorseStalls
        {
            get => _horseStalls;
            set { _horseStalls = value; OnPropertyChanged(); }
        }

        private LoftParameters _loft = new LoftParameters();
        public LoftParameters Loft
        {
            get => _loft;
            set { _loft = value; OnPropertyChanged(); }
        }

        private InteriorPartitionParameters _partitions = new InteriorPartitionParameters();
        public InteriorPartitionParameters Partitions
        {
            get => _partitions;
            set { _partitions = value; OnPropertyChanged(); }
        }

        private WorkshopParameters _workshop = new WorkshopParameters();
        public WorkshopParameters Workshop
        {
            get => _workshop;
            set { _workshop = value; OnPropertyChanged(); }
        }

        // ───────────────────────────────────────────────
        // ───────────────────────────────────────────────
        // Structural Engineering
        // ───────────────────────────────────────────────

        private StructuralParameters _structural = new StructuralParameters();
        /// <summary>Structural engineering design parameters</summary>
        public StructuralParameters Structural
        {
            get => _structural;
            set { _structural = value; OnPropertyChanged(); }
        }

        /// <summary>Whether to run structural analysis and include load tables on drawings</summary>
        public bool IncludeStructuralAnalysis { get; set; } = false;

        // Output Options
        // ───────────────────────────────────────────────

        public bool GeneratePlan { get; set; } = true;
        public bool GenerateFront { get; set; } = true;
        public bool GenerateSide { get; set; } = true;
        public bool Generate3D { get; set; } = true;
        public bool AddDimensions { get; set; } = true;

        // ───────────────────────────────────────────────
        // Validation
        // ───────────────────────────────────────────────

        public (bool IsValid, string Error) Validate()
        {
            if (BuildingWidth <= 0) return (false, "Building width must be positive.");
            if (BuildingLength <= 0) return (false, "Building length must be positive.");
            if (EaveHeight <= 0) return (false, "Eave height must be positive.");
            if (RoofPitchRise < 0 || RoofPitchRise > 24) return (false, "Roof pitch must be between 0/12 and 24/12.");
            if (BaySpacing <= 0 || BaySpacing > BuildingLength) return (false, "Bay spacing must be positive and less than building length.");
            if (BuildingWidth < 10) return (false, "Building width should be at least 10 feet.");
            if (BuildingLength < 10) return (false, "Building length should be at least 10 feet.");
            if (EaveHeight < 6) return (false, "Eave height should be at least 6 feet.");

            // Validate individual door properties
            foreach (var door in Doors)
            {
                if (door.Width <= 0) return (false, "Door width must be positive.");
                if (door.Height <= 0) return (false, "Door height must be positive.");
                if (door.Height > EaveHeight) return (false, $"Door on {door.Wall} wall ({door.Height}') exceeds eave height ({EaveHeight}').");
                if (door.CenterOffset < 0) return (false, "Door center offset must not be negative.");

                if (door.Type == DoorType.Dutch && (door.SplitHeight <= 0 || door.SplitHeight >= door.Height))
                    return (false, $"Dutch door split height must be between 0 and door height ({door.Height}').");
            }

            // Validate individual window properties
            foreach (var window in Windows)
            {
                if (window.Width <= 0) return (false, "Window width must be positive.");
                if (window.Height <= 0) return (false, "Window height must be positive.");
                if (window.SillHeight < 0) return (false, "Window sill height must not be negative.");
                if (window.SillHeight + window.Height > EaveHeight)
                    return (false, $"Window on {window.Wall} wall top ({window.SillHeight + window.Height}') exceeds eave height ({EaveHeight}').");
                if (window.CenterOffset < 0) return (false, "Window center offset must not be negative.");
            }

            // Run conflict detection
            // Validate porches
            foreach (var porch in AllPorches)
            {
                var (pValid, pError) = porch.Validate(this);
                if (!pValid) return (false, pError);
            }

            // Check for porch-porch conflicts (no two porches on same wall)
            var usedPorchWalls = new HashSet<WallSide>();
            foreach (var porch in AllPorches)
            {
                if (!porch.IsEnabled) continue;
                if (!usedPorchWalls.Add(porch.AttachmentWall))
                    return (false, $"Multiple porches on the {porch.AttachmentWall} wall are not allowed.");
            }

            // Validate lean-tos
            foreach (var leanTo in LeanTos)
            {
                var (ltValid, ltError) = leanTo.Validate(this);
                if (!ltValid) return (false, ltError);
            }
            // Check for duplicate attachment walls
            var usedWalls = new HashSet<WallSide>();
            foreach (var lt in LeanTos)
            {
                if (!lt.Enabled) continue;
                if (!usedWalls.Add(lt.AttachmentWall))
                    return (false, $"Multiple lean-tos on the {lt.AttachmentWall} wall are not allowed.");
            }

            var conflicts = Utils.OpeningValidator.ValidateOpenings(this);
            if (conflicts.Count > 0)
                return (false, conflicts[0]);

            // Validate interior features
            var (hsValid, hsError) = HorseStalls.Validate(this);
            if (!hsValid) return (false, hsError);

            if (Loft != null)
            {
                var (lValid, lError) = Loft.Validate(this);
                if (!lValid) return (false, lError);
            }

            if (Partitions != null)
            {
                var (ptValid, ptError) = Partitions.Validate(this);
                if (!ptValid) return (false, ptError);
            }

            if (Workshop != null)
            {
                var (wValid, wError) = Workshop.Validate(this);
                if (!wValid) return (false, wError);
            }

            return (true, null);
        }

        /// <summary>
        /// Returns a deep copy with default preset values for common barn sizes.
        /// </summary>
        public static BarnParameters CreatePreset(string name)
        {
            switch (name)
            {
                case "24x30":
                    return new BarnParameters { BuildingWidth = 24, BuildingLength = 30, EaveHeight = 10, BaySpacing = 10, RoofPitchRise = 4 };
                case "30x40":
                    return new BarnParameters { BuildingWidth = 30, BuildingLength = 40, EaveHeight = 12, BaySpacing = 10, RoofPitchRise = 4 };
                case "40x60":
                    return new BarnParameters { BuildingWidth = 40, BuildingLength = 60, EaveHeight = 14, BaySpacing = 12, RoofPitchRise = 4 };
                case "50x80":
                    return new BarnParameters { BuildingWidth = 50, BuildingLength = 80, EaveHeight = 16, BaySpacing = 10, RoofPitchRise = 3 };
                case "60x100":
                    return new BarnParameters { BuildingWidth = 60, BuildingLength = 100, EaveHeight = 16, BaySpacing = 10, RoofPitchRise = 3 };
                default:
                    return new BarnParameters();
            }
        }
    }

    // ───────────────────────────────────────────────
    // Supporting Types
    // ───────────────────────────────────────────────

    public enum TrussType
    {
        Common,
        Scissor,
        MonoSlope,
        Gambrel,
        Monitor,
        Attic
    }

    public enum WallSide
    {
        Front,  // Endwall at Y = 0
        Back,   // Endwall at Y = BuildingLength
        Left,   // Sidewall at X = 0
        Right   // Sidewall at X = BuildingWidth
    }

    public enum DoorType
    {
        Overhead,
        Sliding,
        Walk,
        /// <summary>Dutch door — split horizontally at SplitHeight</summary>
        Dutch,
        /// <summary>Double (French) door — two swing leaves</summary>
        Double
    }

    public enum SwingDirection
    {
        Out,
        In
    }

    public enum HandingDirection
    {
        /// <summary>Hinges on left side (opens from right)</summary>
        Left,
        /// <summary>Hinges on right side (opens from left)</summary>
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
        // ── Existing properties (backward compatible) ──
        public WallSide Wall { get; set; } = WallSide.Front;
        public DoorType Type { get; set; } = DoorType.Overhead;
        public double Width { get; set; } = 10.0;   // feet
        public double Height { get; set; } = 10.0;  // feet
        public double CenterOffset { get; set; } = 15.0; // feet from left corner of wall

        // ── Enhanced properties ──

        /// <summary>Swing direction for Walk/Dutch/Double doors</summary>
        public SwingDirection SwingDirection { get; set; } = SwingDirection.Out;

        /// <summary>Handing direction — which side the hinges are on</summary>
        public HandingDirection HandingDirection { get; set; } = HandingDirection.Left;

        /// <summary>Track type for Overhead doors</summary>
        public TrackType TrackType { get; set; } = TrackType.StandardLift;

        /// <summary>Whether the door has a lite (window insert)</summary>
        public bool HasLite { get; set; } = false;

        /// <summary>Split height for Dutch doors in feet (measured from bottom)</summary>
        public double SplitHeight { get; set; } = 3.5;

        /// <summary>Auto-calculated header size description (e.g., "2x10 DF")</summary>
        public string HeaderSize => StructuralCalculations.HeaderSizing.GetHeaderDescription(
            StructuralCalculations.HeaderSizing.CalculateHeaderSize(Width, LoadType.Roof));
    }

    public enum WindowType
    {
        Fixed,
        SingleHung,
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
        // ── Existing properties (backward compatible) ──
        public WallSide Wall { get; set; } = WallSide.Left;
        public double Width { get; set; } = 3.0;    // feet
        public double Height { get; set; } = 3.0;   // feet
        public double SillHeight { get; set; } = 4.0; // feet from ground
        public double CenterOffset { get; set; } = 10.0; // feet from left corner

        // ── Enhanced properties ──

        /// <summary>Window type</summary>
        public WindowType Type { get; set; } = WindowType.Fixed;

        /// <summary>Whether the window has a grid pattern</summary>
        public bool HasGrid { get; set; } = false;

        /// <summary>Grid pattern style</summary>
        public GridPattern GridPattern { get; set; } = GridPattern.None;
    }
}
