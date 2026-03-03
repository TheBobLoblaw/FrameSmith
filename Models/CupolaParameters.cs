using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public class CupolaParameters : INotifyPropertyChanged
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

        private double _size = 24.0;
        /// <summary>Base dimension in inches (18-48")</summary>
        public double Size
        {
            get => _size;
            set { _size = value; OnPropertyChanged(); }
        }

        private int _count = 1;
        public int Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(); }
        }

        private double _spacing = 0.0;
        /// <summary>Spacing between cupolas (0 = auto-calculated)</summary>
        public double Spacing
        {
            get => _spacing;
            set { _spacing = value; OnPropertyChanged(); }
        }

        private bool _isVented = true;
        public bool IsVented
        {
            get => _isVented;
            set { _isVented = value; OnPropertyChanged(); }
        }

        private bool _hasWindows = false;
        public bool HasWindows
        {
            get => _hasWindows;
            set { _hasWindows = value; OnPropertyChanged(); }
        }
    }
}
