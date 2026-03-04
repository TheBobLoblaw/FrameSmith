using System;
using System.Collections.Generic;
using System.Linq;
using PoleBarnGenerator.Models.Analysis;
using PoleBarnGenerator.Models.Design;

namespace PoleBarnGenerator.Models.Materials
{
    /// <summary>
    /// Calculates complete lumber takeoffs from building geometry and structural design.
    /// Integrates with StructuralDesignResult for accurate member sizing.
    /// </summary>
    public static class LumberTakeoffCalculator
    {
        public static LumberTakeoff Calculate(BarnGeometry geometry, StructuralDesignResult structural)
        {
            var p = geometry.Params;
            var takeoff = new LumberTakeoff();

            CalculatePosts(takeoff, geometry, structural);
            CalculateGirts(takeoff, geometry, structural);
            CalculatePurlins(takeoff, geometry, structural);
            CalculateHeaders(takeoff, geometry, structural);
            CalculatePlates(takeoff, geometry, structural);
            CalculateTrussMaterial(takeoff, geometry, structural);
            CalculateBlocking(takeoff, geometry);

            // Totals
            var allItems = takeoff.AllItems;
            takeoff.TotalBoardFeet = allItems.Sum(i => i.BoardFeet);
            takeoff.TotalWithWaste = takeoff.TotalBoardFeet * (1.0 + takeoff.WasteFactor);
            takeoff.TotalCost = allItems.Sum(i => i.TotalPrice);

            return takeoff;
        }

        private static void CalculatePosts(LumberTakeoff takeoff, BarnGeometry geometry,
            StructuralDesignResult structural)
        {
            var p = geometry.Params;
            string postSize = p.PostSize; // e.g. "6x6"
            string grade = "DF #2";

            // Use structural result if available
            if (structural?.PostDesign?.RequiredSize != null)
            {
                var s = structural.PostDesign.RequiredSize;
                postSize = $"{s.NominalWidth}x{s.NominalDepth}";
            }

            int postCount = geometry.Posts.Count;
            double postLength = p.EaveHeight;
            // Add embedment depth if foundation designed
            if (structural?.FoundationDesign != null)
                postLength += structural.FoundationDesign.PostEmbedment / 12.0;

            // Round up to standard length
            double stockLength = RoundToStockLength(postLength);

            double nomW = ParseNominalWidth(postSize);
            double nomD = ParseNominalDepth(postSize);
            double bdFtEach = nomW * nomD * stockLength / 12.0;

            takeoff.Posts.Add(new LumberItem
            {
                Description = $"{postSize}×{stockLength:F0}' {grade}",
                Size = postSize,
                Grade = grade,
                Length = stockLength,
                Quantity = postCount,
                LinearFeet = postCount * stockLength,
                BoardFeet = postCount * bdFtEach,
                Category = LumberCategory.Posts,
                Usage = "Building posts (embedded)"
            });
        }

