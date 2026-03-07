using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Models.Loads
{
    public enum BuildingUse { Agricultural, Storage, Assembly, Workshop, Parking }
    public enum LoadDistribution { Uniform, Concentrated, Combined }

    public class LiveLoadResult
    {
        public double FloorLiveLoad { get; set; }
        public double RoofLiveLoad { get; set; }
        public double ReducedFloorLiveLoad { get; set; }
        public double ReducedRoofLiveLoad { get; set; }
        public double ConcentratedLoad { get; set; }
        public double ImpactFactor { get; set; }
        public double TributaryArea { get; set; }
        public double ReductionFactor { get; set; }
        public List<string> CodeReferences { get; set; } = new();
        public string Summary { get; set; }
    }

    /// <summary>
    /// Preliminary/simplified IBC 2021 / ASCE 7-22 Chapter 4 live load calculations.
    /// Intended for early design checks; final design requires full engineered load criteria.
    /// </summary>
    public static class LiveLoadCalculator
    {
        public static LiveLoadResult Calculate(BarnGeometry geometry, BuildingUse use, LoadDistribution dist)
        {
            var p = geometry.Params;
            double Lo = GetMinimumLiveLoad(use);
            double roofLo = 20.0; // IBC Table 1607.1 roof minimum

            // Tributary area for reduction
            double At = p.BaySpacing * p.BuildingWidth; // typical interior bay
            double Kll = 2.0; // interior columns

            // Floor live load reduction: ASCE 7-22 §4.7
            double reducedFloor = Lo;
            if (At * Kll >= 400 && Lo >= 100 == false) // no reduction for >= 100 psf
            {
                reducedFloor = Lo * (0.25 + 15.0 / Math.Sqrt(Kll * At));
                reducedFloor = Math.Max(reducedFloor, Lo * 0.5); // minimum 50%
                reducedFloor = Math.Max(reducedFloor, Lo * 0.4); // for members supporting 2+ floors
            }

            // Roof live load reduction: ASCE 7-22 §4.8
            double R1 = At <= 200 ? 1.0 : At >= 600 ? 0.6 : 1.2 - 0.001 * At;
            double slopeRise = p.RoofPitchRise;
            double R2 = slopeRise <= 4 ? 1.0 : slopeRise >= 12 ? 0.6 : 1.2 - 0.05 * slopeRise;
            double reducedRoof = roofLo * R1 * R2;
            reducedRoof = Math.Max(reducedRoof, 12.0); // minimum 12 psf

            // Concentrated loads
            double concentrated = 0;
            double impact = 1.0;
            if (use == BuildingUse.Storage)
            {
                concentrated = 2000; // lbs, per IBC
            }
            else if (use == BuildingUse.Assembly)
            {
                concentrated = 2000;
                impact = 1.0; // no additional impact for static assembly
            }
            else if (use == BuildingUse.Parking)
            {
                concentrated = 3000;
                impact = 1.0;
            }

            double reductionFactor = reducedFloor / Lo;

            return new LiveLoadResult
            {
                FloorLiveLoad = Lo,
                RoofLiveLoad = roofLo,
                ReducedFloorLiveLoad = Math.Round(reducedFloor, 2),
                ReducedRoofLiveLoad = Math.Round(reducedRoof, 2),
                ConcentratedLoad = concentrated,
                ImpactFactor = impact,
                TributaryArea = Math.Round(At, 1),
                ReductionFactor = Math.Round(reductionFactor, 3),
                CodeReferences = new List<string>
                {
                    "IBC 2021 Table 1607.1 - Minimum Live Loads",
                    "ASCE 7-22 §4.7 - Floor Live Load Reduction",
                    "ASCE 7-22 §4.8 - Roof Live Load Reduction",
                    $"Use: {use}, Lo = {Lo} psf, AT = {At:F0} sf"
                },
                Summary = $"Live: Floor={reducedFloor:F1} psf (from {Lo}), Roof={reducedRoof:F1} psf | Use={use}"
            };
        }

        public static double GetMinimumLiveLoad(BuildingUse use)
        {
            switch (use)
            {
                case BuildingUse.Agricultural: return 20.0;
                case BuildingUse.Storage: return 125.0;
                case BuildingUse.Assembly: return 100.0;
                case BuildingUse.Workshop: return 50.0;
                case BuildingUse.Parking: return 50.0;
                default: return 20.0;
            }
        }
    }
}
