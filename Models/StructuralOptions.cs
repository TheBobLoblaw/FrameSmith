using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public class StructuralOptions : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private string _postSize = "6x6";
        public string PostSize
        {
            get => _postSize;
            set
            {
                _postSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PostWidthInches));
                OnPropertyChanged(nameof(PostDepthInches));
            }
        }

        public double PostWidthInches
        {
            get
            {
                var parts = PostSize.Split('x');
                return parts.Length >= 1 && double.TryParse(parts[0], out double w) ? w : 6.0;
            }
        }

        public double PostDepthInches
        {
            get
            {
                var parts = PostSize.Split('x');
                return parts.Length >= 2 && double.TryParse(parts[1], out double d) ? d : PostWidthInches;
            }
        }

        private double _girtSpacing = 24.0;
        public double GirtSpacing
        {
            get => _girtSpacing;
            set { _girtSpacing = value; OnPropertyChanged(); }
        }

        private double _purlinSpacing = 24.0;
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

        private string _headerStrategy = "Auto";
        public string HeaderStrategy
        {
            get => _headerStrategy;
            set { _headerStrategy = string.IsNullOrWhiteSpace(value) ? "Auto" : value.Trim(); OnPropertyChanged(); }
        }

        private string _windRegion = "Default";
        public string WindRegion
        {
            get => _windRegion;
            set { _windRegion = string.IsNullOrWhiteSpace(value) ? "Default" : value.Trim(); OnPropertyChanged(); }
        }

        private string _snowRegion = "Default";
        public string SnowRegion
        {
            get => _snowRegion;
            set { _snowRegion = string.IsNullOrWhiteSpace(value) ? "Default" : value.Trim(); OnPropertyChanged(); }
        }

        public (bool IsValid, string Error) Validate() => (true, null);
    }
}
