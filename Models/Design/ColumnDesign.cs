using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Models.Design
{
    public class ColumnResult
    {
        public LumberSize RequiredSize { get; set; }
        public double CapacityRatio { get; set; }
        public double BucklingCapacity { get; set; }   // lbs
        public double CompressionCapacity { get; set; } // lbs
        public double BendingCapacity { get; set; }     // ft-lbs
        public double CombinedRatio { get; set; }
        public bool IsAdequate { get; set; }
        public double SlendernessRatio { get; set; }
        public string DesignNotes { get; set; }
        public List<string> CodeReferences { get; set; } = new();
    }

    /// <summary>
    /// NDS 2024 column design — combined axial compression + bending.
    /// Applicable to post-frame (pole barn) construction posts.
    /// </summary>
    public static class ColumnDesign
    {
        /// <summary>
        /// Design a column per NDS §3.9 (combined bending + compression).
        /// </summary>
        /// <param name="axialLoad">Factored axial load (lbs)</param>
        /// <param name="lateralLoad">Factored lateral load (plf on post height)</param>
        /// <param name="unbracedLength">Unbraced length (feet)</param>
        /// <param name="grade">Lumber grade/species</param>
        /// <param name="loadDurationFactor">CD per NDS Table 2.3.2 (1.6 wind, 1.15 snow, 1.0 DL)</param>
        /// <param name="wetService">True if ground contact / exposed</param>
        public static ColumnResult Design(double axialLoad, double lateralLoad,
            double unbracedLength, LumberGrade grade,
            double loadDurationFactor = 1.15, bool wetService = true)
        {
            var best = new ColumnResult { IsAdequate = false, CapacityRatio = double.MaxValue };

            foreach (var size in LumberSize.StandardSizes)
            {
                var result = CheckColumn(size, axialLoad, lateralLoad, unbracedLength,
                    grade, loadDurationFactor, wetService);
                if (result.IsAdequate && result.CombinedRatio < best.CombinedRatio)
                    best = result;
            }

            // If no standard size works, try larger
            if (!best.IsAdequate)
            {
                var large = new LumberSize { NominalWidth = 8, NominalDepth = 10, ActualWidth = 7.25, ActualDepth = 9.25 };
                best = CheckColumn(large, axialLoad, lateralLoad, unbracedLength,
                    grade, loadDurationFactor, wetService);
            }

            return best;
        }

        private static ColumnResult CheckColumn(LumberSize size, double P, double w,
            double Le_ft, LumberGrade grade, double CD, bool wet)
        {
            double Le = Le_ft * 12.0; // inches
            double d = Math.Min(size.ActualWidth, size.ActualDepth); // least dimension
            double slenderness = Le / d;

            // NDS adjustment factors
            double CM = wet ? 0.91 : 1.0;  // wet service (compression)
            double CMb = wet ? 0.85 : 1.0;  // wet service (bending)
            double Ct = 1.0;  // temperature
            double Ci = 1.0;  // incising (treated lumber)

            // Adjusted Fc
            double Fc_star = grade.Fc * CD * CM * Ct * Ci;
            // Fc* without Cp
            double Emin_adj = grade.Emin * CM * Ct * Ci * 1.0; // KF=1 for ASD

            // Column stability factor Cp — NDS Eq. 3.7-1
            double FcE = 0.822 * Emin_adj / (slenderness * slenderness);
            double ratio = FcE / Fc_star;
            double c = 0.8; // sawn lumber
            double Cp = (1 + ratio) / (2 * c) - Math.Sqrt(Math.Pow((1 + ratio) / (2 * c), 2) - ratio / c);

            double Fc_adj = Fc_star * Cp;
            double compCapacity = Fc_adj * size.Area;

            // Bending from lateral load (uniform load on cantilever or pinned-fixed)
            // For pole barn posts: fixed base, pinned top — M_max = w*L²/8 approximately
            double M = w * Le_ft * Le_ft / 8.0; // ft-lbs
            double Fb_adj = grade.Fb * CD * CMb * Ct * Ci;

            // Size factor CF for posts/timbers ≥ 5" (NDS §4.3.6)
            double CF = 1.0;
            if (size.ActualDepth > 12)
                CF = Math.Pow(12.0 / size.ActualDepth, 1.0 / 9.0);

            Fb_adj *= CF;

            double S = size.SectionModulus; // in³
            double bendCapacity = Fb_adj * S / 12.0; // ft-lbs

            // Combined stress ratio — NDS Eq. 3.9-3
            double fc = P / size.Area;
            double fb = M * 12.0 / S; // psi
            double combinedRatio = Math.Pow(fc / Fc_adj, 2) + fb / (Fb_adj * (1 - fc / FcE));
            combinedRatio = Math.Max(combinedRatio, 0);

            bool adequate = combinedRatio <= 1.0 && slenderness <= 50;

            return new ColumnResult
            {
                RequiredSize = size,
                CapacityRatio = Math.Round(combinedRatio, 3),
                CombinedRatio = Math.Round(combinedRatio, 3),
                BucklingCapacity = Math.Round(FcE * size.Area, 0),
                CompressionCapacity = Math.Round(compCapacity, 0),
                BendingCapacity = Math.Round(bendCapacity, 1),
                SlendernessRatio = Math.Round(slenderness, 1),
                IsAdequate = adequate,
                DesignNotes = adequate
                    ? $"{size.Name} OK — ratio={combinedRatio:F2}, le/d={slenderness:F1}"
                    : $"{size.Name} NG — ratio={combinedRatio:F2}, le/d={slenderness:F1}",
                CodeReferences = new List<string>
                {
                    "NDS 2024 §3.7 - Column Stability",
                    "NDS 2024 §3.9 - Combined Bending + Compression",
                    $"CD={CD}, CM={CM}, Cp={Cp:F3}",
                    $"Fc*={Fc_star:F0} psi, FcE={FcE:F0} psi, Fc'={Fc_adj:F0} psi"
                }
            };
        }
    }
}
