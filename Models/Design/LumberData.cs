using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Models.Design
{
    public enum LumberSpecies { DouglasFir, SouthernPine, SprucePineFir, Hem_Fir }
    public enum LumberGradeType { Select, No1, No2, No3, Stud }

    public class LumberGrade
    {
        public LumberSpecies Species { get; set; } = LumberSpecies.DouglasFir;
        public LumberGradeType Grade { get; set; } = LumberGradeType.No2;

        /// <summary>Allowable bending stress Fb (psi)</summary>
        public double Fb => GetFb();
        /// <summary>Allowable tension parallel Ft (psi)</summary>
        public double Ft => GetFt();
        /// <summary>Allowable shear Fv (psi)</summary>
        public double Fv => GetFv();
        /// <summary>Allowable compression parallel Fc (psi)</summary>
        public double Fc => GetFc();
        /// <summary>Allowable compression perp Fc_perp (psi)</summary>
        public double FcPerp => GetFcPerp();
        /// <summary>Modulus of Elasticity E (psi)</summary>
        public double E => GetE();
        /// <summary>Minimum E for stability Emin (psi)</summary>
        public double Emin => GetEmin();
        /// <summary>Specific gravity</summary>
        public double G => GetSpecificGravity();

        public string DisplayName => $"{Species} #{(int)Grade + 1}";

        private double GetFb()
        {
            // NDS Supplement Table 4A (2x-4x wide)
            if (Species == LumberSpecies.DouglasFir)
                return Grade == LumberGradeType.No1 ? 1000 : Grade == LumberGradeType.No2 ? 900 : 525;
            if (Species == LumberSpecies.SouthernPine)
                return Grade == LumberGradeType.No1 ? 1100 : Grade == LumberGradeType.No2 ? 850 : 500;
            return 875; // SPF #2
        }

        private double GetFt()
        {
            if (Species == LumberSpecies.DouglasFir)
                return Grade == LumberGradeType.No1 ? 675 : Grade == LumberGradeType.No2 ? 575 : 325;
            if (Species == LumberSpecies.SouthernPine)
                return Grade == LumberGradeType.No1 ? 725 : Grade == LumberGradeType.No2 ? 550 : 300;
            return 450;
        }

        private double GetFv()
        {
            if (Species == LumberSpecies.DouglasFir) return 180;
            if (Species == LumberSpecies.SouthernPine) return 175;
            return 135;
        }

        private double GetFc()
        {
            if (Species == LumberSpecies.DouglasFir)
                return Grade == LumberGradeType.No1 ? 1500 : Grade == LumberGradeType.No2 ? 1350 : 775;
            if (Species == LumberSpecies.SouthernPine)
                return Grade == LumberGradeType.No1 ? 1650 : Grade == LumberGradeType.No2 ? 1450 : 825;
            return 1150;
        }

        private double GetFcPerp()
        {
            if (Species == LumberSpecies.DouglasFir) return 625;
            if (Species == LumberSpecies.SouthernPine) return 565;
            return 425;
        }

        private double GetE()
        {
            if (Species == LumberSpecies.DouglasFir)
                return Grade == LumberGradeType.No1 ? 1700000 : 1600000;
            if (Species == LumberSpecies.SouthernPine)
                return Grade == LumberGradeType.No1 ? 1800000 : 1600000;
            return 1400000;
        }

        private double GetEmin()
        {
            if (Species == LumberSpecies.DouglasFir)
                return Grade == LumberGradeType.No1 ? 620000 : 580000;
            if (Species == LumberSpecies.SouthernPine)
                return Grade == LumberGradeType.No1 ? 660000 : 580000;
            return 510000;
        }

        private double GetSpecificGravity()
        {
            if (Species == LumberSpecies.DouglasFir) return 0.50;
            if (Species == LumberSpecies.SouthernPine) return 0.55;
            return 0.42;
        }
    }

    public class LumberSize
    {
        public double NominalWidth { get; set; }
        public double NominalDepth { get; set; }
        public double ActualWidth { get; set; }
        public double ActualDepth { get; set; }
        public int Plies { get; set; } = 1;

        /// <summary>Section modulus S = b*d²/6</summary>
        public double SectionModulus => (ActualWidth * Plies) * ActualDepth * ActualDepth / 6.0;
        /// <summary>Moment of inertia I = b*d³/12</summary>
        public double MomentOfInertia => (ActualWidth * Plies) * ActualDepth * ActualDepth * ActualDepth / 12.0;
        /// <summary>Cross-sectional area A = b*d</summary>
        public double Area => (ActualWidth * Plies) * ActualDepth;
        /// <summary>Display name</summary>
        public string Name => Plies > 1 ? $"({Plies}) {NominalWidth}x{NominalDepth}" : $"{NominalWidth}x{NominalDepth}";

        public static LumberSize[] StandardSizes = new[]
        {
            new LumberSize { NominalWidth = 4, NominalDepth = 4, ActualWidth = 3.5, ActualDepth = 3.5 },
            new LumberSize { NominalWidth = 4, NominalDepth = 6, ActualWidth = 3.5, ActualDepth = 5.5 },
            new LumberSize { NominalWidth = 6, NominalDepth = 6, ActualWidth = 5.5, ActualDepth = 5.5 },
            new LumberSize { NominalWidth = 6, NominalDepth = 8, ActualWidth = 5.5, ActualDepth = 7.25 },
            new LumberSize { NominalWidth = 8, NominalDepth = 8, ActualWidth = 7.25, ActualDepth = 7.25 },
        };

        public static LumberSize[] BeamSizes = new[]
        {
            new LumberSize { NominalWidth = 2, NominalDepth = 6, ActualWidth = 1.5, ActualDepth = 5.5 },
            new LumberSize { NominalWidth = 2, NominalDepth = 8, ActualWidth = 1.5, ActualDepth = 7.25 },
            new LumberSize { NominalWidth = 2, NominalDepth = 10, ActualWidth = 1.5, ActualDepth = 9.25 },
            new LumberSize { NominalWidth = 2, NominalDepth = 12, ActualWidth = 1.5, ActualDepth = 11.25 },
        };
    }

    public enum LateralSupport { None, EdgeOnly, Continuous, Bridging }
}
