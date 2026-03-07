using System;
using System.Collections.Generic;
using System.Linq;
using PoleBarnGenerator.Models.Analysis;

namespace PoleBarnGenerator.Models.Materials
{
    /// <summary>
    /// Calculates hardware quantities from geometry, structural results, and lumber takeoff.
    /// </summary>
    public static class HardwareTakeoffCalculator
    {
        public static HardwareTakeoff Calculate(BarnGeometry geometry,
            StructuralDesignResult structural, LumberTakeoff lumber)
        {
            var p = geometry.Params;
            var hw = new HardwareTakeoff();

            // ─── Structural Bolts: post-to-truss connections ───
            int trussCount = p.NumberOfBays + 1;
            int postCount = geometry.Posts.Any(post => post.IsPlanInstance)
                ? geometry.Posts.Count(post => post.IsPlanInstance)
                : geometry.Posts.Count;

            hw.StructuralBolts.Add(new HardwareItem
            {
                PartNumber = "A325-1/2x8",
                Description = "1/2\" × 8\" structural bolt w/ nut & washer",
                Specification = "ASTM A325",
                Quantity = trussCount * 2 * 2, // 2 bolts per connection, 2 connections per truss
                UnitCost = 2.50,
                Category = HardwareCategory.StructuralBolts,
                Usage = "Post-to-truss connections",
                Supplier = "Fastenal"
            });

            // ─── Lag Bolts: girt-to-post connections ───
            int totalGirts = lumber.Girts.Sum(g => g.Quantity);
            int lagBoltsPerGirt = 2; // 2 lags per girt end

            hw.LagBolts.Add(new HardwareItem
            {
                PartNumber = "LAG-3/8x4",
                Description = "3/8\" × 4\" lag screw (hot-dipped galvanized)",
                Specification = "ASTM A307",
                Quantity = totalGirts * lagBoltsPerGirt * 2, // both ends
                UnitCost = 0.85,
                Category = HardwareCategory.LagBolts,
                Usage = "Girt-to-post connections",
                Supplier = "Fastenal"
            });

            // ─── Simpson Connectors ───
            // Post bases (if not embedded — though pole barns are typically embedded)
            hw.ConnectorHardware.Add(new HardwareItem
            {
                PartNumber = "H2.5A",
                Description = "Simpson H2.5A hurricane tie",
                Specification = "Simpson Strong-Tie",
                Quantity = trussCount * 2, // each truss-to-post connection
                UnitCost = 2.75,
                Category = HardwareCategory.ConnectorHardware,
                Usage = "Truss-to-post hurricane ties",
                Supplier = "Simpson Strong-Tie"
            });

            // ─── Truss Plates ───
            // Approximate: 2 plates per joint, ~6-10 joints per truss
            int jointsPerTruss = p.BuildingWidth <= 30 ? 6 : p.BuildingWidth <= 40 ? 8 : 10;
            hw.TrussPlates.Add(new HardwareItem
            {
                PartNumber = "TP-4x6",
                Description = "4\" × 6\" galvanized truss plate (20 ga)",
                Specification = "ASTM A653",
                Quantity = trussCount * jointsPerTruss * 2, // 2 sides per joint
                UnitCost = 0.65,
                Category = HardwareCategory.TrussPlates,
                Usage = "Truss gusset plates",
                Supplier = "MiTek"
            });

            // ─── Purlin Clips ───
            int totalPurlins = lumber.Purlins.Sum(pl => pl.Quantity);
            hw.PurlinClips.Add(new HardwareItem
            {
                PartNumber = "PC-2",
                Description = "Purlin clip (galvanized)",
                Specification = "Simpson H1",
                Quantity = totalPurlins * 2, // clip each end
                UnitCost = 1.15,
                Category = HardwareCategory.PurlinClips,
                Usage = "Purlin-to-truss connections",
                Supplier = "Simpson Strong-Tie"
            });

            // ─── Anchor Bolts ───
            if (structural?.FoundationDesign != null)
            {
                hw.AnchorBolts.Add(new HardwareItem
                {
                    PartNumber = "AB-1/2x10-J",
                    Description = "1/2\" × 10\" J-bolt anchor (galvanized)",
                    Specification = "ASTM F1554 Gr 36",
                    Quantity = postCount, // one per post footing
                    UnitCost = 3.50,
                    Category = HardwareCategory.AnchorBolts,
                    Usage = "Post-to-footing anchors"
                });
            }

            // ─── Fasteners (nails/screws) ───
            // Framing nails: ~30 per girt, ~20 per purlin, ~80 per truss
            int framingNails = totalGirts * 30 + totalPurlins * 20 + trussCount * 80;
            int boxes = (int)Math.Ceiling(framingNails / 2500.0); // ~2500 per 50lb box

            hw.Fasteners.Add(new HardwareItem
            {
                PartNumber = "16D-HDG",
                Description = "16d hot-dipped galvanized framing nail (50 lb box)",
                Specification = "ASTM F1667",
                Quantity = boxes,
                UnitCost = 65.00,
                Category = HardwareCategory.Fasteners,
                Usage = "General framing"
            });

            // Ring-shank nails for girts
            int ringShankBoxes = Math.Max(1, (int)Math.Ceiling(totalGirts * 12 / 1500.0));
            hw.Fasteners.Add(new HardwareItem
            {
                PartNumber = "10D-RS-HDG",
                Description = "10d ring-shank galvanized nail (25 lb box)",
                Specification = "ASTM F1667",
                Quantity = ringShankBoxes,
                UnitCost = 42.00,
                Category = HardwareCategory.Fasteners,
                Usage = "Girt attachment"
            });

            // ─── Miscellaneous ───
            // Washers for structural bolts
            int totalStructBolts = hw.StructuralBolts.Sum(b => b.Quantity);
            hw.Miscellaneous.Add(new HardwareItem
            {
                PartNumber = "FW-1/2",
                Description = "1/2\" flat washer (USS)",
                Specification = "ASTM F844",
                Quantity = totalStructBolts * 2, // 2 per bolt
                UnitCost = 0.15,
                Category = HardwareCategory.Miscellaneous,
                Usage = "Bolt washers"
            });

            // Calculate totals
            foreach (var list in new[] {
                hw.StructuralBolts, hw.LagBolts, hw.ConnectorHardware,
                hw.TrussPlates, hw.Fasteners, hw.PurlinClips,
                hw.AnchorBolts, hw.Miscellaneous })
            {
                foreach (var item in list)
                    item.TotalCost = item.Quantity * item.UnitCost;
            }

            hw.TotalHardwareCost = hw.AllItems.Sum(i => i.TotalCost);
            return hw;
        }
    }
}
