using System;
using System.Collections.Generic;
using System.Linq;
using PoleBarnGenerator.Models.Analysis;
using PoleBarnGenerator.Models.Design;

namespace PoleBarnGenerator.Models.Materials
{
    /// <summary>
    /// Calculates foundation material takeoffs from geometry and structural foundation design.
    /// </summary>
    public static class FoundationTakeoffCalculator
    {
        public static FoundationTakeoff Calculate(BarnGeometry geometry, StructuralDesignResult structural)
        {
            var p = geometry.Params;
            var takeoff = new FoundationTakeoff();
            var footing = structural?.FoundationDesign;

            int postCount = geometry.Posts.Any(post => post.IsPlanInstance)
                ? geometry.Posts.Count(post => post.IsPlanInstance)
                : geometry.Posts.Count;

            // Default values if no structural design
            double footingDia = footing?.FootingDiameter ?? 18.0; // inches
            double footingDepth = footing?.FootingDepth ?? 8.0;   // inches
            double embedment = footing?.PostEmbedment ?? 42.0;    // inches
            double concreteStrength = footing?.ConcreteStrength ?? 3000;
            bool needsReinf = footing?.RequiresReinforcement ?? false;

            // ─── Concrete ───
            // Footing pads
            double padVolEach = Math.PI * Math.Pow(footingDia / 2.0 / 12.0, 2) * (footingDepth / 12.0); // cu ft
            double totalPadVol = postCount * padVolEach;

            // Collar concrete around posts (fill hole above pad)
            double postWidthFt = double.TryParse(p.PostSize.Split('x')[0], out double pw) ? pw / 12.0 : 0.5;
            double holeDia = footingDia / 12.0; // ft
            double collarVolEach = Math.PI * Math.Pow(holeDia / 2.0, 2) * (embedment / 12.0)
                                   - (postWidthFt * postWidthFt) * (embedment / 12.0); // hollow for post
            double totalCollarVol = postCount * Math.Max(0, collarVolEach);

            double totalConcreteCF = totalPadVol + totalCollarVol;
            double totalConcreteYards = totalConcreteCF / 27.0;

            // Add 10% waste
            totalConcreteYards *= 1.10;
            totalConcreteYards = Math.Ceiling(totalConcreteYards * 4) / 4.0; // round to 0.25 yd

            takeoff.TotalConcreteYards = totalConcreteYards;
            takeoff.Concrete.Add(new ConcreteItem
            {
                Description = $"Ready-mix concrete ({concreteStrength:F0} psi)",
                Volume = totalConcreteYards,
                Strength = concreteStrength,
                Quantity = 1,
                UnitPrice = 165.00, // per cubic yard typical
                TotalPrice = totalConcreteYards * 165.00
            });

            // If small quantity, might use bags instead
            if (totalConcreteYards < 2.0)
            {
                int bags80lb = (int)Math.Ceiling(totalConcreteCF * 1.10 / 0.6); // ~0.6 cu ft per 80lb bag
                takeoff.Concrete.Add(new ConcreteItem
                {
                    Description = "80 lb bag premix concrete (alternative to ready-mix)",
                    Volume = totalConcreteYards,
                    Strength = 4000,
                    Quantity = bags80lb,
                    UnitPrice = 6.50,
                    TotalPrice = bags80lb * 6.50
                });
            }

            // ─── Reinforcement ───
            if (needsReinf)
            {
                // Vertical rebar in each footing — 2 × #4 bars
                double rebarLength = (embedment + footingDepth) / 12.0 + 1.0; // +1' for hook
                takeoff.Reinforcement.Add(new ReinforcementItem
                {
                    Description = "#4 rebar (1/2\" deformed bar)",
                    Size = "#4",
                    Length = Math.Ceiling(rebarLength),
                    Quantity = postCount * 2,
                    WeightPerFoot = 0.668,
                    TotalWeight = postCount * 2 * rebarLength * 0.668,
                    UnitPrice = 0.85, // per foot
                    TotalPrice = postCount * 2 * Math.Ceiling(rebarLength) * 0.85
                });
            }

            takeoff.TotalReinforcementWeight = takeoff.Reinforcement.Sum(r => r.TotalWeight);

            // ─── Anchor Bolts (in foundation context) ───
            takeoff.AnchorBolts.Add(new HardwareItem
            {
                PartNumber = "AB-5/8x12-J",
                Description = "5/8\" × 12\" J-bolt (galvanized)",
                Specification = "ASTM F1554 Gr 36",
                Quantity = postCount,
                UnitCost = 4.25,
                TotalCost = postCount * 4.25,
                Category = HardwareCategory.AnchorBolts,
                Usage = "Post-to-footing anchor (cast in concrete)"
            });

            // ─── Excavation ───
            double holeDepthFt = (embedment + footingDepth) / 12.0;
            double holeVolEach = Math.PI * Math.Pow(holeDia / 2.0, 2) * holeDepthFt; // cu ft
            double totalExcavation = postCount * holeVolEach / 27.0; // cu yards

            takeoff.Excavation.Add(new ExcavationItem
            {
                Description = $"Post hole excavation ({footingDia:F0}\" dia × {holeDepthFt:F1}' deep)",
                Volume = Math.Round(totalExcavation, 2),
                UnitPrice = 45.00, // per hole (auger rental amortized)
                TotalPrice = postCount * 45.00
            });

            // Gravel backfill — 4" layer in each hole
            double gravelVolEach = Math.PI * Math.Pow(holeDia / 2.0, 2) * (4.0 / 12.0);
            double totalGravelCY = postCount * gravelVolEach / 27.0;
            takeoff.Excavation.Add(new ExcavationItem
            {
                Description = "Crushed gravel (drainage under footings)",
                Volume = Math.Round(totalGravelCY, 2),
                UnitPrice = 55.00, // per cubic yard
                TotalPrice = Math.Ceiling(totalGravelCY) * 55.00
            });

            // ─── Total ───
            takeoff.TotalFoundationCost =
                takeoff.Concrete.Sum(c => c.TotalPrice) +
                takeoff.Reinforcement.Sum(r => r.TotalPrice) +
                takeoff.AnchorBolts.Sum(a => a.TotalCost) +
                takeoff.Excavation.Sum(e => e.TotalPrice);

            return takeoff;
        }
    }
}
