using System;
using System.Collections.Generic;
using System.Linq;
using PoleBarnGenerator.Models.Materials;

namespace PoleBarnGenerator.Models.Pricing
{
    public class PricingResult
    {
        public CostBreakdown MaterialCosts { get; set; } = new();
        public CostBreakdown LaborCosts { get; set; } = new();
        public CostBreakdown EquipmentCosts { get; set; } = new();
        public CostBreakdown OtherCosts { get; set; } = new();
        public double SubtotalCost { get; set; }
        public double SalesTax { get; set; }
        public double ContingencyCost { get; set; }
        public double TotalProjectCost { get; set; }
        public DateTime PriceDate { get; set; } = DateTime.UtcNow;
        public string PriceValidity { get; set; } = "Valid for 30 days";
        public List<PriceAssumption> Assumptions { get; set; } = new();
    }

    public class CostBreakdown
    {
        public double LumberCost { get; set; }
        public double HardwareCost { get; set; }
        public double RoofingCost { get; set; }
        public double SidingCost { get; set; }
        public double FoundationCost { get; set; }
        public double MiscellaneousCost { get; set; }
        public double TotalCategoryCost => LumberCost + HardwareCost + RoofingCost +
            SidingCost + FoundationCost + MiscellaneousCost;
    }

    public class PriceAssumption
    {
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public enum PricingTier { Budget, Standard, Premium }

    /// <summary>
    /// Estimates project costs using regional pricing data.
    /// Provides material, labor, equipment, and total project cost estimates.
    /// </summary>
    public static class MaterialPricingEngine
    {
        /// <summary>
        /// Calculate complete project pricing from material takeoff.
        /// </summary>
        /// <param name="takeoff">Complete material takeoff</param>
        /// <param name="zipCode">Delivery zip code for regional pricing</param>
        /// <param name="tier">Pricing tier (Budget/Standard/Premium)</param>
        /// <param name="salesTaxRate">Local sales tax rate (e.g. 0.07 for 7%)</param>
        /// <param name="contingencyRate">Contingency percentage (default 10%)</param>
        public static PricingResult CalculateProjectCost(MaterialTakeoff takeoff,
            string zipCode = "43001", PricingTier tier = PricingTier.Standard,
            double salesTaxRate = 0.07, double contingencyRate = 0.10)
        {
            var result = new PricingResult();
            double regionFactor = GetRegionalFactor(zipCode);
            double tierFactor = tier switch
            {
                PricingTier.Budget => 0.85,
                PricingTier.Premium => 1.25,
                _ => 1.0
            };

            // ─── Material Costs (from takeoff with regional adjustments) ───
            result.MaterialCosts = new CostBreakdown
            {
                LumberCost = ApplyLumberPricing(takeoff.Lumber, regionFactor, tierFactor),
                HardwareCost = takeoff.Hardware.TotalHardwareCost * regionFactor,
                RoofingCost = takeoff.Roofing.TotalRoofingCost * regionFactor * tierFactor,
                SidingCost = takeoff.Siding.TotalSidingCost * regionFactor * tierFactor,
                FoundationCost = takeoff.Foundation.TotalFoundationCost * regionFactor,
                MiscellaneousCost = takeoff.TotalMaterialCost * 0.03 // 3% misc/consumables
            };

            // ─── Labor Costs (RSMeans-based estimates) ───
            double sqft = takeoff.Roofing.TotalRoofArea; // proxy for building size
            double laborRate = GetLaborRate(zipCode) * tierFactor;
            double framingHours = EstimateFramingHours(takeoff);
            double roofingHours = sqft / 150.0; // ~150 sqft/hr for metal
            double sidingHours = takeoff.Siding.NetWallArea / 120.0;
            double foundationHours = takeoff.Foundation.Concrete.Sum(c => c.Quantity) * 2.0 + 8; // base

            result.LaborCosts = new CostBreakdown
            {
                LumberCost = framingHours * laborRate,
                HardwareCost = 0, // included in framing
                RoofingCost = roofingHours * laborRate,
                SidingCost = sidingHours * laborRate,
                FoundationCost = foundationHours * laborRate,
                MiscellaneousCost = (framingHours + roofingHours + sidingHours + foundationHours) * laborRate * 0.05
            };

            // ─── Equipment Costs ───
            result.EquipmentCosts = new CostBreakdown
            {
                FoundationCost = 350, // post hole auger rental
                LumberCost = 200,     // scaffolding/ladders
                RoofingCost = 150,    // roof jacks, safety
                MiscellaneousCost = 100 // misc tools
            };

            // ─── Other Costs ───
            result.OtherCosts = new CostBreakdown
            {
                MiscellaneousCost = 500 // delivery, permits estimate
            };

            // ─── Totals ───
            result.SubtotalCost = result.MaterialCosts.TotalCategoryCost +
                result.LaborCosts.TotalCategoryCost +
                result.EquipmentCosts.TotalCategoryCost +
                result.OtherCosts.TotalCategoryCost;

            result.SalesTax = result.MaterialCosts.TotalCategoryCost * salesTaxRate;
            result.ContingencyCost = result.SubtotalCost * contingencyRate;
            result.TotalProjectCost = result.SubtotalCost + result.SalesTax + result.ContingencyCost;

            // ─── Assumptions ───
            result.Assumptions = new List<PriceAssumption>
            {
                new() { Category = "Pricing", Description = $"Regional factor: {regionFactor:F2} (zip: {zipCode})" },
                new() { Category = "Pricing", Description = $"Tier: {tier}, multiplier: {tierFactor:F2}" },
                new() { Category = "Labor", Description = $"Labor rate: ${laborRate:F2}/hr (avg crew rate)" },
                new() { Category = "Tax", Description = $"Sales tax: {salesTaxRate:P1} on materials only" },
                new() { Category = "Contingency", Description = $"Contingency: {contingencyRate:P0} on subtotal" },
                new() { Category = "Delivery", Description = "Delivery included in Other costs" },
                new() { Category = "Permits", Description = "Building permit estimate included" },
                new() { Category = "Note", Description = "Prices are estimates — get supplier quotes for accuracy" }
            };

            return result;
        }

