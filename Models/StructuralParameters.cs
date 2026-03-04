using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PoleBarnGenerator.Models.Loads;
using PoleBarnGenerator.Models.Design;

namespace PoleBarnGenerator.Models
{
    /// <summary>
    /// All structural engineering input parameters.
    /// Bound to the Structural tab in the UI.
    /// </summary>
    public class StructuralParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        // ─── Wind Parameters ───
        private double _basicWindSpeed = 115.0;
        public double BasicWindSpeed
        {
            get => _basicWindSpeed;
            set { _basicWindSpeed = value; OnPropertyChanged(); }
        }

        private ExposureCategory _exposureCategory = ExposureCategory.C;
        public ExposureCategory ExposureCategory
        {
            get => _exposureCategory;
            set { _exposureCategory = value; OnPropertyChanged(); }
        }

        private WindImportanceFactor _windImportance = WindImportanceFactor.II;
        public WindImportanceFactor WindImportance
        {
            get => _windImportance;
            set { _windImportance = value; OnPropertyChanged(); }
        }

        // ─── Snow Parameters ───
        private double _groundSnowLoad = 25.0;
        public double GroundSnowLoad
        {
            get => _groundSnowLoad;
            set { _groundSnowLoad = value; OnPropertyChanged(); }
        }

        private ExposureCondition _snowExposure = ExposureCondition.PartiallyExposed;
        public ExposureCondition SnowExposure
        {
            get => _snowExposure;
            set { _snowExposure = value; OnPropertyChanged(); }
        }

        private ThermalCondition _thermalCondition = ThermalCondition.Unheated;
        public ThermalCondition ThermalCondition
        {
            get => _thermalCondition;
            set { _thermalCondition = value; OnPropertyChanged(); }
        }

        private bool _rainOnSnowZone = false;
        public bool RainOnSnowZone
        {
            get => _rainOnSnowZone;
            set { _rainOnSnowZone = value; OnPropertyChanged(); }
        }

        // ─── Building Use ───
        private BuildingUse _buildingUse = BuildingUse.Agricultural;
        public BuildingUse BuildingUse
        {
            get => _buildingUse;
            set { _buildingUse = value; OnPropertyChanged(); }
        }

        // ─── Materials ───
        private RoofingMaterial _roofMaterial = RoofingMaterial.MetalPanel;
        public RoofingMaterial RoofMaterial
        {
            get => _roofMaterial;
            set { _roofMaterial = value; OnPropertyChanged(); }
        }

        private SidingMaterial _sidingMaterial = SidingMaterial.MetalPanel;
        public SidingMaterial SidingMaterial
        {
            get => _sidingMaterial;
            set { _sidingMaterial = value; OnPropertyChanged(); }
        }

        private bool _hasInsulation = false;
        public bool HasInsulation
        {
            get => _hasInsulation;
            set { _hasInsulation = value; OnPropertyChanged(); }
        }

        private bool _hasCeiling = false;
        public bool HasCeiling
        {
            get => _hasCeiling;
            set { _hasCeiling = value; OnPropertyChanged(); }
        }

        // ─── Lumber Grades ───
        private LumberSpecies _postSpecies = LumberSpecies.DouglasFir;
        public LumberSpecies PostSpecies
        {
            get => _postSpecies;
            set { _postSpecies = value; OnPropertyChanged(); }
        }

        private LumberGradeType _postGrade = LumberGradeType.No2;
        public LumberGradeType PostGrade
        {
            get => _postGrade;
            set { _postGrade = value; OnPropertyChanged(); }
        }

        private LumberSpecies _beamSpecies = LumberSpecies.DouglasFir;
        public LumberSpecies BeamSpecies
        {
            get => _beamSpecies;
            set { _beamSpecies = value; OnPropertyChanged(); }
        }

        private LumberGradeType _beamGrade = LumberGradeType.No2;
        public LumberGradeType BeamGrade
        {
            get => _beamGrade;
            set { _beamGrade = value; OnPropertyChanged(); }
        }

        // ─── Soil ───
        private double _soilBearingCapacity = 2000;
        public double SoilBearingCapacity
        {
            get => _soilBearingCapacity;
            set { _soilBearingCapacity = value; OnPropertyChanged(); }
        }

        private double _frostDepth = 36;
        public double FrostDepth
        {
            get => _frostDepth;
            set { _frostDepth = value; OnPropertyChanged(); }
        }

        private double _soilFrictionAngle = 30;
        public double SoilFrictionAngle
        {
            get => _soilFrictionAngle;
            set { _soilFrictionAngle = value; OnPropertyChanged(); }
        }

        // ─── Helper constructors ───
        public WindParameters ToWindParameters() => new WindParameters
        {
            BasicWindSpeed = BasicWindSpeed,
            Exposure = ExposureCategory,
            ImportanceFactor = WindImportance
        };

        public SnowParameters ToSnowParameters() => new SnowParameters
        {
            GroundSnowLoad = GroundSnowLoad,
            Exposure = SnowExposure,
            Thermal = ThermalCondition,
            RainOnSnowZone = RainOnSnowZone
        };

        public MaterialProperties ToMaterialProperties() => new MaterialProperties
        {
            RoofMaterial = RoofMaterial,
            SidingMaterial = SidingMaterial,
            HasInsulation = HasInsulation,
            HasCeiling = HasCeiling
        };

        public SoilProperties ToSoilProperties() => new SoilProperties
        {
            BearingCapacity = SoilBearingCapacity,
            FrostDepth = FrostDepth,
            FrictionAngle = SoilFrictionAngle
        };

        public LumberGrade GetPostGrade() => new LumberGrade { Species = PostSpecies, Grade = PostGrade };
        public LumberGrade GetBeamGrade() => new LumberGrade { Species = BeamSpecies, Grade = BeamGrade };
    }
}
