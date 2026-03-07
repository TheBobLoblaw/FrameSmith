using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public class DimensionParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private double _buildingWidth = 30.0;
        public double BuildingWidth
        {
            get => _buildingWidth;
            set
            {
                _buildingWidth = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RoofRise));
                OnPropertyChanged(nameof(RoofAngleDegrees));
            }
        }

        private double _buildingLength = 40.0;
        public double BuildingLength
        {
            get => _buildingLength;
            set
            {
                _buildingLength = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NumberOfBays));
                OnPropertyChanged(nameof(ActualBaySpacing));
            }
        }

        private double _eaveHeight = 12.0;
        public double EaveHeight
        {
            get => _eaveHeight;
            set
            {
                _eaveHeight = value;
                OnPropertyChanged();
            }
        }

        private double _roofPitchRise = 4.0;
        public double RoofPitchRise
        {
            get => _roofPitchRise;
            set
            {
                _roofPitchRise = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(RoofPitchDisplay));
                OnPropertyChanged(nameof(RoofRise));
                OnPropertyChanged(nameof(RoofAngleDegrees));
            }
        }

        public string RoofPitchDisplay => $"{RoofPitchRise}/12";

        public double RoofRise => (BuildingWidth / 2.0) * (RoofPitchRise / 12.0);

        public double RoofAngleDegrees => Math.Atan(RoofPitchRise / 12.0) * (180.0 / Math.PI);

        private double _baySpacing = 10.0;
        public double BaySpacing
        {
            get => _baySpacing;
            set
            {
                _baySpacing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NumberOfBays));
                OnPropertyChanged(nameof(ActualBaySpacing));
            }
        }

        public int NumberOfBays => BaySpacing <= 0 ? 1 : Math.Max(1, (int)Math.Round(BuildingLength / BaySpacing));

        public double ActualBaySpacing => BuildingLength / NumberOfBays;

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

            return (true, null);
        }
    }
}
