using System;
using System.Collections.Generic;
using System.Linq;

namespace PoleBarnGenerator.Models.Materials
{
    public class CutList
    {
        public List<CutListItem> Items { get; set; } = new();
        public CutListSummary Summary { get; set; } = new();
        public double OptimizationScore { get; set; }
        public double WastePercentage { get; set; }
        public double CostSavings { get; set; }
    }

    public class CutListItem
    {
        public string MaterialDescription { get; set; } = "";
        public int StockPieces { get; set; }
        public List<CutPattern> CutPatterns { get; set; } = new();
        public double WastePerPiece { get; set; }
        public string CutNotes { get; set; } = "";
        public int CutPriority { get; set; }
    }

    public class CutPattern
    {
        public double CutLength { get; set; }
        public string Usage { get; set; } = "";
        public string SpecialRequirements { get; set; } = "";
        public bool IsOffcut { get; set; }
    }

    public class CutListSummary
    {
        public double TotalStockFeet { get; set; }
        public double TotalUsedFeet { get; set; }
        public double TotalWasteFeet { get; set; }
        public int TotalStockPieces { get; set; }
    }

    /// <summary>
    /// Generates optimized cut lists from lumber takeoffs using first-fit-decreasing bin packing.
    /// </summary>
    public static class CutListGenerator
    {
        private static readonly double[] StandardStockLengths = { 8, 10, 12, 14, 16, 20 };

        public static CutList Generate(LumberTakeoff lumber)
        {
            var cutList = new CutList();
            double totalWaste = 0;
            double totalStock = 0;
            double totalUsed = 0;
            int priority = 1;

            // Group by size+grade and optimize cuts
            var grouped = lumber.AllItems
                .GroupBy(i => new { i.Size, i.Grade })
                .OrderByDescending(g => g.First().Length); // longest first

            foreach (var group in grouped)
            {
                string sizeGrade = $"{group.Key.Size} {group.Key.Grade}";

                // Expand items into individual cut requests
                var cuts = new List<(double length, string usage)>();
                foreach (var item in group)
                    for (int i = 0; i < item.Quantity; i++)
                        cuts.Add((item.Length, item.Usage));

                if (cuts.Count == 0) continue;

                // Find best stock length
                double maxCut = cuts.Max(c => c.length);
                double bestStockLen = StandardStockLengths.FirstOrDefault(sl => sl >= maxCut);
                if (bestStockLen == 0) bestStockLen = Math.Ceiling(maxCut / 2.0) * 2.0;

                // First-Fit-Decreasing bin packing
                var sortedCuts = cuts.OrderByDescending(c => c.length).ToList();
                var bins = new List<List<(double length, string usage)>>();
                var binRemaining = new List<double>();

                foreach (var cut in sortedCuts)
                {
                    bool placed = false;
                    for (int b = 0; b < bins.Count; b++)
                    {
                        if (binRemaining[b] >= cut.length + 0.25) // 3" kerf allowance
                        {
                            bins[b].Add(cut);
                            binRemaining[b] -= (cut.length + 0.25 / 12.0);
                            placed = true;
                            break;
                        }
                    }
                    if (!placed)
                    {
                        bins.Add(new List<(double, string)> { cut });
                        binRemaining.Add(bestStockLen - cut.length - 0.25 / 12.0);
                    }
                }

                double wastePerPiece = bins.Count > 0
                    ? binRemaining.Sum() / bins.Count : 0;

                var patterns = bins.SelectMany(bin =>
                    bin.Select(c => new CutPattern
                    {
                        CutLength = c.length,
                        Usage = c.usage,
                        IsOffcut = false
                    })).ToList();

                cutList.Items.Add(new CutListItem
                {
                    MaterialDescription = $"{sizeGrade}×{bestStockLen:F0}'",
                    StockPieces = bins.Count,
                    CutPatterns = patterns,
                    WastePerPiece = Math.Round(wastePerPiece, 2),
                    CutPriority = priority++,
                    CutNotes = cuts.Count == bins.Count ? "Full length — no cutting" : ""
                });

                totalStock += bins.Count * bestStockLen;
                totalUsed += cuts.Sum(c => c.length);
                totalWaste += binRemaining.Sum();
            }

            cutList.Summary = new CutListSummary
            {
                TotalStockFeet = totalStock,
                TotalUsedFeet = totalUsed,
                TotalWasteFeet = totalWaste,
                TotalStockPieces = cutList.Items.Sum(i => i.StockPieces)
            };

            cutList.WastePercentage = totalStock > 0 ? totalWaste / totalStock : 0;
            cutList.OptimizationScore = totalStock > 0 ? totalUsed / totalStock : 1.0;

            // Estimate savings vs naive (1 stock piece per cut)
            double naiveStock = lumber.AllItems.Sum(i =>
                i.Quantity * LumberTakeoffCalculator.RoundToStockLength(i.Length));
            cutList.CostSavings = Math.Max(0, (naiveStock - totalStock) * 1.50); // ~$1.50/ft avg

            return cutList;
        }
    }
}
