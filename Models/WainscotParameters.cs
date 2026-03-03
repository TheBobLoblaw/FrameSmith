using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public class WainscotParameters : INotifyPropertyChanged
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

        private double _height = 3.5;
        /// <summary>Wainscot height in feet (typical 3-4')</summary>
        public double Height
        {
            get => _height;
            set { _height = value; OnPropertyChanged(); }
        }

        private WainscotMaterial _material = WainscotMaterial.BoardAndBatten;
        public WainscotMaterial Material
        {
            get => _material;
            set { _material = value; OnPropertyChanged(); }
        }

        /// <summary>Which walls get wainscot: [0]=Front, [1]=Back, [2]=Left, [3]=Right</summary>
        public bool[] Walls { get; set; } = new bool[] { true, true, true, true };
    }

    public enum WainscotMaterial { BoardAndBatten, Metal, Vinyl, Wood }
}
