using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public enum FootprintShape
    {
        Rectangle,
        LShape,
        TShape,
        UShape,
        CustomPolygon
    }

    public class FootprintVertex : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private double _x;
        public double X
        {
            get => _x;
            set { _x = value; OnPropertyChanged(); }
        }

        private double _y;
        public double Y
        {
            get => _y;
            set { _y = value; OnPropertyChanged(); }
        }
    }

    public class FootprintParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private double _insetWidth = 10.0;
        private double _insetDepth = 10.0;
        private FootprintShape _shape = FootprintShape.Rectangle;
        private List<FootprintVertex> _vertices = new List<FootprintVertex>();

        public FootprintShape Shape
        {
            get => _shape;
            set { _shape = value; OnPropertyChanged(); }
        }

        public List<FootprintVertex> Vertices
        {
            get => _vertices;
            set { _vertices = value ?? new List<FootprintVertex>(); OnPropertyChanged(); }
        }

        public double InsetWidth
        {
            get => _insetWidth;
            set
            {
                _insetWidth = Math.Max(1.0, value);
                OnPropertyChanged();
            }
        }

        public double InsetDepth
        {
            get => _insetDepth;
            set
            {
                _insetDepth = Math.Max(1.0, value);
                OnPropertyChanged();
            }
        }
    }
}
