using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Models.Design
{
    public class BeamResult
    {
        public LumberSize RequiredSize { get; set; }
        public double MomentCapacityRatio { get; set; }
        public double ShearCapacityRatio { get; set; }
        public double DeflectionRatio { get; set; }
        public double ActualDeflection { get; set; }    // inches
        public double AllowableDeflection { get; set; } // inches
        public bool MeetsDeflectionLimit { get; set; }
        public bool IsAdequate { get; set; }
        public LateralSupport RequiredBracing { get; set; }
        public string DesignNotes { get; set; }
        public List<string> CodeReferences { get; set; } = new();
    }

    /// <summary>
    /// NDS 2024 beam design for headers, girts, and purlins.
    /// </summary>
    public static class BeamDesign
    {
        /// <summary>
        /// Design a beam for bending, shear, and deflection.
        /// </summary>
        /// <param name="totalLoad">Total uniform load (plf)</param>
        /// <param name="liveLoad">Live load portion (plf) for deflection check</param>
        /// <param name="spanFt">Clear span (feet)</param>
        /// <param name="grade">Lumber grade</param>
        /// <param name="plies">Number of plies (1 for single, 2 for doubled)</param>
        /// <param name="CD">Load duration factor</param>
        /// <param name="deflLimitLive">Live load deflection limit denominator (240 = L/240)</param>
        /// <param name="deflLimitTotal">Total deflection limit denominator (180 = L/180)</param>
        public static BeamResult Design(double totalLoad, double liveLoad, double spanFt,
            LumberGrade grade, int plies = 1, double CD = 1.15,
            double deflLimitLive = 240, double deflLimitTotal = 180)
        {
            var best = new BeamResult { IsAdequate = false, MomentCapacityRatio = double.MaxValue };

            foreach (var baseSize in LumberSize.BeamSizes)
            {
                var size = new LumberSize
                {
                    NominalWidth = baseSize.NominalWidth,
                    NominalDepth = baseSize.NominalDepth,
                    ActualWidth = baseSize.ActualWidth,
                    ActualDepth = baseSize.ActualDepth,
                    Plies = plies
                };

                var result = CheckBeam(size, totalLoad, liveLoad, spanFt, grade, CD,
                    deflLimitLive, deflLimitTotal);
                if (result.IsAdequate &&
                    result.MomentCapacityRatio < (best.IsAdequate ? best.MomentCapacityRatio : double.MaxValue))
                    best = result;
            }

            return best;
        }

        private static BeamResult CheckBeam(LumberSize size, double wTotal, double wLive,
            double L_ft, LumberGrade grade, double CD,
            double deflLiveDenom, double deflTotalDenom)
        {
            double L = L_ft * 12.0; // inches

            // Moment and shear for simple span uniform load
            double M = wTotal * L_ft * L_ft / 8.0;      // ft-lbs
            double V = wTotal * L_ft / 2.0;              // lbs

            // Adjusted bending stress
            double CM = 1.0;
            double CF = GetSizeFactor(size.ActualDepth);
            double CL = 1.0; // assume continuous lateral support from sheathing
            double Cr = pliesRepetitiveFactor(size.Plies);
            double Fb_adj = grade.Fb * CD * CM * CF * CL * Cr;

            // Bending check
            double fb = M * 12.0 / size.SectionModulus;
            double momentRatio = fb / Fb_adj;

            // Shear check (NDS §3.4)
            double Fv_adj = grade.Fv * CD * CM;
            double fv = 1.5 * V / size.Area;
            double shearRatio = fv / Fv_adj;

            // Deflection check
            double I = size.MomentOfInertia;
            double E = grade.E;

            // delta = 5*w*L^4 / (384*E*I) — uniform load simple span
            double wLive_in = wLive / 12.0; // plf to pli
            double wTotal_in = wTotal / 12.0;
            double deltaLive = 5.0 * wLive_in * Math.Pow(L, 4) / (384.0 * E * I);
            double deltaTotal = 5.0 * wTotal_in * Math.Pow(L, 4) / (384.0 * E * I);

            double allowLive = L / deflLiveDenom;
            double allowTotal = L / deflTotalDenom;

            bool deflOK = deltaLive <= allowLive && deltaTotal <= allowTotal;
            double deflRatio = Math.Max(deltaLive / allowLive, deltaTotal / allowTotal);

            bool adequate = momentRatio <= 1.0 && shearRatio <= 1.0 && deflOK;

            return new BeamResult
            {
                RequiredSize = size,
                MomentCapacityRatio = Math.Round(momentRatio, 3),
                ShearCapacityRatio = Math.Round(shearRatio, 3),
                DeflectionRatio = Math.Round(deflRatio, 3),
                ActualDeflection = Math.Round(Math.Max(deltaLive, deltaTotal), 3),
                AllowableDeflection = Math.Round(Math.Min(allowLive, allowTotal), 3),
                MeetsDeflectionLimit = deflOK,
                IsAdequate = adequate,
                RequiredBracing = LateralSupport.Continuous,
                DesignNotes = adequate
                    ? $"{size.Name} OK — M={momentRatio:F2}, V={shearRatio:F2}, Δ={deflRatio:F2}"
                    : $"{size.Name} NG — M={momentRatio:F2}, V={shearRatio:F2}, Δ={deflRatio:F2}",
                CodeReferences = new List<string>
                {
                    "NDS 2024 §3.3 - Bending",
                    "NDS 2024 §3.4 - Shear",
                    "IBC 2021 Table 1604.3 - Deflection Limits",
                    $"Fb'={Fb_adj:F0} psi, Fv'={Fv_adj:F0} psi",
                    $"L/Δ_live = {(deltaLive > 0 ? L / deltaLive : 9999):F0}, limit = {deflLiveDenom}"
                }
            };
        }

        private static double GetSizeFactor(double depth)
        {
            if (depth <= 11.25) return 1.0; // up to 2x12
            return Math.Pow(12.0 / depth, 1.0 / 9.0);
        }

        private static double pliesRepetitiveFactor(int plies)
        {
            return plies >= 3 ? 1.15 : 1.0; // Cr per NDS §4.3.9
        }
    }
}
