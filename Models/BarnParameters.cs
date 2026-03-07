using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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

        private DimensionParameters _dimensions = new DimensionParameters();
        public DimensionParameters Dimensions
        {
            get => _dimensions;
            set
            {
                if (_dimensions != null) _dimensions.PropertyChanged -= DimensionsChanged;
                _dimensions = value ?? new DimensionParameters();
                _dimensions.PropertyChanged += DimensionsChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BuildingWidth));
                OnPropertyChanged(nameof(BuildingLength));
                OnPropertyChanged(nameof(EaveHeight));
                OnPropertyChanged(nameof(RoofPitchRise));
                OnPropertyChanged(nameof(RoofPitchDisplay));
                OnPropertyChanged(nameof(RoofRise));
                OnPropertyChanged(nameof(RoofAngleDegrees));
                OnPropertyChanged(nameof(BaySpacing));
                OnPropertyChanged(nameof(NumberOfBays));
                OnPropertyChanged(nameof(ActualBaySpacing));
                OnPropertyChanged(nameof(PeakHeight));
                OnPropertyChanged(nameof(DefaultFloorHeight));
            }
        }

        private StructuralOptions _structuralOptions = new StructuralOptions();
        public StructuralOptions StructuralOptions
        {
            get => _structuralOptions;
            set
            {
                if (_structuralOptions != null) _structuralOptions.PropertyChanged -= StructuralOptionsChanged;
                _structuralOptions = value ?? new StructuralOptions();
                _structuralOptions.PropertyChanged += StructuralOptionsChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PostSize));
                OnPropertyChanged(nameof(PostWidthInches));
                OnPropertyChanged(nameof(PostDepthInches));
                OnPropertyChanged(nameof(GirtSpacing));
                OnPropertyChanged(nameof(PurlinSpacing));
                OnPropertyChanged(nameof(TrussType));
                OnPropertyChanged(nameof(HeaderStrategy));
                OnPropertyChanged(nameof(WindRegion));
                OnPropertyChanged(nameof(SnowRegion));
                OnPropertyChanged(nameof(PeakHeight));
            }
        }

        private OpeningParameters _openings = new OpeningParameters();
        public OpeningParameters Openings
        {
            get => _openings;
            set
            {
                if (_openings != null) _openings.PropertyChanged -= OpeningsChanged;
                _openings = value ?? new OpeningParameters();
                _openings.PropertyChanged += OpeningsChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Doors));
                OnPropertyChanged(nameof(Windows));
            }
        }

        private ExteriorParameters _exterior = new ExteriorParameters();
        public ExteriorParameters Exterior
        {
            get => _exterior;
            set
            {
                if (_exterior != null) _exterior.PropertyChanged -= ExteriorChanged;
                _exterior = value ?? new ExteriorParameters();
                _exterior.PropertyChanged += ExteriorChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(OverhangEave));
                OnPropertyChanged(nameof(OverhangGable));
                OnPropertyChanged(nameof(Wainscot));
                OnPropertyChanged(nameof(Cupola));
                OnPropertyChanged(nameof(Gutters));
            }
        }

        private AdvancedGeometryParameters _advancedGeometry = new AdvancedGeometryParameters();
        public AdvancedGeometryParameters AdvancedGeometry
        {
            get => _advancedGeometry;
            set
            {
                if (_advancedGeometry != null) _advancedGeometry.PropertyChanged -= AdvancedGeometryChanged;
                _advancedGeometry = value ?? new AdvancedGeometryParameters();
                _advancedGeometry.PropertyChanged += AdvancedGeometryChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Story));
                OnPropertyChanged(nameof(NumberOfFloors));
                OnPropertyChanged(nameof(FloorHeights));
                OnPropertyChanged(nameof(DefaultFloorHeight));
                OnPropertyChanged(nameof(FloorConnection));
                OnPropertyChanged(nameof(FloorBeamSize));
                OnPropertyChanged(nameof(CurvedWall));
                OnPropertyChanged(nameof(Footprint));
                OnPropertyChanged(nameof(FootprintShape));
                OnPropertyChanged(nameof(FootprintVertices));
                OnPropertyChanged(nameof(FootprintInsetWidth));
                OnPropertyChanged(nameof(FootprintInsetDepth));
                OnPropertyChanged(nameof(ExpansionJoint));
            }
        }

        private StructuralEngineeringParameters _engineering = new StructuralEngineeringParameters();
        public StructuralEngineeringParameters Engineering
        {
            get => _engineering;
            set
            {
                if (_engineering != null) _engineering.PropertyChanged -= EngineeringChanged;
                _engineering = value ?? new StructuralEngineeringParameters();
                _engineering.PropertyChanged += EngineeringChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Structural));
                OnPropertyChanged(nameof(IncludeStructuralAnalysis));
            }
        }

        public BarnParameters()
        {
            _dimensions.PropertyChanged += DimensionsChanged;
            _structuralOptions.PropertyChanged += StructuralOptionsChanged;
            _openings.PropertyChanged += OpeningsChanged;
            _exterior.PropertyChanged += ExteriorChanged;
            _advancedGeometry.PropertyChanged += AdvancedGeometryChanged;
            _engineering.PropertyChanged += EngineeringChanged;

            _frontPorch.PropertyChanged += PorchChanged;
            _backPorch.PropertyChanged += PorchChanged;
            _leftPorch.PropertyChanged += PorchChanged;
            _rightPorch.PropertyChanged += PorchChanged;
        }

        private void DimensionsChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DimensionParameters.BuildingWidth):
                    OnPropertyChanged(nameof(BuildingWidth));
                    OnPropertyChanged(nameof(RoofRise));
                    OnPropertyChanged(nameof(PeakHeight));
                    break;
                case nameof(DimensionParameters.BuildingLength):
                    OnPropertyChanged(nameof(BuildingLength));
                    OnPropertyChanged(nameof(NumberOfBays));
                    OnPropertyChanged(nameof(ActualBaySpacing));
                    break;
                case nameof(DimensionParameters.EaveHeight):
                    OnPropertyChanged(nameof(EaveHeight));
                    OnPropertyChanged(nameof(PeakHeight));
                    OnPropertyChanged(nameof(DefaultFloorHeight));
                    break;
                case nameof(DimensionParameters.RoofPitchRise):
                    OnPropertyChanged(nameof(RoofPitchRise));
                    OnPropertyChanged(nameof(RoofPitchDisplay));
                    OnPropertyChanged(nameof(RoofRise));
                    OnPropertyChanged(nameof(RoofAngleDegrees));
                    OnPropertyChanged(nameof(PeakHeight));
                    break;
                case nameof(DimensionParameters.BaySpacing):
                    OnPropertyChanged(nameof(BaySpacing));
                    OnPropertyChanged(nameof(NumberOfBays));
                    OnPropertyChanged(nameof(ActualBaySpacing));
                    break;
                case nameof(DimensionParameters.RoofPitchDisplay):
                    OnPropertyChanged(nameof(RoofPitchDisplay));
                    break;
                case nameof(DimensionParameters.RoofAngleDegrees):
                    OnPropertyChanged(nameof(RoofAngleDegrees));
                    break;
                case nameof(DimensionParameters.NumberOfBays):
                    OnPropertyChanged(nameof(NumberOfBays));
                    break;
                case nameof(DimensionParameters.ActualBaySpacing):
                    OnPropertyChanged(nameof(ActualBaySpacing));
                    break;
            }
        }

        private void StructuralOptionsChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(StructuralOptions.PostSize):
                    OnPropertyChanged(nameof(PostSize));
                    OnPropertyChanged(nameof(PostWidthInches));
                    OnPropertyChanged(nameof(PostDepthInches));
                    break;
                case nameof(StructuralOptions.GirtSpacing):
                    OnPropertyChanged(nameof(GirtSpacing));
                    break;
                case nameof(StructuralOptions.PurlinSpacing):
                    OnPropertyChanged(nameof(PurlinSpacing));
                    break;
                case nameof(StructuralOptions.TrussType):
                    OnPropertyChanged(nameof(TrussType));
                    OnPropertyChanged(nameof(PeakHeight));
                    break;
                case nameof(StructuralOptions.HeaderStrategy):
                    OnPropertyChanged(nameof(HeaderStrategy));
                    break;
                case nameof(StructuralOptions.WindRegion):
                    OnPropertyChanged(nameof(WindRegion));
                    break;
                case nameof(StructuralOptions.SnowRegion):
                    OnPropertyChanged(nameof(SnowRegion));
                    break;
                case nameof(StructuralOptions.PostWidthInches):
                    OnPropertyChanged(nameof(PostWidthInches));
                    break;
                case nameof(StructuralOptions.PostDepthInches):
                    OnPropertyChanged(nameof(PostDepthInches));
                    break;
            }
        }

        private void OpeningsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OpeningParameters.Doors)) OnPropertyChanged(nameof(Doors));
            if (e.PropertyName == nameof(OpeningParameters.Windows)) OnPropertyChanged(nameof(Windows));
        }

        private void ExteriorChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ExteriorParameters.OverhangEave)) OnPropertyChanged(nameof(OverhangEave));
            if (e.PropertyName == nameof(ExteriorParameters.OverhangGable)) OnPropertyChanged(nameof(OverhangGable));
            if (e.PropertyName == nameof(ExteriorParameters.Wainscot)) OnPropertyChanged(nameof(Wainscot));
            if (e.PropertyName == nameof(ExteriorParameters.Cupola)) OnPropertyChanged(nameof(Cupola));
            if (e.PropertyName == nameof(ExteriorParameters.Gutters)) OnPropertyChanged(nameof(Gutters));
        }

        private void AdvancedGeometryChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AdvancedGeometryParameters.Story))
            {
                OnPropertyChanged(nameof(Story));
                OnPropertyChanged(nameof(NumberOfFloors));
                OnPropertyChanged(nameof(FloorHeights));
                OnPropertyChanged(nameof(FloorConnection));
                OnPropertyChanged(nameof(FloorBeamSize));
                OnPropertyChanged(nameof(DefaultFloorHeight));
            }

            if (e.PropertyName == nameof(AdvancedGeometryParameters.CurvedWall)) OnPropertyChanged(nameof(CurvedWall));
            if (e.PropertyName == nameof(AdvancedGeometryParameters.Footprint))
            {
                OnPropertyChanged(nameof(Footprint));
                OnPropertyChanged(nameof(FootprintShape));
                OnPropertyChanged(nameof(FootprintVertices));
                OnPropertyChanged(nameof(FootprintInsetWidth));
                OnPropertyChanged(nameof(FootprintInsetDepth));
            }

            if (e.PropertyName == nameof(AdvancedGeometryParameters.ExpansionJoint)) OnPropertyChanged(nameof(ExpansionJoint));
        }

        private void EngineeringChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(StructuralEngineeringParameters.Structural)) OnPropertyChanged(nameof(Structural));
            if (e.PropertyName == nameof(StructuralEngineeringParameters.IncludeStructuralAnalysis)) OnPropertyChanged(nameof(IncludeStructuralAnalysis));
        }

        private void PorchChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, _frontPorch)) OnPropertyChanged(nameof(FrontPorch));
            if (ReferenceEquals(sender, _backPorch)) OnPropertyChanged(nameof(BackPorch));
            if (ReferenceEquals(sender, _leftPorch)) OnPropertyChanged(nameof(LeftPorch));
            if (ReferenceEquals(sender, _rightPorch)) OnPropertyChanged(nameof(RightPorch));
        }

        // Primary Dimensions
        public double BuildingWidth
        {
            get => Dimensions.BuildingWidth;
            set => Dimensions.BuildingWidth = value;
        }

        public double BuildingLength
        {
            get => Dimensions.BuildingLength;
            set => Dimensions.BuildingLength = value;
        }

        public double EaveHeight
        {
            get => Dimensions.EaveHeight;
            set => Dimensions.EaveHeight = value;
        }

        public double RoofPitchRise
        {
            get => Dimensions.RoofPitchRise;
            set => Dimensions.RoofPitchRise = value;
        }

        public string RoofPitchDisplay => Dimensions.RoofPitchDisplay;

        public double PeakHeight => TrussFactory.GetTrussProfile(TrussType).CalculatePeakHeight(EaveHeight, BuildingWidth, RoofPitchRise);

        public double RoofRise => Dimensions.RoofRise;

        public double RoofAngleDegrees => Dimensions.RoofAngleDegrees;

        public double BaySpacing
        {
            get => Dimensions.BaySpacing;
            set => Dimensions.BaySpacing = value;
        }

        public int NumberOfBays => Dimensions.NumberOfBays;

        public double ActualBaySpacing => Dimensions.ActualBaySpacing;

        public StoryParameters Story
        {
            get => AdvancedGeometry.Story;
            set { AdvancedGeometry.Story = value ?? new StoryParameters(); OnPropertyChanged(); }
        }

        public int NumberOfFloors
        {
            get => Story.NumberOfFloors;
            set { Story.NumberOfFloors = value; OnPropertyChanged(); OnPropertyChanged(nameof(DefaultFloorHeight)); }
        }

        public List<double> FloorHeights
        {
            get => Story.FloorHeights;
            set { Story.FloorHeights = value ?? new List<double>(); OnPropertyChanged(); }
        }

        public double DefaultFloorHeight => NumberOfFloors > 0 ? EaveHeight / NumberOfFloors : EaveHeight;

        public FloorConnectionType FloorConnection
        {
            get => Story.FloorConnection;
            set { Story.FloorConnection = value; OnPropertyChanged(); }
        }

        public string FloorBeamSize
        {
            get => Story.FloorBeamSize;
            set { Story.FloorBeamSize = value; OnPropertyChanged(); }
        }

        // Structural Options
        public string PostSize
        {
            get => StructuralOptions.PostSize;
            set => StructuralOptions.PostSize = value;
        }

        public double PostWidthInches => StructuralOptions.PostWidthInches;

        public double PostDepthInches => StructuralOptions.PostDepthInches;

        public double GirtSpacing
        {
            get => StructuralOptions.GirtSpacing;
            set => StructuralOptions.GirtSpacing = value;
        }

        public double PurlinSpacing
        {
            get => StructuralOptions.PurlinSpacing;
            set => StructuralOptions.PurlinSpacing = value;
        }

        public TrussType TrussType
        {
            get => StructuralOptions.TrussType;
            set => StructuralOptions.TrussType = value;
        }

        public string HeaderStrategy
        {
            get => StructuralOptions.HeaderStrategy;
            set => StructuralOptions.HeaderStrategy = value;
        }

        public string WindRegion
        {
            get => StructuralOptions.WindRegion;
            set => StructuralOptions.WindRegion = value;
        }

        public string SnowRegion
        {
            get => StructuralOptions.SnowRegion;
            set => StructuralOptions.SnowRegion = value;
        }

        // Openings
        public List<DoorOpening> Doors
        {
            get => Openings.Doors;
            set => Openings.Doors = value;
        }

        public List<WindowOpening> Windows
        {
            get => Openings.Windows;
            set => Openings.Windows = value;
        }

        // Porches
        private PorchParameters _frontPorch = new PorchParameters { AttachmentWall = WallSide.Front };
        public PorchParameters FrontPorch
        {
            get => _frontPorch;
            set
            {
                if (_frontPorch != null) _frontPorch.PropertyChanged -= PorchChanged;
                _frontPorch = value ?? new PorchParameters { AttachmentWall = WallSide.Front };
                _frontPorch.PropertyChanged += PorchChanged;
                OnPropertyChanged();
            }
        }

        private PorchParameters _backPorch = new PorchParameters { AttachmentWall = WallSide.Back };
        public PorchParameters BackPorch
        {
            get => _backPorch;
            set
            {
                if (_backPorch != null) _backPorch.PropertyChanged -= PorchChanged;
                _backPorch = value ?? new PorchParameters { AttachmentWall = WallSide.Back };
                _backPorch.PropertyChanged += PorchChanged;
                OnPropertyChanged();
            }
        }

        private PorchParameters _leftPorch = new PorchParameters { AttachmentWall = WallSide.Left };
        public PorchParameters LeftPorch
        {
            get => _leftPorch;
            set
            {
                if (_leftPorch != null) _leftPorch.PropertyChanged -= PorchChanged;
                _leftPorch = value ?? new PorchParameters { AttachmentWall = WallSide.Left };
                _leftPorch.PropertyChanged += PorchChanged;
                OnPropertyChanged();
            }
        }

        private PorchParameters _rightPorch = new PorchParameters { AttachmentWall = WallSide.Right };
        public PorchParameters RightPorch
        {
            get => _rightPorch;
            set
            {
                if (_rightPorch != null) _rightPorch.PropertyChanged -= PorchChanged;
                _rightPorch = value ?? new PorchParameters { AttachmentWall = WallSide.Right };
                _rightPorch.PropertyChanged += PorchChanged;
                OnPropertyChanged();
            }
        }

        public PorchParameters[] AllPorches => new[] { FrontPorch, BackPorch, LeftPorch, RightPorch };

        // Exterior Details
        public double OverhangEave
        {
            get => Exterior.OverhangEave;
            set => Exterior.OverhangEave = value;
        }

        public double OverhangGable
        {
            get => Exterior.OverhangGable;
            set => Exterior.OverhangGable = value;
        }

        public WainscotParameters Wainscot
        {
            get => Exterior.Wainscot;
            set => Exterior.Wainscot = value;
        }

        public CupolaParameters Cupola
        {
            get => Exterior.Cupola;
            set => Exterior.Cupola = value;
        }

        public GutterParameters Gutters
        {
            get => Exterior.Gutters;
            set => Exterior.Gutters = value;
        }

        // Lean-Tos
        private List<LeanToParameters> _leanTos = new List<LeanToParameters>();
        public List<LeanToParameters> LeanTos
        {
            get => _leanTos;
            set { _leanTos = value ?? new List<LeanToParameters>(); OnPropertyChanged(); }
        }

        // Interior Features
        private HorseStallParameters _horseStalls = new HorseStallParameters();
        public HorseStallParameters HorseStalls
        {
            get => _horseStalls;
            set { _horseStalls = value ?? new HorseStallParameters(); OnPropertyChanged(); }
        }

        private LoftParameters _loft = new LoftParameters();
        public LoftParameters Loft
        {
            get => _loft;
            set { _loft = value ?? new LoftParameters(); OnPropertyChanged(); }
        }

        private InteriorPartitionParameters _partitions = new InteriorPartitionParameters();
        public InteriorPartitionParameters Partitions
        {
            get => _partitions;
            set { _partitions = value ?? new InteriorPartitionParameters(); OnPropertyChanged(); }
        }

        private WorkshopParameters _workshop = new WorkshopParameters();
        public WorkshopParameters Workshop
        {
            get => _workshop;
            set { _workshop = value ?? new WorkshopParameters(); OnPropertyChanged(); }
        }

        // Industry Specialization
        private BuildingSpecialtyType _specialtyType = BuildingSpecialtyType.Standard;
        public BuildingSpecialtyType SpecialtyType
        {
            get => _specialtyType;
            set { _specialtyType = value; OnPropertyChanged(); }
        }

        private DairyBarnParameters _dairyBarn = new DairyBarnParameters();
        public DairyBarnParameters DairyBarn
        {
            get => _dairyBarn;
            set { _dairyBarn = value ?? new DairyBarnParameters(); OnPropertyChanged(); }
        }

        private EquipmentStorageParameters _equipmentStorage = new EquipmentStorageParameters();
        public EquipmentStorageParameters EquipmentStorage
        {
            get => _equipmentStorage;
            set { _equipmentStorage = value ?? new EquipmentStorageParameters(); OnPropertyChanged(); }
        }

        private VentilationParameters _ventilation = new VentilationParameters();
        public VentilationParameters Ventilation
        {
            get => _ventilation;
            set { _ventilation = value ?? new VentilationParameters(); OnPropertyChanged(); }
        }

        private DrainageParameters _drainage = new DrainageParameters();
        public DrainageParameters Drainage
        {
            get => _drainage;
            set { _drainage = value ?? new DrainageParameters(); OnPropertyChanged(); }
        }

        private GrainStorageParameters _grainStorage = new GrainStorageParameters();
        public GrainStorageParameters GrainStorage
        {
            get => _grainStorage;
            set { _grainStorage = value ?? new GrainStorageParameters(); OnPropertyChanged(); }
        }

        private MachineryBuildingParameters _machineryBuilding = new MachineryBuildingParameters();
        public MachineryBuildingParameters MachineryBuilding
        {
            get => _machineryBuilding;
            set { _machineryBuilding = value ?? new MachineryBuildingParameters(); OnPropertyChanged(); }
        }

        // Structural Engineering
        public StructuralParameters Structural
        {
            get => Engineering.Structural;
            set => Engineering.Structural = value;
        }

        public bool IncludeStructuralAnalysis
        {
            get => Engineering.IncludeStructuralAnalysis;
            set => Engineering.IncludeStructuralAnalysis = value;
        }

        // Advanced Geometry
        public CurvedWallParameters CurvedWall
        {
            get => AdvancedGeometry.CurvedWall;
            set => AdvancedGeometry.CurvedWall = value;
        }

        public FootprintParameters Footprint
        {
            get => AdvancedGeometry.Footprint;
            set => AdvancedGeometry.Footprint = value;
        }

        public FootprintShape FootprintShape
        {
            get => Footprint.Shape;
            set { Footprint.Shape = value; OnPropertyChanged(); }
        }

        public List<FootprintVertex> FootprintVertices
        {
            get => Footprint.Vertices;
            set { Footprint.Vertices = value ?? new List<FootprintVertex>(); OnPropertyChanged(); }
        }

        public double FootprintInsetWidth
        {
            get => Footprint.InsetWidth;
            set { Footprint.InsetWidth = value; OnPropertyChanged(); }
        }

        public double FootprintInsetDepth
        {
            get => Footprint.InsetDepth;
            set { Footprint.InsetDepth = value; OnPropertyChanged(); }
        }

        public ExpansionJointParameters ExpansionJoint
        {
            get => AdvancedGeometry.ExpansionJoint;
            set => AdvancedGeometry.ExpansionJoint = value;
        }

        // Output Options
        public bool GeneratePlan { get; set; } = true;
        public bool GenerateFront { get; set; } = true;
        public bool GenerateSide { get; set; } = true;
        public bool Generate3D { get; set; } = true;
        public bool AddDimensions { get; set; } = true;

        // Validation
        public (bool IsValid, string Error) Validate()
        {
            var (dimValid, dimError) = Dimensions.Validate();
            if (!dimValid) return (false, dimError);

            var (structValid, structError) = StructuralOptions.Validate();
            if (!structValid) return (false, structError);

            var (extValid, extError) = Exterior.Validate();
            if (!extValid) return (false, extError);

            var (advValid, advError) = AdvancedGeometry.Validate(this);
            if (!advValid) return (false, advError);

            var (openValid, openError) = Openings.Validate(this);
            if (!openValid) return (false, openError);

            foreach (var porch in AllPorches)
            {
                var (pValid, pError) = porch.Validate(this);
                if (!pValid) return (false, pError);
            }

            var usedPorchWalls = new HashSet<WallSide>();
            foreach (var porch in AllPorches)
            {
                if (!porch.IsEnabled) continue;
                if (!usedPorchWalls.Add(porch.AttachmentWall))
                    return (false, $"Multiple porches on the {porch.AttachmentWall} wall are not allowed.");
            }

            foreach (var leanTo in LeanTos)
            {
                var (ltValid, ltError) = leanTo.Validate(this);
                if (!ltValid) return (false, ltError);
            }

            var usedWalls = new HashSet<WallSide>();
            foreach (var lt in LeanTos)
            {
                if (!lt.Enabled) continue;
                if (!usedWalls.Add(lt.AttachmentWall))
                    return (false, $"Multiple lean-tos on the {lt.AttachmentWall} wall are not allowed.");
            }

            var geometry = new BarnGeometry(this);
            var conflicts = Utils.OpeningValidator.ValidateOpenings(this, geometry);
            if (conflicts.Count > 0)
                return (false, conflicts[0]);

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

            if (DairyBarn != null)
            {
                var (dairyValid, dairyError) = DairyBarn.Validate(this);
                if (!dairyValid) return (false, dairyError);
            }

            if (EquipmentStorage != null)
            {
                var (equipValid, equipError) = EquipmentStorage.Validate(this);
                if (!equipValid) return (false, equipError);
            }

            if (Ventilation != null)
            {
                var (ventValid, ventError) = Ventilation.Validate();
                if (!ventValid) return (false, ventError);
            }

            if (Drainage != null)
            {
                var (drainValid, drainError) = Drainage.Validate(this);
                if (!drainValid) return (false, drainError);
            }

            if (GrainStorage != null)
            {
                var (grainValid, grainError) = GrainStorage.Validate(this);
                if (!grainValid) return (false, grainError);
            }

            if (MachineryBuilding != null)
            {
                var (machValid, machError) = MachineryBuilding.Validate(this);
                if (!machValid) return (false, machError);
            }

            var (engValid, engError) = Engineering.Validate();
            if (!engValid) return (false, engError);

            return (true, null);
        }

        public List<double> GetResolvedFloorHeights()
        {
            return Story.ResolveHeights(EaveHeight);
        }

        public List<double> GetSuggestedExpansionJointLocations()
        {
            return AdvancedGeometry.GetSuggestedExpansionJointLocations(BuildingLength);
        }

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
}
