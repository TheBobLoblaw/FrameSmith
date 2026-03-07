using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public enum ExpansionJointType
    {
        SlipPlate,
        DoublePost,
        IsolationGap
    }

    public class ExpansionJointParameters : INotifyPropertyChanged
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

        private List<double> _locations = new List<double>();
        public List<double> Locations
        {
            get => _locations;
            set { _locations = value ?? new List<double>(); OnPropertyChanged(); }
        }

        private double _gapWidth = 0.5;
        public double GapWidth
        {
            get => _gapWidth;
            set { _gapWidth = value; OnPropertyChanged(); }
        }

        private ExpansionJointType _jointType = ExpansionJointType.SlipPlate;
        public ExpansionJointType JointType
        {
            get => _jointType;
            set { _jointType = value; OnPropertyChanged(); }
        }
    }
}
