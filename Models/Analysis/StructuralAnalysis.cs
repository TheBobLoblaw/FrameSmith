using System;
using System.Collections.Generic;
using System.Linq;
using PoleBarnGenerator.Models.Loads;
using PoleBarnGenerator.Models.Design;

namespace PoleBarnGenerator.Models.Analysis
{
    public class MemberForces
    {
        public string MemberType { get; set; }
        public string Location { get; set; }
        public double AxialForce { get; set; }      // lbs (+ compression)
        public double ShearForce { get; set; }       // lbs
        public double BendingMoment { get; set; }    // ft-lbs
        public double Deflection { get; set; }       // inches
        public string ControllingCombo { get; set; }
    }

    public class ReactionForces
    {
        public string Location { get; set; }
        public double VerticalReaction { get; set; }   // lbs (+ downward)
        public double HorizontalReaction { get; set; } // lbs
        public double Moment { get; set; }             // ft-lbs
        public double UpliftForce { get; set; }        // lbs (net upward)
        public string ControllingCombo { get; set; }
    }

    public class AnalysisResults
    {
        public List<MemberForces> PostForces { get; set; } = new();
        public List<MemberForces> GirtForces { get; set; } = new();
        public List<MemberForces> PurlinForces { get; set; } = new();
        public List<MemberForces> TrussForces { get; set; } = new();
        public List<ReactionForces> FoundationReactions { get; set; } = new();
        public double MaxDrift { get; set; }           // inches
        public string ControllingLoadCombo { get; set; }
        public string Summary { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Simplified structural analysis for post-frame buildings.
    /// Distributes loads through the structural system and determines member forces.
    /// </summary>
    public static class StructuralAnalysis
    {
        public static AnalysisResults Analyze(BarnGeometry geometry,
            WindLoadResult windLoads, SnowLoadResult snowLoads,
            LiveLoadResult liveLoads, DeadLoadResult deadLoads)
        {
            var p = geometry.Params;
            var results = new AnalysisResults();

            // ASD load combinations
            var combos = LoadCombinations.GetASDCombinations();

            // Per-unit loads (psf → plf based on tributary)
            double D_roof = deadLoads.TotalRoofDead; // psf
            double S = snowLoads.DesignRoofSnowLoad;
            double Lr = liveLoads.ReducedRoofLiveLoad;
            double W_roof = Math.Abs(windLoads.UpliftPressure);
            double W_wall = Math.Abs(windLoads.WindwardWallPressure);

            // ─── Post analysis ───
            double baySpacing = p.ActualBaySpacing;
            double tributaryRoof = baySpacing * p.BuildingWidth / 2.0; // sq ft per post
            double postAxialD = D_roof * tributaryRoof / 2.0; // dead to each side
            double postAxialS = S * baySpacing * p.BuildingWidth / 4.0; // per sidewall post
            double postAxialLr = Lr * baySpacing * p.BuildingWidth / 4.0;
            double postLateral = W_wall * baySpacing; // plf on post from wind

            // Find controlling gravity combo
            var (gravCombo, maxGravity) = LoadCombinations.FindControlling(combos,
                postAxialD, 0, postAxialLr, postAxialS, 0);

            // Find controlling lateral+gravity
            var (latCombo, maxLateral) = LoadCombinations.FindControlling(combos,
                postAxialD, 0, postAxialLr, postAxialS, postLateral * p.EaveHeight / 2.0);

            results.PostForces.Add(new MemberForces
            {
                MemberType = "Sidewall Post (Interior)",
                Location = "Typical Interior Bay",
                AxialForce = Math.Round(maxGravity, 0),
                ShearForce = Math.Round(postLateral * p.EaveHeight / 2.0 * 0.6, 0), // ASD wind
                BendingMoment = Math.Round(postLateral * 0.6 * p.EaveHeight * p.EaveHeight / 8.0, 0),
                ControllingCombo = latCombo.Name
            });

            // Corner posts get more wind
            results.PostForces.Add(new MemberForces
            {
                MemberType = "Corner Post",
                Location = "Building Corners",
                AxialForce = Math.Round(maxGravity * 0.5, 0), // half tributary
                ShearForce = Math.Round(postLateral * p.EaveHeight / 2.0 * 0.6 * 1.2, 0),
                BendingMoment = Math.Round(postLateral * 0.6 * p.EaveHeight * p.EaveHeight / 8.0 * 1.2, 0),
                ControllingCombo = "D + 0.6W"
            });

            // ─── Girt analysis ───
            double girtTrib = p.GirtSpacing / 12.0;
            double girtWindLoad = W_wall * 0.6 * girtTrib; // plf ASD
            double girtSpan = baySpacing;
            double girtMoment = girtWindLoad * girtSpan * girtSpan / 8.0;
            double girtShear = girtWindLoad * girtSpan / 2.0;

            results.GirtForces.Add(new MemberForces
            {
                MemberType = "Sidewall Girt",
                Location = $"Typical @ {p.GirtSpacing}\" OC",
                BendingMoment = Math.Round(girtMoment, 1),
                ShearForce = Math.Round(girtShear, 0),
                ControllingCombo = "D + 0.6W"
            });

            // ─── Purlin analysis ───
            double purlinTrib = p.PurlinSpacing / 12.0;
            double purlinDL = D_roof * purlinTrib; // plf dead
            double purlinSL = S * purlinTrib;      // plf snow
            double purlinSpan = baySpacing;

            var (purlinCombo, purlinMaxLoad) = LoadCombinations.FindControlling(combos,
                purlinDL, 0, Lr * purlinTrib, purlinSL, Math.Abs(windLoads.RoofWindwardPressure) * purlinTrib);

            double purlinTotalPlf = purlinMaxLoad / purlinSpan; // approximate
            double purlinMoment = purlinMaxLoad * purlinSpan / 8.0;
            double purlinShear = purlinMaxLoad / 2.0;

            results.PurlinForces.Add(new MemberForces
            {
                MemberType = "Roof Purlin",
                Location = $"Typical @ {p.PurlinSpacing}\" OC",
                BendingMoment = Math.Round(purlinMoment, 1),
                ShearForce = Math.Round(purlinShear, 0),
                ControllingCombo = purlinCombo.Name
            });

            // ─── Foundation reactions ───
            var (upliftCombo, upliftLoad) = LoadCombinations.FindUpliftControlling(combos,
                postAxialD, 0, 0, 0, -W_roof * tributaryRoof / 2.0);

            results.FoundationReactions.Add(new ReactionForces
            {
                Location = "Typical Interior Post",
                VerticalReaction = Math.Round(maxGravity, 0),
                HorizontalReaction = Math.Round(postLateral * p.EaveHeight / 2.0 * 0.6, 0),
                Moment = Math.Round(postLateral * 0.6 * p.EaveHeight * p.EaveHeight / 8.0, 0),
                UpliftForce = upliftLoad < 0 ? Math.Round(Math.Abs(upliftLoad), 0) : 0,
                ControllingCombo = gravCombo.Name
            });

            results.FoundationReactions.Add(new ReactionForces
            {
                Location = "Corner Post",
                VerticalReaction = Math.Round(maxGravity * 0.5, 0),
                HorizontalReaction = Math.Round(postLateral * p.EaveHeight / 2.0 * 0.6 * 1.2, 0),
                UpliftForce = upliftLoad < 0 ? Math.Round(Math.Abs(upliftLoad) * 0.6, 0) : 0,
                ControllingCombo = "D + 0.6W"
            });

            // ─── Building drift ───
            double drift = W_wall * 0.6 * Math.Pow(p.EaveHeight, 4) * 12 /
                           (8 * 1600000 * 500); // simplified, approximate
            results.MaxDrift = Math.Round(drift, 3);

            results.ControllingLoadCombo = latCombo.Name;
            results.Summary = $"Analysis: {combos.Count} load combos | " +
                              $"Max post axial = {maxGravity:F0} lbs ({gravCombo.Name}) | " +
                              $"Max girt moment = {girtMoment:F0} ft-lbs | " +
                              $"Max purlin moment = {purlinMoment:F0} ft-lbs";

            // Warnings
            if (upliftLoad < 0)
                results.Warnings.Add($"NET UPLIFT: {Math.Abs(upliftLoad):F0} lbs at interior posts — verify hold-down");
            if (drift > p.EaveHeight * 12 / 60.0)
                results.Warnings.Add($"Building drift {drift:F2}\" exceeds H/60 limit");

            return results;
        }
    }
}
