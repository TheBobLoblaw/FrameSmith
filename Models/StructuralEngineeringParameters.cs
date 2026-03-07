using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public class StructuralEngineeringParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private StructuralParameters _structural = new StructuralParameters();
        public StructuralParameters Structural
        {
            get => _structural;
            set
            {
                if (_structural != null) _structural.PropertyChanged -= ChildChanged;
                _structural = value ?? new StructuralParameters();
                _structural.PropertyChanged += ChildChanged;
                OnPropertyChanged();
            }
        }

        private bool _includeStructuralAnalysis;
        public bool IncludeStructuralAnalysis
        {
            get => _includeStructuralAnalysis;
            set { _includeStructuralAnalysis = value; OnPropertyChanged(); }
        }

        public StructuralEngineeringParameters()
        {
            _structural.PropertyChanged += ChildChanged;
        }

        private void ChildChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(Structural));
        }

        public (bool IsValid, string Error) Validate() => (true, null);
    }
}