        private static void CalculateGirts(LumberTakeoff takeoff, BarnGeometry geometry,
            StructuralDesignResult structural)
        {
            var p = geometry.Params;
            string girtSize = "2x6";
            string grade = "DF #2";

            if (structural?.GirtDesign?.RequiredSize != null)
            {
                var s = structural.GirtDesign.RequiredSize;
                girtSize = $"{s.NominalWidth}x{s.NominalDepth}";
            }

            double girtSpacing = p.GirtSpacing > 0 ? p.GirtSpacing : 24.0; // inches
            int girtsPerBay = Math.Max(1, (int)Math.Floor(p.EaveHeight * 12.0 / girtSpacing));
            int numBays = p.NumberOfBays;
            double baySpacing = p.ActualBaySpacing;
            double stockLength = RoundToStockLength(baySpacing);

            // Sidewalls: 2 sides × bays × girts per bay
            int sidewallGirts = 2 * numBays * girtsPerBay;

            // Endwalls: 2 ends, approximate spans
            // Endwall girts span building width with intermediate posts
            int endwallGirts = 2 * girtsPerBay; // simplified — single span each end

            int totalGirts = sidewallGirts + endwallGirts;

            double nomW = ParseNominalWidth(girtSize);
            double nomD = ParseNominalDepth(girtSize);
            double bdFtEach = nomW * nomD * stockLength / 12.0;

            if (sidewallGirts > 0)
            {
                takeoff.Girts.Add(new LumberItem
                {
                    Description = $"{girtSize}×{stockLength:F0}' {grade}",
                    Size = girtSize,
                    Grade = grade,
                    Length = stockLength,
                    Quantity = sidewallGirts,
                    LinearFeet = sidewallGirts * stockLength,
                    BoardFeet = sidewallGirts * bdFtEach,
                    Category = LumberCategory.Girts,
                    Usage = "Sidewall girts"
                });
            }

            if (endwallGirts > 0)
            {
                double ewStockLen = RoundToStockLength(p.BuildingWidth);
                double ewBdFt = nomW * nomD * ewStockLen / 12.0;
                takeoff.Girts.Add(new LumberItem
                {
                    Description = $"{girtSize}×{ewStockLen:F0}' {grade}",
                    Size = girtSize,
                    Grade = grade,
                    Length = ewStockLen,
                    Quantity = endwallGirts,
                    LinearFeet = endwallGirts * ewStockLen,
                    BoardFeet = endwallGirts * ewBdFt,
                    Category = LumberCategory.Girts,
                    Usage = "Endwall girts"
                });
            }
        }

        private static void CalculatePurlins(LumberTakeoff takeoff, BarnGeometry geometry,
            StructuralDesignResult structural)
        {
            var p = geometry.Params;
            string purlinSize = "2x6";
            string grade = "DF #2";

            if (structural?.PurlinDesign?.RequiredSize != null)
            {
                var s = structural.PurlinDesign.RequiredSize;
                purlinSize = $"{s.NominalWidth}x{s.NominalDepth}";
            }

            double purlinSpacing = p.PurlinSpacing > 0 ? p.PurlinSpacing : 24.0; // inches
            double slopeLength = Math.Sqrt(
                Math.Pow(p.BuildingWidth / 2.0, 2) + Math.Pow(p.RoofRise, 2));
            int purlinsPerSide = Math.Max(1, (int)Math.Ceiling(slopeLength * 12.0 / purlinSpacing));
            int totalPurlins = 2 * purlinsPerSide * p.NumberOfBays; // both slopes × all bays

            double baySpacing = p.ActualBaySpacing;
            double stockLength = RoundToStockLength(baySpacing);

            double nomW = ParseNominalWidth(purlinSize);
            double nomD = ParseNominalDepth(purlinSize);
            double bdFtEach = nomW * nomD * stockLength / 12.0;

            takeoff.Purlins.Add(new LumberItem
            {
                Description = $"{purlinSize}×{stockLength:F0}' {grade}",
                Size = purlinSize,
                Grade = grade,
                Length = stockLength,
                Quantity = totalPurlins,
                LinearFeet = totalPurlins * stockLength,
                BoardFeet = totalPurlins * bdFtEach,
                Category = LumberCategory.Purlins,
                Usage = $"Roof purlins @ {purlinSpacing}\" o.c."
            });
        }

