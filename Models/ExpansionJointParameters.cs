using System.Collections.Generic;

namespace PoleBarnGenerator.Models
{
    public enum ExpansionJointType
    {
        SlipPlate,
        DoublePost,
        IsolationGap
    }

    public class ExpansionJointParameters
    {
        public bool Enabled { get; set; } = false;
        public List<double> Locations { get; set; } = new List<double>();
        public double GapWidth { get; set; } = 0.5;
        public ExpansionJointType JointType { get; set; } = ExpansionJointType.SlipPlate;
    }
}
