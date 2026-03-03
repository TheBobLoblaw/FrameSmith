using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public class GutterParameters : INotifyPropertyChanged
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

        /// <summary>Which eaves get gutters: [0]=Front, [1]=Back, [2]=Left, [3]=Right</summary>
        public bool[] Eaves { get; set; } = new bool[] { true, true, true, true };

        public List<DownspoutLocation> Downspouts { get; set; } = new List<DownspoutLocation>();

        private GutterStyle _style = GutterStyle.KStyle;
        public GutterStyle Style
        {
            get => _style;
            set { _style = value; OnPropertyChanged(); }
        }
    }

    public enum GutterStyle { KStyle, HalfRound }

    public class DownspoutLocation
    {
        public WallSide Wall { get; set; }
        public double Position { get; set; } // Distance from wall start in feet
    }
}
