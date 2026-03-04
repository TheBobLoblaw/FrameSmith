using System;
using System.Collections.Generic;
using System.Linq;

namespace PoleBarnGenerator.Models.Materials
{
    /// <summary>
    /// Calculates roofing material takeoffs for metal panel roofing.
    /// </summary>
    public static class RoofingTakeoffCalculator
    {
        public static RoofingTakeoff Calculate(BarnGeometry geometry, string color = "Galvalume",
            int gauge = 26, double panelCoverageWidth = 36.0)
        {
            var p = geometry.Params;
            var takeoff = new RoofingTakeoff();

            double halfWidth = p.BuildingWidth / 2.0;
            double slopeLength = Math.Sqrt(halfWidth * halfWidth + p.RoofRise * p.RoofRise);
            double overhangEave = p.OverhangEave;
            double overhangGable = p.OverhangGable;

            // Total roof slope length including eave overhang
            double panelLength = slopeLength + overhangEave;
            double ridgeToEave = panelLength;

            // Building length with gable overhangs
            double totalLength = p.BuildingLength + 2.0 * overhangGable;

            // ─── Metal Panels ───
            double coverageWidthFt = panelCoverageWidth / 12.0; // 36" = 3'
            int panelsPerSide = (int)Math.Ceiling(totalLength / coverageWidthFt);
            int totalPanels = panelsPerSide * 2; // both slopes

            double roofArea = 2.0 * ridgeToEave * totalLength;
            takeoff.TotalRoofArea = roofArea;
            takeoff.TotalWithWaste = roofArea * (1.0 + takeoff.WastePercentage);

            // Round panel length up to nearest foot
            double stockPanelLen = Math.Ceiling(ridgeToEave);

            takeoff.MetalPanels.Add(new RoofingItem
            {
                ProductCode = $"{gauge}GA-PBR-{color}",
                Description = $"{gauge} GA PBR Panel - {color}",
                Coverage = panelCoverageWidth,
                Length = stockPanelLen,
                Quantity = totalPanels,
                SquareFeet = roofArea,
                Color = color,
                Manufacturer = "",
                UnitPrice = stockPanelLen * 1.25, // ~$1.25/lin ft for 26ga
                TotalPrice = totalPanels * stockPanelLen * 1.25
            });

            // ─── Ridge Cap ───
            double ridgeLength = totalLength;
            int ridgePieces = (int)Math.Ceiling(ridgeLength / 10.0); // 10' sections
            takeoff.RidgeCap.Add(new RoofingItem
            {
                ProductCode = $"RC-{gauge}GA-{color}",
                Description = $"Ridge cap - {color} ({gauge} GA)",
                Coverage = 10.0 * 12.0, // 10' piece
                Length = 10.0,
                Quantity = ridgePieces,
                SquareFeet = ridgeLength * 1.0, // ~1' coverage
                Color = color
            });

            // ─── Eave Trim / Drip Edge ───
            double eaveLength = 2.0 * totalLength; // both sides
            int eavePieces = (int)Math.Ceiling(eaveLength / 10.0);
            takeoff.EaveTrim.Add(new RoofingItem
            {
                ProductCode = $"ET-{gauge}GA-{color}",
                Description = $"Eave trim / drip edge - {color}",
                Length = 10.0,
                Quantity = eavePieces,
                SquareFeet = eaveLength,
                Color = color
            });

            // ─── Rake Trim ───
            double rakeLength = 4.0 * ridgeToEave; // 2 gable ends × 2 slopes
            int rakePieces = (int)Math.Ceiling(rakeLength / 10.0);
            takeoff.RakeTrim.Add(new RoofingItem
            {
                ProductCode = $"RK-{gauge}GA-{color}",
                Description = $"Rake trim - {color}",
                Length = 10.0,
                Quantity = rakePieces,
                SquareFeet = rakeLength,
                Color = color
            });

            // ─── Roofing Screws ───
            // Typical: 3-5 screws per sq ft for metal panels
            double screwsPerSqFt = 4.0;
            int totalScrews = (int)Math.Ceiling(roofArea * screwsPerSqFt);
            int screwBags = (int)Math.Ceiling(totalScrews / 250.0); // 250 per bag

            takeoff.Fasteners.Add(new RoofingItem
            {
                ProductCode = "WH-#10x1.5",
                Description = "#10 × 1-1/2\" woodgrip screw w/ EPDM washer (bag of 250)",
                Quantity = screwBags,
                SquareFeet = 0,
                UnitPrice = 28.00,
                TotalPrice = screwBags * 28.00
            });

            // Stitch screws for panel overlaps
            int stitchScrews = panelsPerSide * 2 * (int)Math.Ceiling(stockPanelLen / 2.0);
            int stitchBags = Math.Max(1, (int)Math.Ceiling(stitchScrews / 250.0));
            takeoff.Fasteners.Add(new RoofingItem
            {
                ProductCode = "SS-#10x3/4",
                Description = "#10 × 3/4\" stitch screw (bag of 250)",
                Quantity = stitchBags,
                UnitPrice = 18.00,
                TotalPrice = stitchBags * 18.00
            });

            // ─── Closure Strips ───
            int closureStrips = (int)Math.Ceiling(eaveLength + ridgeLength);
            takeoff.Accessories.Add(new RoofingItem
            {
                ProductCode = "CS-PBR",
                Description = "PBR profile closure strip (foam, 3' piece)",
                Quantity = (int)Math.Ceiling(closureStrips / 3.0),
                UnitPrice = 2.50
            });

            // Calculate totals
            takeoff.TotalRoofingCost =
                takeoff.MetalPanels.Sum(i => i.TotalPrice) +
                takeoff.RidgeCap.Sum(i => i.TotalPrice) +
                takeoff.EaveTrim.Sum(i => i.TotalPrice) +
                takeoff.RakeTrim.Sum(i => i.TotalPrice) +
                takeoff.Fasteners.Sum(i => i.TotalPrice) +
                takeoff.Accessories.Sum(i => i.TotalPrice);

            return takeoff;
        }
    }
}
