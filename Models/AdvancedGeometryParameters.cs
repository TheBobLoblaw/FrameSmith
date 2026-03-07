using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public class AdvancedGeometryParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private StoryParameters _story = new StoryParameters();
        public StoryParameters Story
        {
            get => _story;
            set
            {
                if (_story != null) _story.PropertyChanged -= ChildChanged;
                _story = value ?? new StoryParameters();
                _story.PropertyChanged += ChildChanged;
                OnPropertyChanged();
            }
        }

        private CurvedWallParameters _curvedWall = new CurvedWallParameters();
        public CurvedWallParameters CurvedWall
        {
            get => _curvedWall;
            set
            {
                if (_curvedWall != null) _curvedWall.PropertyChanged -= ChildChanged;
                _curvedWall = value ?? new CurvedWallParameters();
                _curvedWall.PropertyChanged += ChildChanged;
                OnPropertyChanged();
            }
        }

        private FootprintParameters _footprint = new FootprintParameters();
        public FootprintParameters Footprint
        {
            get => _footprint;
            set
            {
                if (_footprint != null) _footprint.PropertyChanged -= ChildChanged;
                _footprint = value ?? new FootprintParameters();
                _footprint.PropertyChanged += ChildChanged;
                OnPropertyChanged();
            }
        }

        private ExpansionJointParameters _expansionJoint = new ExpansionJointParameters();
        public ExpansionJointParameters ExpansionJoint
        {
            get => _expansionJoint;
            set
            {
                if (_expansionJoint != null) _expansionJoint.PropertyChanged -= ChildChanged;
                _expansionJoint = value ?? new ExpansionJointParameters();
                _expansionJoint.PropertyChanged += ChildChanged;
                OnPropertyChanged();
            }
        }

        public AdvancedGeometryParameters()
        {
            _story.PropertyChanged += ChildChanged;
            _curvedWall.PropertyChanged += ChildChanged;
            _footprint.PropertyChanged += ChildChanged;
            _expansionJoint.PropertyChanged += ChildChanged;
        }

        private void ChildChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, _story)) OnPropertyChanged(nameof(Story));
            if (ReferenceEquals(sender, _curvedWall)) OnPropertyChanged(nameof(CurvedWall));
            if (ReferenceEquals(sender, _footprint)) OnPropertyChanged(nameof(Footprint));
            if (ReferenceEquals(sender, _expansionJoint)) OnPropertyChanged(nameof(ExpansionJoint));
        }

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (Story.NumberOfFloors < 1) return (false, "Number of floors must be at least 1.");

            var floorHeights = barn.GetResolvedFloorHeights();
            if (floorHeights.Exists(h => h <= 0))
                return (false, "All floor heights must be positive.");
            if (floorHeights.Sum() > barn.EaveHeight + 0.01)
                return (false, $"Total floor heights ({floorHeights.Sum():F2}') cannot exceed eave height ({barn.EaveHeight:F2}').");

            if (CurvedWall.Enabled)
            {
                if (CurvedWall.Radius <= barn.BuildingWidth / 2.0)
                    return (false, "Curved wall radius must be greater than half the building width.");
                if (CurvedWall.ArcAngleDegrees <= 0 || CurvedWall.ArcAngleDegrees >= 180)
                    return (false, "Curved wall arc angle must be between 0 and 180 degrees.");
            }

            if (Footprint.Shape == FootprintShape.CustomPolygon)
            {
                if (Footprint.Vertices == null || Footprint.Vertices.Count < 3)
                    return (false, "Custom polygon footprint requires at least 3 vertices.");
            }

            if (ExpansionJoint.Enabled)
            {
                if (ExpansionJoint.GapWidth <= 0)
                    return (false, "Expansion joint gap width must be positive.");
                if (ExpansionJoint.Locations.Count == 0 && barn.BuildingLength <= 120.0)
                    return (false, "At least one expansion joint location is required when expansion joints are enabled.");
                if (ExpansionJoint.Locations.Exists(l => l <= 0 || l >= barn.BuildingLength))
                    return (false, "Expansion joint locations must fall inside the building length.");
            }

            return (true, null);
        }

        public List<double> GetSuggestedExpansionJointLocations(double buildingLength)
        {
            var locations = new List<double>();
            if (buildingLength <= 120.0)
                return locations;

            int segments = (int)System.Math.Ceiling(buildingLength / 120.0);
            double segmentLength = buildingLength / segments;
            for (int i = 1; i < segments; i++)
            {
                locations.Add(i * segmentLength);
            }

            return locations;
        }
    }
}