        private static void CalculateHeaders(LumberTakeoff takeoff, BarnGeometry geometry,
            StructuralDesignResult structural)
        {
            var p = geometry.Params;
            string grade = "DF #2";

            // Headers over each door opening
            foreach (var door in p.Doors)
            {
                string headerSize = "2x12";
                int plies = 2;

                if (structural?.HeaderDesign?.RequiredSize != null)
                {
                    var s = structural.HeaderDesign.RequiredSize;
                    headerSize = $"{s.NominalWidth}x{s.NominalDepth}";
                    plies = s.Plies > 0 ? s.Plies : (door.Width > 12 ? 3 : 2);
                }
                else
                {
                    // Rule of thumb: 2-ply for <12', 3-ply for 12-16'
                    plies = door.Width > 12 ? 3 : 2;
                }

                double stockLength = RoundToStockLength(door.Width + 0.5); // +6" bearing
                double nomW = ParseNominalWidth(headerSize);
                double nomD = ParseNominalDepth(headerSize);
                double bdFtEach = nomW * nomD * stockLength / 12.0;

                takeoff.Headers.Add(new LumberItem
                {
                    Description = $"({plies}) {headerSize}×{stockLength:F0}' {grade}",
                    Size = headerSize,
                    Grade = grade,
                    Length = stockLength,
                    Quantity = plies,
                    LinearFeet = plies * stockLength,
                    BoardFeet = plies * bdFtEach,
                    Category = LumberCategory.Headers,
                    Usage = $"Header over {door.Width:F0}'×{door.Height:F0}' {door.Type} door on {door.Wall}"
                });
            }

            // Headers over windows
            foreach (var window in p.Windows)
            {
                double stockLength = RoundToStockLength(window.Width + 0.5);
                string headerSize = "2x8";
                double nomW = ParseNominalWidth(headerSize);
                double nomD = ParseNominalDepth(headerSize);
                double bdFtEach = nomW * nomD * stockLength / 12.0;

                takeoff.Headers.Add(new LumberItem
                {
                    Description = $"(2) {headerSize}×{stockLength:F0}' {grade}",
                    Size = headerSize,
                    Grade = grade,
                    Length = stockLength,
                    Quantity = 2,
                    LinearFeet = 2 * stockLength,
                    BoardFeet = 2 * bdFtEach,
                    Category = LumberCategory.Headers,
                    Usage = $"Header over {window.Width:F0}'×{window.Height:F0}' window on {window.Wall}"
                });
            }
        }

        private static void CalculatePlates(LumberTakeoff takeoff, BarnGeometry geometry,
            StructuralDesignResult structural)
        {
            var p = geometry.Params;
            string grade = "DF #2";
            double perimeter = 2.0 * (p.BuildingWidth + p.BuildingLength);

            // Top plates (splash boards / eave struts) — 2x6 or 2x8
            string plateSize = "2x6";
            double stockLength = 16.0; // standard
            int piecesNeeded = (int)Math.Ceiling(perimeter / stockLength);
            double nomW = ParseNominalWidth(plateSize);
            double nomD = ParseNominalDepth(plateSize);
            double bdFtEach = nomW * nomD * stockLength / 12.0;

            takeoff.Plates.Add(new LumberItem
            {
                Description = $"{plateSize}×{stockLength:F0}' {grade}",
                Size = plateSize,
                Grade = grade,
                Length = stockLength,
                Quantity = piecesNeeded,
                LinearFeet = piecesNeeded * stockLength,
                BoardFeet = piecesNeeded * bdFtEach,
                Category = LumberCategory.Plates,
                Usage = "Top plate / eave strut"
            });

            // Skirt boards at base — 2x6 treated
            takeoff.Plates.Add(new LumberItem
            {
                Description = $"2x6×{stockLength:F0}' PT",
                Size = "2x6",
                Grade = "PT #2",
                Length = stockLength,
                Quantity = piecesNeeded,
                LinearFeet = piecesNeeded * stockLength,
                BoardFeet = piecesNeeded * bdFtEach,
                Category = LumberCategory.Plates,
                Usage = "Skirt board (pressure treated)"
            });
        }

