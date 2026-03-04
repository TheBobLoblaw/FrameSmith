using System;
using System.Collections.Generic;
using System.Linq;

namespace PoleBarnGenerator.Models.Materials
{
    public enum BuildingComplexity { Simple, Moderate, Complex }
    public enum CrewExperience { Professional, Experienced, Novice }

    public class WasteAnalysis
    {
        public Dictionary<LumberCategory, double> WasteFactors { get; set; } = new();
        public double ProjectedWasteCost { get; set; }
        public double SalvageValue { get; set; }
        public double NetWasteCost { get; set; }
        public List<WasteRecommendation> Recommendations { get; set; } = new();
    }

    public class WasteRecommendation
    {
        public string Category { get; set; } = "";
        public string Recommendation { get; set; } = "";
        public double PotentialSavings { get; set; }
        public string Difficulty { get; set; } = "Easy";
    }

    /// <summary>
    /// Calculates waste factors and provides recommendations to minimize material waste.
    /// </summary>
    public static class WasteCalculator
    {
        public static WasteAnalysis Calculate(MaterialTakeoff takeoff,
            BuildingComplexity complexity = BuildingComplexity.Moderate,
            CrewExperience experience = CrewExperience.Experienced)
        {
            var analysis = new WasteAnalysis();

            // Base waste factors by complexity
            double lumberBase, hardwareBase, metalBase;
            switch (complexity)
            {
                case BuildingComplexity.Simple:
                    lumberBase = 0.05; hardwareBase = 0.03; metalBase = 0.05;
                    break;
                case BuildingComplexity.Complex:
                    lumberBase = 0.12; hardwareBase = 0.08; metalBase = 0.10;
                    break;
                default: // Moderate
                    lumberBase = 0.08; hardwareBase = 0.05; metalBase = 0.07;
                    break;
            }

            // Crew experience adjustment
            double crewAdj = experience switch
            {
                CrewExperience.Professional => 0.0,
                CrewExperience.Novice => 0.05,
                _ => 0.02
            };

            double lumberWaste = lumberBase + crewAdj;
            double hardwareWaste = hardwareBase + crewAdj * 0.5;

            analysis.WasteFactors[LumberCategory.Posts] = lumberWaste * 0.5; // posts have less waste
            analysis.WasteFactors[LumberCategory.Girts] = lumberWaste;
            analysis.WasteFactors[LumberCategory.Trusses] = lumberWaste * 1.2; // trusses have more
            analysis.WasteFactors[LumberCategory.Purlins] = lumberWaste;
            analysis.WasteFactors[LumberCategory.Headers] = lumberWaste * 0.3; // precision cut
            analysis.WasteFactors[LumberCategory.Plates] = lumberWaste;
            analysis.WasteFactors[LumberCategory.Blocking] = 0.02; // uses offcuts

            // Projected waste cost
            double lumberWasteCost = takeoff.Lumber.TotalCost * lumberWaste;
            double hardwareWasteCost = takeoff.Hardware.TotalHardwareCost * hardwareWaste;
            double roofingWasteCost = takeoff.Roofing.TotalRoofingCost * metalBase;
            double sidingWasteCost = takeoff.Siding.TotalSidingCost * metalBase;
            double foundationWasteCost = takeoff.Foundation.TotalFoundationCost * 0.10; // concrete waste

            analysis.ProjectedWasteCost = lumberWasteCost + hardwareWasteCost +
                roofingWasteCost + sidingWasteCost + foundationWasteCost;

            // Salvage value (usable offcuts for blocking, etc.)
            analysis.SalvageValue = lumberWasteCost * 0.30; // ~30% of waste is reusable
            analysis.NetWasteCost = analysis.ProjectedWasteCost - analysis.SalvageValue;

            // Recommendations
            analysis.Recommendations.Add(new WasteRecommendation
            {
                Category = "Lumber Cutting",
                Recommendation = "Use optimized cut lists to minimize offcuts",
                PotentialSavings = lumberWasteCost * 0.40,
                Difficulty = "Easy"
            });

            if (experience == CrewExperience.Novice)
            {
                analysis.Recommendations.Add(new WasteRecommendation
                {
                    Category = "Crew Training",
                    Recommendation = "Pre-cut all lumber in shop before field assembly",
                    PotentialSavings = lumberWasteCost * crewAdj / lumberWaste * 0.5,
                    Difficulty = "Medium"
                });
            }

            analysis.Recommendations.Add(new WasteRecommendation
            {
                Category = "Metal Panels",
                Recommendation = "Order panels to exact length to avoid field cutting waste",
                PotentialSavings = (roofingWasteCost + sidingWasteCost) * 0.60,
                Difficulty = "Easy"
            });

            analysis.Recommendations.Add(new WasteRecommendation
            {
                Category = "Concrete",
                Recommendation = "Calculate exact volumes; order in 0.25 yd increments",
                PotentialSavings = foundationWasteCost * 0.30,
                Difficulty = "Easy"
            });

            return analysis;
        }
    }
}
