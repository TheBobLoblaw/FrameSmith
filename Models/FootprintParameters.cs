using System;
using System.Collections.Generic;

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

    public class FootprintVertex
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class FootprintParameters
    {
        private double _insetWidth = 10.0;
        private double _insetDepth = 10.0;

        public FootprintShape Shape { get; set; } = FootprintShape.Rectangle;

        public List<FootprintVertex> Vertices { get; set; } = new List<FootprintVertex>();

        public double InsetWidth
        {
            get => _insetWidth;
            set => _insetWidth = Math.Max(1.0, value);
        }

        public double InsetDepth
        {
            get => _insetDepth;
            set => _insetDepth = Math.Max(1.0, value);
        }
    }
}