        private static void CalculateTrussMaterial(LumberTakeoff takeoff, BarnGeometry geometry,
            StructuralDesignResult structural)
        {
            var p = geometry.Params;
            int trussCount = p.NumberOfBays + 1; // one per bay line
            string grade = "DF #2";

            // Top chord — approximate length per side
            double slopeLength = Math.Sqrt(
                Math.Pow(p.BuildingWidth / 2.0, 2) + Math.Pow(p.RoofRise, 2));
            double topChordLen = RoundToStockLength(slopeLength);
            string topChordSize = "2x6";
            double nomW = 2, nomD = 6;
            double bdFtEach = nomW * nomD * topChordLen / 12.0;

            takeoff.Trusses.Add(new LumberItem
            {
                Description = $"{topChordSize}×{topChordLen:F0}' {grade}",
                Size = topChordSize,
                Grade = grade,
                Length = topChordLen,
                Quantity = trussCount * 2, // 2 top chords per truss
                LinearFeet = trussCount * 2 * topChordLen,
                BoardFeet = trussCount * 2 * bdFtEach,
                Category = LumberCategory.Trusses,
                Usage = "Truss top chords"
            });

            // Bottom chord — full width
            double btmLen = RoundToStockLength(p.BuildingWidth);
            double btmBdFt = nomW * nomD * btmLen / 12.0;

            takeoff.Trusses.Add(new LumberItem
            {
                Description = $"{topChordSize}×{btmLen:F0}' {grade}",
                Size = topChordSize,
                Grade = grade,
                Length = btmLen,
                Quantity = trussCount,
                LinearFeet = trussCount * btmLen,
                BoardFeet = trussCount * btmBdFt,
                Category = LumberCategory.Trusses,
                Usage = "Truss bottom chords"
            });

            // Web members — approximate 4-6 webs per truss depending on span
            int websPerTruss = p.BuildingWidth <= 30 ? 4 : p.BuildingWidth <= 40 ? 6 : 8;
            double avgWebLen = p.RoofRise * 0.7; // rough average
            double webStockLen = RoundToStockLength(avgWebLen);
            string webSize = "2x4";
            double webBdFt = 2.0 * 4.0 * webStockLen / 12.0;

            takeoff.Trusses.Add(new LumberItem
            {
                Description = $"{webSize}×{webStockLen:F0}' {grade}",
                Size = webSize,
                Grade = grade,
                Length = webStockLen,
                Quantity = trussCount * websPerTruss,
                LinearFeet = trussCount * websPerTruss * webStockLen,
                BoardFeet = trussCount * websPerTruss * webBdFt,
                Category = LumberCategory.Trusses,
                Usage = "Truss web members"
            });
        }

        private static void CalculateBlocking(LumberTakeoff takeoff, BarnGeometry geometry)
        {
            var p = geometry.Params;
            string grade = "DF #2";

            // Purlin blocking at truss lines
            double slopeLength = Math.Sqrt(
                Math.Pow(p.BuildingWidth / 2.0, 2) + Math.Pow(p.RoofRise, 2));
            double purlinSpacing = p.PurlinSpacing > 0 ? p.PurlinSpacing : 24.0;
            int purlinsPerSide = Math.Max(1, (int)Math.Ceiling(slopeLength * 12.0 / purlinSpacing));
            int trussLines = p.NumberOfBays + 1;
            int blockingPcs = 2 * purlinsPerSide * trussLines; // rough estimate

            // Use short pieces — 2x4 × 2'
            takeoff.Blocking.Add(new LumberItem
            {
                Description = "2x4×8' DF #2",
                Size = "2x4",
                Grade = grade,
                Length = 8,
                Quantity = (int)Math.Ceiling(blockingPcs / 4.0), // 4 blocks per 8' piece
                LinearFeet = blockingPcs * 2,
                BoardFeet = blockingPcs * 2.0 * 4.0 * 2.0 / 12.0,
                Category = LumberCategory.Blocking,
                Usage = "Purlin blocking / miscellaneous"
            });
        }

        // ─── Helpers ──────────────────────────────────────

        /// <summary>Round up to nearest standard stock length.</summary>
        public static double RoundToStockLength(double needed)
        {
            double[] stockLengths = { 8, 10, 12, 14, 16, 20, 24 };
            foreach (var sl in stockLengths)
                if (sl >= needed) return sl;
            return Math.Ceiling(needed / 2.0) * 2.0; // round to even
        }

        private static double ParseNominalWidth(string size)
        {
            var parts = size.ToLower().Split('x');
            return parts.Length >= 1 && double.TryParse(parts[0], out double v) ? v : 2;
        }

        private static double ParseNominalDepth(string size)
        {
            var parts = size.ToLower().Split('x');
            return parts.Length >= 2 && double.TryParse(parts[1], out double v) ? v : 6;
        }
    }
}
