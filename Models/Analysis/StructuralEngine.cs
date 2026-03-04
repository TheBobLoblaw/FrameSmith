using System;
using System.Collections.Generic;
using PoleBarnGenerator.Models.Loads;
using PoleBarnGenerator.Models.Design;
using PoleBarnGenerator.Models.Code;

namespace PoleBarnGenerator.Models.Analysis
{
    /// <summary>
    /// Complete structural design result including all member sizes,
    /// load calculations, and code compliance.
    /// </summary>
    public class StructuralDesignResult
    {
        // Load calculations
        public WindLoadResult WindLoads { get; set; }
        public SnowLoadResult SnowLoads { get; set; }
        public LiveLoadResult LiveLoads { get; set; }
        public DeadLoadResult DeadLoads { get; set; }

        // Analysis
        public AnalysisResults Analysis { get; set; }

        // Member designs
        public ColumnResult PostDesign { get; set; }
        public BeamResult GirtDesign { get; set; }
        public BeamResult PurlinDesign { get; set; }
        public BeamResult HeaderDesign { get; set; }
        public FootingResult FoundationDesign { get; set; }

        // Code compliance
        public ComplianceReport CodeCompliance { get; set; }

        // Summary for UI
        public List<MemberScheduleEntry> MemberSchedule { get; set; } = new();
        public string DesignSummary { get; set; }
        public bool AllAdequate { get; set; }
        public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    }

    public class MemberScheduleEntry
    {
        public string Type { get; set; }
        public string Size { get; set; }
        public string Grade { get; set; }
        public string Spacing { get; set; }
        public double Ratio { get; set; }
        public string Status { get; set; }
    }

    /// <summary>
    /// Master structural engineering engine. Runs all calculations in sequence:
    /// loads → analysis → member design → foundation → code check.
    /// </summary>
    public static class StructuralEngine
    {
        public static StructuralDesignResult RunFullAnalysis(BarnGeometry geometry, StructuralParameters structural)
        {
            var result = new StructuralDesignResult();

            // Phase 1: Load calculations
            result.WindLoads = WindLoadCalculator.Calculate(geometry, structural.ToWindParameters());
            result.SnowLoads = SnowLoadCalculator.Calculate(geometry, structural.ToSnowParameters());
            result.LiveLoads = LiveLoadCalculator.Calculate(geometry, structural.BuildingUse, LoadDistribution.Uniform);
            result.DeadLoads = DeadLoadCalculator.Calculate(geometry, structural.ToMaterialProperties());

            // Phase 2: Structural analysis
            result.Analysis = StructuralAnalysis.Analyze(geometry,
                result.WindLoads, result.SnowLoads, result.LiveLoads, result.DeadLoads);

            // Phase 3: Member design
            var postGrade = structural.GetPostGrade();
            var beamGrade = structural.GetBeamGrade();
            var p = geometry.Params;

            // Post design — use worst-case forces from analysis
            double postAxial = 0, postLateral = 0;
            if (result.Analysis.PostForces.Count > 0)
            {
                postAxial = result.Analysis.PostForces[0].AxialForce;
                postLateral = result.Analysis.PostForces[0].ShearForce / (p.EaveHeight / 2.0);
            }
            double windCD = 1.6; // wind load duration factor
            result.PostDesign = ColumnDesign.Design(postAxial, postLateral,
                p.EaveHeight, postGrade, windCD, wetService: true);

            // Girt design
            double girtWindPlf = 0;
            if (result.Analysis.GirtForces.Count > 0)
            {
                double girtM = result.Analysis.GirtForces[0].BendingMoment;
                girtWindPlf = 8 * girtM / (p.ActualBaySpacing * p.ActualBaySpacing);
            }
            result.GirtDesign = BeamDesign.Design(girtWindPlf, girtWindPlf,
                p.ActualBaySpacing, beamGrade, plies: 1, CD: 1.6,
                deflLimitLive: 120, deflLimitTotal: 120); // L/120 for wind on girts

            // Purlin design
            double purlinDL = result.DeadLoads.TotalRoofDead * p.PurlinSpacing / 12.0;
            double purlinSL = result.SnowLoads.DesignRoofSnowLoad * p.PurlinSpacing / 12.0;
            double purlinTotal = purlinDL + purlinSL; // controlling gravity
            result.PurlinDesign = BeamDesign.Design(purlinTotal, purlinSL,
                p.ActualBaySpacing, beamGrade, plies: 1, CD: 1.15,
                deflLimitLive: 240, deflLimitTotal: 180);

            // Foundation design
            double uplift = 0;
            if (result.Analysis.FoundationReactions.Count > 0)
                uplift = result.Analysis.FoundationReactions[0].UpliftForce;
            double lateralReaction = 0;
            if (result.Analysis.FoundationReactions.Count > 0)
                lateralReaction = result.Analysis.FoundationReactions[0].HorizontalReaction;

            result.FoundationDesign = FoundationDesign.Design(
                postAxial, lateralReaction, uplift, p.EaveHeight,
                structural.ToSoilProperties());

            // Phase 4: Code compliance
            result.CodeCompliance = BuildingCodeChecker.Check(geometry, result.Analysis);

            // Build member schedule
            result.MemberSchedule = BuildMemberSchedule(result, p);
            result.AllAdequate = result.PostDesign.IsAdequate &&
                                 result.GirtDesign.IsAdequate &&
                                 result.PurlinDesign.IsAdequate &&
                                 result.FoundationDesign.IsAdequate &&
                                 result.CodeCompliance.IsCompliant;

            result.DesignSummary = BuildSummary(result, p);

            return result;
        }

