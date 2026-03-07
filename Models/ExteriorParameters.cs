using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public class ExteriorParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private double _overhangEave = 1.0;
        public double OverhangEave
        {
            get => _overhangEave;
            set { _overhangEave = value; OnPropertyChanged(); }
        }

        private double _overhangGable = 1.0;
        public double OverhangGable
        {
            get => _overhangGable;
            set { _overhangGable = value; OnPropertyChanged(); }
        }

        private WainscotParameters _wainscot = new WainscotParameters();
        public WainscotParameters Wainscot
        {
            get => _wainscot;
            set
            {
                if (_wainscot != null) _wainscot.PropertyChanged -= ChildChanged;
                _wainscot = value ?? new WainscotParameters();
                _wainscot.PropertyChanged += ChildChanged;
                OnPropertyChanged();
            }
        }

        private CupolaParameters _cupola = new CupolaParameters();
        public CupolaParameters Cupola
        {
            get => _cupola;
            set
            {
                if (_cupola != null) _cupola.PropertyChanged -= ChildChanged;
                _cupola = value ?? new CupolaParameters();
                _cupola.PropertyChanged += ChildChanged;
                OnPropertyChanged();
            }
        }

        private GutterParameters _gutters = new GutterParameters();
        public GutterParameters Gutters
        {
            get => _gutters;
            set
            {
                if (_gutters != null) _gutters.PropertyChanged -= ChildChanged;
                _gutters = value ?? new GutterParameters();
                _gutters.PropertyChanged += ChildChanged;
                OnPropertyChanged();
            }
        }

        public ExteriorParameters()
        {
            _wainscot.PropertyChanged += ChildChanged;
            _cupola.PropertyChanged += ChildChanged;
            _gutters.PropertyChanged += ChildChanged;
        }

        private void ChildChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ReferenceEquals(sender, _wainscot)) OnPropertyChanged(nameof(Wainscot));
            if (ReferenceEquals(sender, _cupola)) OnPropertyChanged(nameof(Cupola));
            if (ReferenceEquals(sender, _gutters)) OnPropertyChanged(nameof(Gutters));
        }

        public (bool IsValid, string Error) Validate()
        {
            if (Cupola?.Count < 0) return (false, "Cupola count cannot be negative.");
            return (true, null);
        }
    }
}
