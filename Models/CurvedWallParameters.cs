using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public enum CurvedWallMode
    {
        ArcLengthDriven,
        ChordDriven
    }

    public class CurvedWallParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool _enabled = false;
        public bool Enabled
        {
            get => _enabled;
            set { _enabled = value; OnPropertyChanged(); }
        }

        private double _radius = 120.0;
        public double Radius
        {
            get => _radius;
            set { _radius = value; OnPropertyChanged(); }
        }

        private double _arcAngleDegrees = 45.0;
        public double ArcAngleDegrees
        {
            get => _arcAngleDegrees;
            set { _arcAngleDegrees = value; OnPropertyChanged(); }
        }

        private CurvedWallMode _mode = CurvedWallMode.ArcLengthDriven;
        public CurvedWallMode Mode
        {
            get => _mode;
            set { _mode = value; OnPropertyChanged(); }
        }
    }
}
