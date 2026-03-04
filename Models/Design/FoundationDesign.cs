using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Models.Design
{
    public class SoilProperties
    {
        public double BearingCapacity { get; set; } = 2000;  // psf
        public double FrictionAngle { get; set; } = 30;      // degrees
        public double FrictionCoefficient { get; set; } = 0.35; // concrete on soil
        public double UnitWeight { get; set; } = 120;        // pcf
        public double FrostDepth { get; set; } = 36;         // inches
        public double PassivePressureCoeff => Math.Pow(Math.Tan((45 + FrictionAngle / 2.0) * Math.PI / 180), 2);
    }

    public enum DrainageRequirement { None, GravelBase, FrenchDrain, SumpRequired }

    public class FootingResult
    {
        public double FootingDiameter { get; set; }       // inches (round footing)
        public double FootingDepth { get; set; }          // inches (footing pad thickness)
        public double PostEmbedment { get; set; }         // inches below grade
        public double TotalHoleDepth { get; set; }        // inches
        public double ConcreteStrength { get; set; }      // psi
        public double ConcreteVolume { get; set; }        // cubic feet
        public bool RequiresReinforcement { get; set; }
        public double BearingRatio { get; set; }          // demand/capacity
        public double OverturningSafetyFactor { get; set; }
        public double SlidingSafetyFactor { get; set; }
        public double UpliftSafetyFactor { get; set; }
        public DrainageRequirement Drainage { get; set; }
        public bool IsAdequate { get; set; }
        public string DesignNotes { get; set; }
        public List<string> CodeReferences { get; set; } = new();
    }

    /// <summary>
    /// Post-frame (pole barn) embedded post foundation design.
    /// Per ASAE EP486 and IBC 2021 §1807.3.
    /// </summary>
    public static class FoundationDesign
    {
        /// <summary>
        /// Design an embedded post footing for pole barn construction.
        /// </summary>
        /// <param name="axialLoad">Factored downward load (lbs)</param>
        /// <param name="lateralLoad">Factored lateral load at top of post (lbs)</param>
        /// <param name="upliftLoad">Factored net uplift (lbs)</param>
        /// <param name="postHeight">Post height above grade (feet)</param>
        /// <param name="soil">Soil properties</param>
        public static FootingResult Design(double axialLoad, double lateralLoad,
            double upliftLoad, double postHeight, SoilProperties soil)
        {
            // Minimum embedment for frost
            double minEmbed = Math.Max(soil.FrostDepth + 6, 36); // inches, min 3' or frost + 6"

            // Embedment for lateral resistance — simplified IBC §1807.3.2.1
            // d = 0.5 * A * (1 + sqrt(1 + 4.36*h/A))
            // where A = 2.34*P / (S1*b), S1 = allowable lateral soil pressure
            double S1 = soil.BearingCapacity / 3.0; // lateral bearing ≈ 1/3 vertical
            double b = 12.0; // assumed post width or hole diameter in inches
            double h = postHeight * 12.0; // inches
            double P = lateralLoad;

            double A = 2.34 * P / (S1 / 144.0 * b); // convert S1 to psi
            double embedLateral = A > 0 ? 0.5 * A * (1 + Math.Sqrt(1 + 4.36 * h / A)) : minEmbed;
            embedLateral = Math.Max(embedLateral, minEmbed);

            // Embedment for uplift resistance
            // Uplift capacity = soil weight in cone of influence
            double embedUplift = minEmbed;
            if (upliftLoad > 0)
            {
                // Cone method: soil cone at 30° from bottom of post
                // Iteratively find depth where soil weight > uplift
                for (double d = minEmbed; d <= 120; d += 6)
                {
                    double coneRadius = d / 12.0 * Math.Tan(30 * Math.PI / 180) + b / 24.0;
                    double coneVolume = Math.PI / 3.0 * (d / 12.0) * coneRadius * coneRadius;
                    double soilResist = coneVolume * soil.UnitWeight;
                    if (soilResist >= upliftLoad * 1.5)
                    {
                        embedUplift = d;
                        break;
                    }
                    embedUplift = d;
                }
            }

            double embedment = Math.Max(embedLateral, embedUplift);
            embedment = Math.Ceiling(embedment / 6.0) * 6; // round up to nearest 6"

            // Footing pad sizing
            double footingDiameter = 18; // minimum
            double bearingArea = Math.PI * Math.Pow(footingDiameter / 2.0, 2) / 144.0; // sq ft
            while (axialLoad / bearingArea > soil.BearingCapacity && footingDiameter <= 48)
            {
                footingDiameter += 2;
                bearingArea = Math.PI * Math.Pow(footingDiameter / 2.0, 2) / 144.0;
            }

            double bearingRatio = axialLoad / (bearingArea * soil.BearingCapacity);
            double footingPadDepth = Math.Max(8, footingDiameter <= 24 ? 8 : 12); // inches

            // Safety factors
            double totalHole = embedment + footingPadDepth;
            double soilWeight = Math.PI * Math.Pow(footingDiameter / 24.0, 2) * (embedment / 12.0) * soil.UnitWeight;
            double concreteWeight = bearingArea * (footingPadDepth / 12.0) * 150; // 150 pcf concrete

            double overturningSF = lateralLoad > 0
                ? (soil.PassivePressureCoeff * soil.UnitWeight * Math.Pow(embedment / 12.0, 3) * (footingDiameter / 12.0) / 6.0) / (lateralLoad * (postHeight + embedment / 24.0))
                : 99;

            double slidingSF = lateralLoad > 0
                ? (axialLoad + soilWeight + concreteWeight) * soil.FrictionCoefficient / lateralLoad
                : 99;

            double upliftSF = upliftLoad > 0
                ? (soilWeight + concreteWeight) / upliftLoad
                : 99;

            bool adequate = bearingRatio <= 1.0 && overturningSF >= 1.5 &&
                            slidingSF >= 1.5 && (upliftLoad <= 0 || upliftSF >= 1.5);

            double concreteVol = bearingArea * footingPadDepth / 12.0 +
                                 Math.PI * Math.Pow(b / 24.0, 2) * embedment / 12.0; // collar

            return new FootingResult
            {
                FootingDiameter = footingDiameter,
                FootingDepth = footingPadDepth,
                PostEmbedment = embedment,
                TotalHoleDepth = totalHole,
                ConcreteStrength = 3000,
                ConcreteVolume = Math.Round(concreteVol, 2),
                RequiresReinforcement = embedment > 48 || lateralLoad > 1000,
                BearingRatio = Math.Round(bearingRatio, 3),
                OverturningSafetyFactor = Math.Round(Math.Min(overturningSF, 99), 2),
                SlidingSafetyFactor = Math.Round(Math.Min(slidingSF, 99), 2),
                UpliftSafetyFactor = Math.Round(Math.Min(upliftSF, 99), 2),
                Drainage = GetDrainageRequirement(soil),
                IsAdequate = adequate,
                DesignNotes = adequate
                    ? $"{footingDiameter}\" dia × {footingPadDepth}\" pad, embed {embedment}\" — OK"
                    : $"{footingDiameter}\" dia × {footingPadDepth}\" pad, embed {embedment}\" — REVIEW REQUIRED",
                CodeReferences = new List<string>
                {
                    "IBC 2021 §1807.3 - Embedded Posts and Poles",
                    "ASAE EP486 - Post-Frame Building Design",
                    $"Soil qa = {soil.BearingCapacity} psf, frost = {soil.FrostDepth}\"",
                    $"Bearing ratio = {bearingRatio:F2}, OT SF = {overturningSF:F1}, Slide SF = {slidingSF:F1}"
                }
            };
        }

        private static DrainageRequirement GetDrainageRequirement(SoilProperties soil)
        {
            if (soil.BearingCapacity < 1500) return DrainageRequirement.FrenchDrain;
            if (soil.FrostDepth > 48) return DrainageRequirement.GravelBase;
            return DrainageRequirement.GravelBase; // always recommend gravel for pole barns
        }
    }
}
