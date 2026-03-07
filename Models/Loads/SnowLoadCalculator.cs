using System;
using System.Collections.Generic;
using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.Models.Loads
{
    public enum ExposureCondition { FullyExposed, PartiallyExposed, Sheltered }
    public enum ThermalCondition { Heated, Unheated, FreezerBuilding, ContinuouslyHeated }
    public enum BuildingCategory { I, II, III, IV }
    public enum RoofSurface { Unobstructed, Obstructed }

    public class SnowParameters
    {
        public double GroundSnowLoad { get; set; } = 25.0;
        public BuildingCategory ImportanceFactor { get; set; } = BuildingCategory.II;
        public ExposureCondition Exposure { get; set; } = ExposureCondition.PartiallyExposed;
        public ThermalCondition Thermal { get; set; } = ThermalCondition.Unheated;
        public RoofSurface Surface { get; set; } = RoofSurface.Unobstructed;
        public bool RainOnSnowZone { get; set; } = false;
    }

    public class SnowLoadResult
    {
        public double GroundSnowLoad { get; set; }
        public double FlatRoofSnowLoad { get; set; }
        public double SlopedRoofSnowLoad { get; set; }
        public double UnbalancedSnowLoad { get; set; }
        public double RainOnSnowSurcharge { get; set; }
        public double DriftLoad { get; set; }
        public double DesignRoofSnowLoad { get; set; }
        public double ExposureFactor { get; set; }
        public double ThermalFactor { get; set; }
        public double SlopeReductionFactor { get; set; }
        public List<string> CodeReferences { get; set; } = new();
        public string Summary { get; set; }
    }

    /// <summary>
    /// ASCE 7-22 Chapter 7 snow load calculations.
    /// </summary>
    public static class SnowLoadCalculator
    {
        public static SnowLoadResult Calculate(BarnGeometry geometry, SnowParameters snow)
        {
            var p = geometry.Params;
            double pg = snow.GroundSnowLoad;
            double Ce = GetExposureFactor(snow.Exposure);
            double Ct = GetThermalFactor(snow.Thermal);
            double Is = GetImportanceFactor(snow.ImportanceFactor);

            // Flat roof snow load: ASCE 7-22 Eq. 7.3-1
            double pf = 0.7 * Ce * Ct * Is * pg;
            // Minimum per §7.3.4
            double pfMin = Is * pg <= 20 ? Is * pg : 20 * Is;
            pf = Math.Max(pf, pfMin > 0 ? 20 * Is : pf);
            if (pg <= 0) pf = 0;

            // Sloped roof factor Cs: ASCE 7-22 §7.4
            double roofAngle = p.RoofAngleDegrees;
            double Cs = GetSlopeReductionFactor(roofAngle, snow.Surface, snow.Thermal);
            double ps = Cs * pf;

            // Unbalanced snow (ASCE 7-22 §7.6) — simplified for gable
            double unbalanced = 0;
            if (roofAngle >= 2.39 && roofAngle <= 70) // 0.5/12 to ~70 degrees
            {
                double W = p.BuildingWidth / 2.0; // horizontal distance eave to ridge
                double hd = 0.43 * Math.Pow(W, 1.0 / 3.0) * Math.Pow(pg + 10, 1.0 / 4.0) - 1.5;
                hd = Math.Max(hd, 0);
                double gamma = Math.Min(0.13 * pg + 14, 30); // snow density pcf
                unbalanced = ps + gamma * hd; // windward side drift
            }

            // Rain-on-snow surcharge: ASCE 7-22 §7.10
            double rainOnSnow = 0;
            if (snow.RainOnSnowZone && pg <= 20 && roofAngle < GetRainOnSnowMaxSlope())
                rainOnSnow = 5.0; // 5 psf surcharge

            // Drift load for multi-level roofs (lean-tos)
            double driftLoad = 0;
            if (geometry.LeanToGeometries != null && geometry.LeanToGeometries.Count > 0)
            {
                double lu = p.BuildingLength; // upwind fetch
                double hb = pf / (Math.Min(0.13 * pg + 14, 30)); // balanced depth
                LeanToGeometry leanToGeo = geometry.LeanToGeometries[0];
                double leanToEaveHeight = leanToGeo?.LeanTo?.EaveHeight ?? (p.EaveHeight * 0.7);
                double hc = (p.EaveHeight - leanToEaveHeight) - hb;
                if (hc > 0)
                {
                    double hd_drift = 0.43 * Math.Pow(lu, 1.0 / 3.0) * Math.Pow(pg + 10, 1.0 / 4.0) - 1.5;
                    hd_drift = Math.Max(hd_drift, 0);
                    double gamma = Math.Min(0.13 * pg + 14, 30);
                    driftLoad = gamma * Math.Min(hd_drift, hc);
                }
            }

            double designLoad = Math.Max(ps, Math.Max(unbalanced, ps + rainOnSnow));

            return new SnowLoadResult
            {
                GroundSnowLoad = pg,
                FlatRoofSnowLoad = Math.Round(pf, 2),
                SlopedRoofSnowLoad = Math.Round(ps, 2),
                UnbalancedSnowLoad = Math.Round(unbalanced, 2),
                RainOnSnowSurcharge = rainOnSnow,
                DriftLoad = Math.Round(driftLoad, 2),
                DesignRoofSnowLoad = Math.Round(designLoad, 2),
                ExposureFactor = Ce,
                ThermalFactor = Ct,
                SlopeReductionFactor = Math.Round(Cs, 3),
                CodeReferences = new List<string>
                {
                    "ASCE 7-22 §7.3 - Flat Roof Snow Loads",
                    "ASCE 7-22 §7.4 - Sloped Roof Snow Loads",
                    "ASCE 7-22 §7.6 - Unbalanced Snow Loads",
                    snow.RainOnSnowZone ? "ASCE 7-22 §7.10 - Rain-on-Snow" : "",
                    $"pg = {pg} psf, Ce = {Ce}, Ct = {Ct}, Is = {Is}"
                },
                Summary = $"Snow: pg={pg} psf, pf={pf:F1} psf, ps={ps:F1} psf, " +
                          $"Design={designLoad:F1} psf (Ce={Ce}, Ct={Ct}, Cs={Cs:F2})"
            };
        }

        private static double GetExposureFactor(ExposureCondition exp)
        {
            switch (exp)
            {
                case ExposureCondition.FullyExposed: return 0.8;
                case ExposureCondition.Sheltered: return 1.2;
                default: return 1.0;
            }
        }

        private static double GetThermalFactor(ThermalCondition thermal)
        {
            switch (thermal)
            {
                case ThermalCondition.Heated: return 1.0;
                case ThermalCondition.ContinuouslyHeated: return 0.85;
                case ThermalCondition.Unheated: return 1.1;
                case ThermalCondition.FreezerBuilding: return 1.3;
                default: return 1.1;
            }
        }

        private static double GetImportanceFactor(BuildingCategory cat)
        {
            switch (cat)
            {
                case BuildingCategory.I: return 0.8;
                case BuildingCategory.III: return 1.1;
                case BuildingCategory.IV: return 1.2;
                default: return 1.0;
            }
        }

        private static double GetSlopeReductionFactor(double thetaDeg, RoofSurface surface, ThermalCondition thermal)
        {
            if (thetaDeg <= 5) return 1.0;
            if (surface == RoofSurface.Unobstructed && thermal != ThermalCondition.FreezerBuilding)
            {
                if (thetaDeg >= 70) return 0.0;
                return 1.0 - (thetaDeg - 5) / 65.0;
            }
            // Obstructed or cold surface
            if (thetaDeg >= 70) return 0.0;
            return 1.0 - (thetaDeg - 10) / 60.0;
        }

        private static double GetRainOnSnowMaxSlope() => Math.Atan(0.5 / 12.0) * 180.0 / Math.PI;
    }
}
