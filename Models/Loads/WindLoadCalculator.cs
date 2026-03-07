using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Models.Loads
{
    public enum ExposureCategory { B, C, D }
    public enum WindImportanceFactor { I, II, III, IV }

    public class WindParameters
    {
        public double BasicWindSpeed { get; set; } = 115.0;
        public WindImportanceFactor ImportanceFactor { get; set; } = WindImportanceFactor.II;
        public ExposureCategory Exposure { get; set; } = ExposureCategory.C;
        public double TopographicFactor { get; set; } = 1.0;
        public double DirectionalityFactor { get; set; } = 0.85;
        public double InternalPressureCoeff { get; set; } = 0.18;
    }

    public class WindLoadResult
    {
        public double BasicWindSpeed { get; set; }
        public double VelocityPressure { get; set; }
        public double WindwardWallPressure { get; set; }
        public double LeewardWallPressure { get; set; }
        public double SideWallPressure { get; set; }
        public double RoofWindwardPressure { get; set; }
        public double RoofLeewardPressure { get; set; }
        public double UpliftPressure { get; set; }
        public double ComponentCladdingPressure { get; set; }
        public List<MemberWindLoad> MemberLoads { get; set; } = new();
        public List<string> CodeReferences { get; set; } = new();
        public string Summary { get; set; }
    }

    public class MemberWindLoad
    {
        public string MemberType { get; set; }
        public double TributaryArea { get; set; }
        public double DesignPressure { get; set; }
        public double LinearLoad { get; set; }
    }

    /// <summary>
    /// Preliminary/simplified ASCE 7-22 Chapter 27/28 wind load screening for enclosed low-rise buildings.
    /// Results are for early-stage design only and must be engineered for construction documents.
    /// </summary>
    public static class WindLoadCalculator
    {
        public static WindLoadResult Calculate(BarnGeometry geometry, WindParameters wind)
        {
            var p = geometry.Params;
            double V = wind.BasicWindSpeed;
            double meanRoofHt = p.EaveHeight + geometry.RoofRise / 2.0;
            if (p.RoofPitchRise <= 2.4) meanRoofHt = p.EaveHeight;

            double Kz = GetKz(meanRoofHt, wind.Exposure);
            double Kzt = wind.TopographicFactor;
            double Kd = wind.DirectionalityFactor;
            double Iw = GetImportanceFactorValue(wind.ImportanceFactor);
            double Ke = 1.0;

            // ASCE 7-22 Eq. 26.10-1
            double qh = 0.00256 * Kz * Kzt * Kd * Ke * Iw * V * V;
            double G = 0.85;
            double roofAngle = p.RoofAngleDegrees;
            double L_over_B = p.BuildingLength / p.BuildingWidth;

            double CpWindward = 0.8;
            double CpLeeward = GetLeewardCp(L_over_B);
            double CpSidewall = -0.7;
            double CpRoofWW = GetRoofWindwardCp(roofAngle);
            double CpRoofLW = -0.6;
            double GCpi = wind.InternalPressureCoeff;

            double pWindward = qh * G * CpWindward + qh * GCpi;
            double pLeeward = qh * G * CpLeeward - qh * GCpi;
            double pSidewall = qh * G * CpSidewall - qh * GCpi;
            double pRoofWW = qh * G * CpRoofWW - qh * GCpi;
            double pRoofLW = qh * G * CpRoofLW - qh * GCpi;
            double pUplift = qh * G * Math.Min(CpRoofWW, CpRoofLW) - qh * GCpi;
            double pCC = qh * (-1.8) - qh * GCpi;

            var memberLoads = CalculateMemberLoads(geometry, qh, G, GCpi);

            return new WindLoadResult
            {
                BasicWindSpeed = V,
                VelocityPressure = Math.Round(qh, 2),
                WindwardWallPressure = Math.Round(pWindward, 2),
                LeewardWallPressure = Math.Round(pLeeward, 2),
                SideWallPressure = Math.Round(pSidewall, 2),
                RoofWindwardPressure = Math.Round(pRoofWW, 2),
                RoofLeewardPressure = Math.Round(pRoofLW, 2),
                UpliftPressure = Math.Round(pUplift, 2),
                ComponentCladdingPressure = Math.Round(pCC, 2),
                MemberLoads = memberLoads,
                CodeReferences = new List<string>
                {
                    "ASCE 7-22 §26.10 - Velocity Pressure",
                    "ASCE 7-22 §27.3 - Directional Procedure (MWFRS)",
                    "ASCE 7-22 §30.3 - Components & Cladding",
                    $"Exposure Category {wind.Exposure}",
                    $"V = {V} mph, I = {Iw:F2}, Kz = {Kz:F3}, Kzt = {Kzt:F2}, Kd = {Kd:F2}"
                },
                Summary = $"Wind: V={V} mph, Exp {wind.Exposure}, qh={qh:F1} psf | " +
                          $"Walls: WW={pWindward:F1}, LW={pLeeward:F1}, SW={pSidewall:F1} psf | " +
                          $"Roof: WW={pRoofWW:F1}, LW={pRoofLW:F1}, Uplift={pUplift:F1} psf"
            };
        }

        private static double GetImportanceFactorValue(WindImportanceFactor importance)
        {
            switch (importance)
            {
                case WindImportanceFactor.I: return 0.87;
                case WindImportanceFactor.III:
                case WindImportanceFactor.IV:
                    return 1.15;
                default:
                    return 1.0;
            }
        }

        private static double GetKz(double z, ExposureCategory exposure)
        {
            double alpha, zg;
            switch (exposure)
            {
                case ExposureCategory.B: alpha = 7.0; zg = 1200; break;
                case ExposureCategory.D: alpha = 11.5; zg = 700; break;
                default: alpha = 9.5; zg = 900; break;
            }
            double zMin = exposure == ExposureCategory.B ? 30 : exposure == ExposureCategory.D ? 7 : 15;
            return 2.01 * Math.Pow(Math.Max(z, zMin) / zg, 2.0 / alpha);
        }

        private static double GetLeewardCp(double LoverB)
        {
            if (LoverB <= 1.0) return -0.5;
            if (LoverB <= 2.0) return -0.3;
            return -0.2;
        }

        private static double GetRoofWindwardCp(double thetaDeg)
        {
            if (thetaDeg <= 10) return -0.7;
            if (thetaDeg <= 15) return -0.5;
            if (thetaDeg <= 20) return -0.3;
            if (thetaDeg <= 25) return -0.2;
            if (thetaDeg <= 35) return 0.0;
            if (thetaDeg <= 45) return 0.2;
            return 0.3;
        }

        private static List<MemberWindLoad> CalculateMemberLoads(
            BarnGeometry geometry, double qh, double G, double GCpi)
        {
            var p = geometry.Params;
            var loads = new List<MemberWindLoad>();

            double girtTrib = p.GirtSpacing / 12.0;
            double girtP = Math.Abs(qh * G * (-0.7) - qh * GCpi);
            loads.Add(new MemberWindLoad
            {
                MemberType = "Sidewall Girt",
                TributaryArea = Math.Round(girtTrib * p.BaySpacing, 1),
                DesignPressure = Math.Round(girtP, 2),
                LinearLoad = Math.Round(girtP * girtTrib, 2)
            });

            double ewP = Math.Abs(qh * G * 0.8 + qh * GCpi);
            loads.Add(new MemberWindLoad
            {
                MemberType = "Endwall Girt (Windward)",
                TributaryArea = Math.Round(girtTrib * p.BuildingWidth / 2.0, 1),
                DesignPressure = Math.Round(ewP, 2),
                LinearLoad = Math.Round(ewP * girtTrib, 2)
            });

            double purlinTrib = p.PurlinSpacing / 12.0;
            double purlinP = Math.Abs(qh * G * (-0.7) - qh * GCpi);
            loads.Add(new MemberWindLoad
            {
                MemberType = "Roof Purlin",
                TributaryArea = Math.Round(purlinTrib * p.BaySpacing, 1),
                DesignPressure = Math.Round(purlinP, 2),
                LinearLoad = Math.Round(purlinP * purlinTrib, 2)
            });

            double postTrib = p.BaySpacing * p.EaveHeight / 2.0;
            double postP = Math.Abs(qh * G * 0.8 + qh * GCpi);
            loads.Add(new MemberWindLoad
            {
                MemberType = "Corner/Sidewall Post",
                TributaryArea = Math.Round(postTrib, 1),
                DesignPressure = Math.Round(postP, 2),
                LinearLoad = Math.Round(postP * p.BaySpacing / 2.0, 2)
            });

            return loads;
        }
    }
}
