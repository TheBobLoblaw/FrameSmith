using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Models.Analysis
{
    public class LoadCombination
    {
        public string Name { get; set; }
        public double DeadFactor { get; set; }
        public double LiveFactor { get; set; }
        public double RoofLiveFactor { get; set; }
        public double SnowFactor { get; set; }
        public double WindFactor { get; set; }
        public bool IsUpliftCase { get; set; }
        public string CodeReference { get; set; }

        public double CombinedLoad(double D, double L, double Lr, double S, double W)
        {
            return DeadFactor * D + LiveFactor * L + RoofLiveFactor * Lr +
                   SnowFactor * S + WindFactor * W;
        }
    }

    /// <summary>
    /// ASCE 7-22 §2.3.1 (LRFD) and §2.4.1 (ASD) load combinations.
    /// </summary>
    public static class LoadCombinations
    {
        /// <summary>Get ASD load combinations per ASCE 7-22 §2.4.1</summary>
        public static List<LoadCombination> GetASDCombinations(bool includeSnow = true, bool includeWind = true)
        {
            var combos = new List<LoadCombination>
            {
                new LoadCombination { Name = "D", DeadFactor = 1.0, CodeReference = "ASCE 7-22 §2.4.1(1)" },
                new LoadCombination { Name = "D + L", DeadFactor = 1.0, LiveFactor = 1.0, CodeReference = "ASCE 7-22 §2.4.1(2)" },
            };

            if (includeSnow)
            {
                combos.Add(new LoadCombination
                {
                    Name = "D + S", DeadFactor = 1.0, SnowFactor = 1.0,
                    CodeReference = "ASCE 7-22 §2.4.1(3)"
                });
                combos.Add(new LoadCombination
                {
                    Name = "D + 0.75L + 0.75S", DeadFactor = 1.0, LiveFactor = 0.75, SnowFactor = 0.75,
                    CodeReference = "ASCE 7-22 §2.4.1(4)"
                });
            }

            if (includeWind)
            {
                combos.Add(new LoadCombination
                {
                    Name = "D + 0.6W", DeadFactor = 1.0, WindFactor = 0.6,
                    CodeReference = "ASCE 7-22 §2.4.1(5)"
                });
                combos.Add(new LoadCombination
                {
                    Name = "D + 0.75L + 0.75(0.6W) + 0.75S",
                    DeadFactor = 1.0, LiveFactor = 0.75, WindFactor = 0.45, SnowFactor = 0.75,
                    CodeReference = "ASCE 7-22 §2.4.1(6)"
                });
                combos.Add(new LoadCombination
                {
                    Name = "0.6D + 0.6W", DeadFactor = 0.6, WindFactor = 0.6, IsUpliftCase = true,
                    CodeReference = "ASCE 7-22 §2.4.1(7) — Uplift"
                });
            }

            return combos;
        }

        /// <summary>Get LRFD load combinations per ASCE 7-22 §2.3.1</summary>
        public static List<LoadCombination> GetLRFDCombinations(bool includeSnow = true, bool includeWind = true)
        {
            var combos = new List<LoadCombination>
            {
                new LoadCombination { Name = "1.4D", DeadFactor = 1.4, CodeReference = "ASCE 7-22 §2.3.1(1)" },
                new LoadCombination { Name = "1.2D + 1.6L + 0.5Lr", DeadFactor = 1.2, LiveFactor = 1.6, RoofLiveFactor = 0.5, CodeReference = "ASCE 7-22 §2.3.1(2)" },
            };

            if (includeSnow)
            {
                combos.Add(new LoadCombination
                {
                    Name = "1.2D + 1.6S + L", DeadFactor = 1.2, SnowFactor = 1.6, LiveFactor = 1.0,
                    CodeReference = "ASCE 7-22 §2.3.1(3)"
                });
                combos.Add(new LoadCombination
                {
                    Name = "1.2D + 1.6S + 0.5W", DeadFactor = 1.2, SnowFactor = 1.6, WindFactor = 0.5,
                    CodeReference = "ASCE 7-22 §2.3.1(3)"
                });
            }

            if (includeWind)
            {
                combos.Add(new LoadCombination
                {
                    Name = "1.2D + 1.0W + L + 0.5S",
                    DeadFactor = 1.2, WindFactor = 1.0, LiveFactor = 1.0, SnowFactor = 0.5,
                    CodeReference = "ASCE 7-22 §2.3.1(4)"
                });
                combos.Add(new LoadCombination
                {
                    Name = "0.9D + 1.0W", DeadFactor = 0.9, WindFactor = 1.0, IsUpliftCase = true,
                    CodeReference = "ASCE 7-22 §2.3.1(6) — Uplift"
                });
            }

            return combos;
        }

        /// <summary>Find the controlling (maximum) load combination.</summary>
        public static (LoadCombination Combo, double MaxLoad) FindControlling(
            List<LoadCombination> combos, double D, double L, double Lr, double S, double W)
        {
            LoadCombination controlling = combos[0];
            double maxLoad = double.MinValue;

            foreach (var combo in combos)
            {
                double load = combo.CombinedLoad(D, L, Lr, S, W);
                if (load > maxLoad)
                {
                    maxLoad = load;
                    controlling = combo;
                }
            }

            return (controlling, maxLoad);
        }

        /// <summary>Find minimum (uplift) controlling combination.</summary>
        public static (LoadCombination Combo, double MinLoad) FindUpliftControlling(
            List<LoadCombination> combos, double D, double L, double Lr, double S, double W)
        {
            LoadCombination controlling = null;
            double minLoad = double.MaxValue;

            foreach (var combo in combos)
            {
                if (!combo.IsUpliftCase) continue;
                double load = combo.CombinedLoad(D, L, Lr, S, W);
                if (load < minLoad)
                {
                    minLoad = load;
                    controlling = combo;
                }
            }

            return (controlling, minLoad);
        }
    }
}