        private static List<MemberScheduleEntry> BuildMemberSchedule(StructuralDesignResult r, BarnParameters p)
        {
            var schedule = new List<MemberScheduleEntry>();

            schedule.Add(new MemberScheduleEntry
            {
                Type = "Posts",
                Size = r.PostDesign.RequiredSize?.Name ?? p.PostSize,
                Grade = "Treated DF #2",
                Spacing = $"{p.BaySpacing:F0}' OC",
                Ratio = r.PostDesign.CombinedRatio,
                Status = r.PostDesign.IsAdequate ? "OK" : "NG"
            });

            schedule.Add(new MemberScheduleEntry
            {
                Type = "Wall Girts",
                Size = r.GirtDesign.RequiredSize?.Name ?? "2x6",
                Grade = "DF #2",
                Spacing = $"{p.GirtSpacing}\" OC",
                Ratio = r.GirtDesign.MomentCapacityRatio,
                Status = r.GirtDesign.IsAdequate ? "OK" : "NG"
            });

            schedule.Add(new MemberScheduleEntry
            {
                Type = "Roof Purlins",
                Size = r.PurlinDesign.RequiredSize?.Name ?? "2x6",
                Grade = "DF #2",
                Spacing = $"{p.PurlinSpacing}\" OC",
                Ratio = r.PurlinDesign.MomentCapacityRatio,
                Status = r.PurlinDesign.IsAdequate ? "OK" : "NG"
            });

            schedule.Add(new MemberScheduleEntry
            {
                Type = "Footings",
                Size = $"{r.FoundationDesign.FootingDiameter}\" dia",
                Grade = $"{r.FoundationDesign.ConcreteStrength} psi",
                Spacing = $"Embed {r.FoundationDesign.PostEmbedment}\"",
                Ratio = r.FoundationDesign.BearingRatio,
                Status = r.FoundationDesign.IsAdequate ? "OK" : "NG"
            });

            return schedule;
        }

        private static string BuildSummary(StructuralDesignResult r, BarnParameters p)
        {
            return $"═══ STRUCTURAL DESIGN SUMMARY ═══\n" +
                   $"Building: {p.BuildingWidth}'×{p.BuildingLength}'×{p.EaveHeight}' eave, {p.RoofPitchRise}/12 pitch\n" +
                   $"─── LOADS ───\n" +
                   $"  Wind: {r.WindLoads.Summary}\n" +
                   $"  Snow: {r.SnowLoads.Summary}\n" +
                   $"  Live: {r.LiveLoads.Summary}\n" +
                   $"  Dead: {r.DeadLoads.Summary}\n" +
                   $"─── MEMBERS ───\n" +
                   $"  Posts: {r.PostDesign.RequiredSize?.Name ?? p.PostSize} ({r.PostDesign.CombinedRatio:F2}) {(r.PostDesign.IsAdequate ? "✓" : "✗")}\n" +
                   $"  Girts: {r.GirtDesign.RequiredSize?.Name ?? "2x6"} ({r.GirtDesign.MomentCapacityRatio:F2}) {(r.GirtDesign.IsAdequate ? "✓" : "✗")}\n" +
                   $"  Purlins: {r.PurlinDesign.RequiredSize?.Name ?? "2x6"} ({r.PurlinDesign.MomentCapacityRatio:F2}) {(r.PurlinDesign.IsAdequate ? "✓" : "✗")}\n" +
                   $"─── FOUNDATION ───\n" +
                   $"  {r.FoundationDesign.DesignNotes}\n" +
                   $"─── CODE ───\n" +
                   $"  {r.CodeCompliance.Summary}\n" +
                   $"─── RESULT: {(r.AllAdequate ? "ALL ADEQUATE ✓" : "REVIEW REQUIRED ✗")} ───";
        }
    }
}
