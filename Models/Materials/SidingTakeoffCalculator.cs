using System;
using System.Collections.Generic;
using System.Linq;

namespace PoleBarnGenerator.Models.Materials
{
    /// <summary>
    /// Calculates siding and trim material takeoffs.
    /// Deducts openings from wall area for accurate quantities.
    /// </summary>
    public static class SidingTakeoffCalculator
    {
        public static SidingTakeoff Calculate(BarnGeometry geometry, string color = "Galvalume",
            int gauge = 29, double panelCoverageWidth = 36.0)
        {
            var p = geometry.Params;
            var takeoff = new SidingTakeoff();

            // Wall areas
            double sidewallArea = 2.0 * p.BuildingLength * p.EaveHeight;
            double endwallRectArea = 2.0 * p.BuildingWidth * p.EaveHeight;
            double endwallGableArea = 2.0 * (p.BuildingWidth * p.RoofRise / 2.0);
            double totalWallArea = sidewallArea + endwallRectArea + endwallGableArea;

            // Opening deductions
            double doorArea = p.Doors.Sum(d => d.Width * d.Height);
            double windowArea = p.Windows.Sum(w => w.Width * w.Height);
            double totalOpeningArea = doorArea + windowArea;
            double netWallArea = totalWallArea - totalOpeningArea;

            takeoff.TotalWallArea = totalWallArea;
            takeoff.OpeningDeductions = totalOpeningArea;
            takeoff.NetWallArea = netWallArea;

            // ─── Wall Panels ───
            double coverageWidthFt = panelCoverageWidth / 12.0;
            double perimeter = 2.0 * (p.BuildingWidth + p.BuildingLength);
            int panelsAround = (int)Math.Ceiling(perimeter / coverageWidthFt);

            // Panel length = eave height (most panels, ignoring gable triangles for now)
            double panelLength = Math.Ceiling(p.EaveHeight);

            takeoff.WallPanels.Add(new SidingItem
            {
                ProductCode = $"{gauge}GA-PBR-{color}",
                Description = $"{gauge} GA PBR Wall Panel - {color}",
                Coverage = panelCoverageWidth,
                Length = panelLength,
                Quantity = panelsAround,
                SquareFeet = netWallArea,
                Color = color,
                UnitPrice = panelLength * 0.95, // ~$0.95/lin ft for 29ga
                TotalPrice = panelsAround * panelLength * 0.95
            });

            // Gable end panels (taller, triangular cuts)
            double maxGableHeight = p.EaveHeight + p.RoofRise;
            int gablePanels = (int)Math.Ceiling(p.BuildingWidth / coverageWidthFt) * 2;
            double gablePanelLen = Math.Ceiling(maxGableHeight);

            takeoff.WallPanels.Add(new SidingItem
            {
                ProductCode = $"{gauge}GA-PBR-{color}",
                Description = $"{gauge} GA PBR Gable Panel - {color} (field cut)",
                Coverage = panelCoverageWidth,
                Length = gablePanelLen,
                Quantity = gablePanels,
                SquareFeet = endwallGableArea,
                Color = color,
                UnitPrice = gablePanelLen * 0.95,
                TotalPrice = gablePanels * gablePanelLen * 0.95
            });

            // ─── Corner Trim ───
            int corners = 4;
            int cornerPieces = corners * (int)Math.Ceiling(p.EaveHeight / 10.0);
            takeoff.CornerTrim.Add(new SidingItem
            {
                ProductCode = $"CT-{gauge}GA-{color}",
                Description = $"Outside corner trim - {color} (10' piece)",
                Length = 10.0,
                Quantity = cornerPieces,
                Color = color,
                UnitPrice = 18.00,
                TotalPrice = cornerPieces * 18.00
            });

            // ─── Base Trim ───
            int basePieces = (int)Math.Ceiling(perimeter / 10.0);
            takeoff.BaseTrim.Add(new SidingItem
            {
                ProductCode = $"BT-{gauge}GA-{color}",
                Description = $"Base trim / rat guard - {color} (10' piece)",
                Length = 10.0,
                Quantity = basePieces,
                Color = color,
                UnitPrice = 12.00,
                TotalPrice = basePieces * 12.00
            });

            // ─── J-Channel around openings ───
            double jChannelLinFt = 0;
            foreach (var d in p.Doors)
                jChannelLinFt += 2 * d.Height + d.Width; // sides + top
            foreach (var w in p.Windows)
                jChannelLinFt += 2 * w.Height + 2 * w.Width; // all 4 sides

            int jPieces = (int)Math.Ceiling(jChannelLinFt / 10.0);
            if (jPieces > 0)
            {
                takeoff.JChannel.Add(new SidingItem
                {
                    ProductCode = $"JC-{gauge}GA-{color}",
                    Description = $"J-channel trim - {color} (10' piece)",
                    Length = 10.0,
                    Quantity = jPieces,
                    Color = color,
                    UnitPrice = 8.50,
                    TotalPrice = jPieces * 8.50
                });
            }

            // ─── Siding Screws ───
            double screwsPerSqFt = 3.5;
            int totalScrews = (int)Math.Ceiling(netWallArea * screwsPerSqFt);
            int screwBags = (int)Math.Ceiling(totalScrews / 250.0);
            takeoff.Fasteners.Add(new SidingItem
            {
                ProductCode = "WH-#10x1",
                Description = "#10 × 1\" woodgrip screw w/ EPDM washer (bag of 250)",
                Quantity = screwBags,
                UnitPrice = 24.00,
                TotalPrice = screwBags * 24.00
            });

            // ─── Flashing ───
            // Base flashing
            int flashPieces = (int)Math.Ceiling(perimeter / 10.0);
            takeoff.Flashing.Add(new SidingItem
            {
                ProductCode = $"FL-BASE-{color}",
                Description = $"Base flashing - {color} (10' piece)",
                Length = 10.0,
                Quantity = flashPieces,
                UnitPrice = 10.00,
                TotalPrice = flashPieces * 10.00
            });

            // Window/door head flashing
            double headFlashLF = p.Doors.Sum(d => d.Width + 1) + p.Windows.Sum(w => w.Width + 0.5);
            int headFlashPcs = Math.Max(0, (int)Math.Ceiling(headFlashLF / 10.0));
            if (headFlashPcs > 0)
            {
                takeoff.Flashing.Add(new SidingItem
                {
                    ProductCode = $"FL-HEAD-{color}",
                    Description = $"Head flashing - {color} (10' piece)",
                    Length = 10.0,
                    Quantity = headFlashPcs,
                    UnitPrice = 10.00,
                    TotalPrice = headFlashPcs * 10.00
                });
            }

            // Calculate totals
            takeoff.TotalSidingCost =
                takeoff.WallPanels.Sum(i => i.TotalPrice) +
                takeoff.CornerTrim.Sum(i => i.TotalPrice) +
                takeoff.BaseTrim.Sum(i => i.TotalPrice) +
                takeoff.JChannel.Sum(i => i.TotalPrice) +
                takeoff.Fasteners.Sum(i => i.TotalPrice) +
                takeoff.Flashing.Sum(i => i.TotalPrice) +
                takeoff.Accessories.Sum(i => i.TotalPrice);

            return takeoff;
        }
    }
}
