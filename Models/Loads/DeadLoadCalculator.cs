using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Models.Loads
{
    public enum RoofingMaterial { MetalPanel, Shingles, StandingSeam, BuiltUp }
    public enum SidingMaterial { MetalPanel, WoodBoard, Vinyl, FiberCement }

    public class PointLoad
    {
        public string Description { get; set; }
        public double Load { get; set; }     // lbs
        public double X { get; set; }        // feet from origin
        public double Y { get; set; }
    }

    public class MaterialProperties
    {
        public RoofingMaterial RoofMaterial { get; set; } = RoofingMaterial.MetalPanel;
        public SidingMaterial SidingMaterial { get; set; } = SidingMaterial.MetalPanel;
        public bool HasInsulation { get; set; } = false;
        public double InsulationThickness { get; set; } = 6.0; // inches
        public bool HasCeiling { get; set; } = false;
        public double EquipmentLoad { get; set; } = 0.0;
        public List<PointLoad> PointLoads { get; set; } = new();
    }

    public class DeadLoadResult
    {
        public double RoofDeadLoad { get; set; }        // psf on roof surface
        public double WallDeadLoad { get; set; }        // psf on wall
        public double TrussWeight { get; set; }         // plf per truss
        public double PurlinWeight { get; set; }        // plf
        public double GirtWeight { get; set; }          // plf
        public double PostWeight { get; set; }          // lbs per post
        public double TotalRoofDead { get; set; }       // psf on horizontal projection
        public double TotalWallDead { get; set; }       // psf
        public double CeilingLoad { get; set; }
        public double EquipmentLoad { get; set; }
        public Dictionary<string, double> Breakdown { get; set; } = new();
        public List<string> CodeReferences { get; set; } = new();
        public string Summary { get; set; }
    }

    /// <summary>
    /// Preliminary/simplified dead load calculation per ASCE 7-22 Chapter 3.
    /// Uses representative material weights for early design only; not final takeoff/design values.
    /// </summary>
    public static class DeadLoadCalculator
    {
        public static DeadLoadResult Calculate(BarnGeometry geometry, MaterialProperties materials)
        {
            var p = geometry.Params;
            var breakdown = new Dictionary<string, double>();

            // Roof dead load components (psf on slope)
            double roofPanel = GetRoofMaterialWeight(materials.RoofMaterial);
            breakdown["Roof Panel"] = roofPanel;

            double purlinSelf = 2.0; // 2x4 or 2x6 purlins @ 24" OC ≈ 1.5-2.5 psf
            breakdown["Purlins"] = purlinSelf;

            double trussChords = 3.0; // truss self-weight distributed ≈ 3 psf
            breakdown["Truss Self-Weight"] = trussChords;

            double roofInsulation = materials.HasInsulation ? GetInsulationWeight(materials.InsulationThickness) : 0;
            if (roofInsulation > 0) breakdown["Roof Insulation"] = roofInsulation;

            double roofDead = roofPanel + purlinSelf + trussChords + roofInsulation;

            // Convert slope psf to horizontal projection
            double slopeLength = geometry.GetRoofSlopeLength();
            double halfWidth = p.BuildingWidth / 2.0;
            double slopeFactor = slopeLength / halfWidth;
            double totalRoofDead = roofDead * slopeFactor;

            // Wall dead load
            double wallPanel = GetSidingMaterialWeight(materials.SidingMaterial);
            breakdown["Wall Panel"] = wallPanel;

            double girtSelf = 1.5; // 2x6 girts
            breakdown["Girts"] = girtSelf;

            double wallInsulation = materials.HasInsulation ? GetInsulationWeight(materials.InsulationThickness) : 0;
            if (wallInsulation > 0) breakdown["Wall Insulation"] = wallInsulation;

            double wallDead = wallPanel + girtSelf + wallInsulation;

            // Ceiling
            double ceilingLoad = materials.HasCeiling ? 5.0 : 0; // gypsum + framing
            if (ceilingLoad > 0) breakdown["Ceiling"] = ceilingLoad;

            // Individual member weights
            double purlinPlf = GetPurlinLinearWeight(p.PurlinSpacing);
            double girtPlf = GetGirtLinearWeight(p.GirtSpacing);

            // Post self-weight (assume treated timber, ~35 pcf)
            double postArea = (p.PostWidthInches * p.PostDepthInches) / 144.0; // sq ft
            double postWeight = postArea * p.EaveHeight * 35.0; // lbs

            // Truss linear weight estimate (based on span) using a simplified rule-of-thumb.
            double trussPlf = 3.0 + p.BuildingWidth * 0.15; // rough: 3 plf base + 0.15 per foot span

            return new DeadLoadResult
            {
                RoofDeadLoad = Math.Round(roofDead, 2),
                WallDeadLoad = Math.Round(wallDead, 2),
                TrussWeight = Math.Round(trussPlf, 2),
                PurlinWeight = Math.Round(purlinPlf, 2),
                GirtWeight = Math.Round(girtPlf, 2),
                PostWeight = Math.Round(postWeight, 1),
                TotalRoofDead = Math.Round(totalRoofDead, 2),
                TotalWallDead = Math.Round(wallDead, 2),
                CeilingLoad = ceilingLoad,
                EquipmentLoad = materials.EquipmentLoad,
                Breakdown = breakdown,
                CodeReferences = new List<string>
                {
                    "ASCE 7-22 Chapter 3 - Dead Loads",
                    "AISC Steel Construction Manual Table 17-13",
                    $"Roof: {materials.RoofMaterial}, Walls: {materials.SidingMaterial}"
                },
                Summary = $"Dead: Roof={totalRoofDead:F1} psf (horiz), Wall={wallDead:F1} psf, " +
                          $"Post={postWeight:F0} lbs, Truss={trussPlf:F1} plf"
            };
        }

        private static double GetRoofMaterialWeight(RoofingMaterial mat)
        {
            switch (mat)
            {
                case RoofingMaterial.MetalPanel: return 1.5;
                case RoofingMaterial.StandingSeam: return 1.8;
                case RoofingMaterial.Shingles: return 3.0;
                case RoofingMaterial.BuiltUp: return 6.0;
                default: return 1.5;
            }
        }

        private static double GetSidingMaterialWeight(SidingMaterial mat)
        {
            switch (mat)
            {
                case SidingMaterial.MetalPanel: return 1.5;
                case SidingMaterial.WoodBoard: return 3.0;
                case SidingMaterial.Vinyl: return 0.8;
                case SidingMaterial.FiberCement: return 4.0;
                default: return 1.5;
            }
        }

        private static double GetInsulationWeight(double thicknessInches)
        {
            return thicknessInches * 0.15; // fiberglass batt ~0.15 psf per inch
        }

        private static double GetPurlinLinearWeight(double spacingInches)
        {
            // 2x4 @ 24" = ~1.3 plf, 2x6 @ 24" = ~2.0 plf
            return spacingInches <= 16 ? 2.5 : spacingInches <= 24 ? 2.0 : 1.5;
        }

        private static double GetGirtLinearWeight(double spacingInches)
        {
            return spacingInches <= 16 ? 2.5 : spacingInches <= 24 ? 2.0 : 1.5;
        }
    }
}