        private static double ApplyLumberPricing(LumberTakeoff lumber,
            double regionFactor, double tierFactor)
        {
            double total = 0;
            foreach (var item in lumber.AllItems)
            {
                // Price per board foot by category
                double pricePerBdFt = item.Category switch
                {
                    LumberCategory.Posts => 1.80,      // larger dimension, treated
                    LumberCategory.Headers => 1.50,    // dimension lumber
                    LumberCategory.Trusses => 1.40,    // standard framing
                    LumberCategory.Girts => 1.30,
                    LumberCategory.Purlins => 1.30,
                    LumberCategory.Plates => 1.50,     // some treated
                    LumberCategory.Blocking => 1.10,
                    _ => 1.30
                };

                double itemCost = item.BoardFeet * pricePerBdFt * regionFactor * tierFactor;
                item.UnitPrice = Math.Round(itemCost / Math.Max(1, item.Quantity), 2);
                item.TotalPrice = Math.Round(itemCost, 2);
                total += itemCost;
            }
            return total;
        }

        private static double EstimateFramingHours(MaterialTakeoff takeoff)
        {
            // Rough estimates: hours per piece for different categories
            double hours = 0;
            foreach (var item in takeoff.Lumber.AllItems)
            {
                double hoursPerPiece = item.Category switch
                {
                    LumberCategory.Posts => 1.5,     // set, plumb, brace
                    LumberCategory.Girts => 0.25,    // nail up
                    LumberCategory.Trusses => 2.0,   // assemble + raise
                    LumberCategory.Purlins => 0.15,  // quick install
                    LumberCategory.Headers => 0.75,  // precision work
                    LumberCategory.Plates => 0.15,
                    LumberCategory.Blocking => 0.10,
                    _ => 0.20
                };
                hours += item.Quantity * hoursPerPiece;
            }
            return hours;
        }

        /// <summary>Regional cost factor based on zip code prefix (simplified).</summary>
        private static double GetRegionalFactor(string zipCode)
        {
            if (string.IsNullOrEmpty(zipCode) || zipCode.Length < 3) return 1.0;
            int prefix = int.TryParse(zipCode[..3], out int p) ? p : 500;

            // Simplified regional adjustments
            return prefix switch
            {
                >= 100 and < 200 => 1.15, // Northeast (NY, NJ, CT)
                >= 200 and < 300 => 1.05, // Mid-Atlantic
                >= 300 and < 400 => 0.92, // Southeast
                >= 400 and < 500 => 0.95, // Midwest (OH, IN, KY)
                >= 500 and < 600 => 0.93, // Upper Midwest
                >= 600 and < 700 => 0.97, // Central
                >= 700 and < 800 => 0.90, // South Central (TX)
                >= 800 and < 900 => 1.00, // Mountain West
                >= 900 and < 999 => 1.20, // West Coast (CA)
                _ => 1.0
            };
        }

        /// <summary>Regional labor rate ($/hr for 2-person crew avg).</summary>
        private static double GetLaborRate(string zipCode)
        {
            double baseFactor = GetRegionalFactor(zipCode);
            return 55.0 * baseFactor; // $55/hr base × regional
        }
    }
}
