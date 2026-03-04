using System;
using System.Collections.Generic;
using PoleBarnGenerator.Models.Analysis;
using PoleBarnGenerator.Models.Design;

namespace PoleBarnGenerator.Models.Code
{
    public enum IssueSeverity { Info, Warning, Error }
    public enum BuildingCode { IBC2021, IBC2018, IRC2021 }

    public class ComplianceIssue
    {
        public IssueSeverity Severity { get; set; }
        public string Description { get; set; }
        public string CodeSection { get; set; }
        public string Resolution { get; set; }
        public bool RequiresEngineerReview { get; set; }
    }

    public class CodeReference
    {
        public string Code { get; set; }
        public string Section { get; set; }
        public string Description { get; set; }
    }

    public class ComplianceReport
    {
        public bool IsCompliant { get; set; }
        public List<ComplianceIssue> Issues { get; set; } = new();
        public List<CodeReference> ApplicableCodes { get; set; } = new();
        public string JurisdictionNotes { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
        public int ErrorCount => Issues.FindAll(i => i.Severity == IssueSeverity.Error).Count;
        public int WarningCount => Issues.FindAll(i => i.Severity == IssueSeverity.Warning).Count;
        public string Summary { get; set; }
    }

    /// <summary>
    /// IBC 2021 compliance checking for agricultural/commercial post-frame buildings.
    /// </summary>
    public static class BuildingCodeChecker
    {
        public static ComplianceReport Check(BarnGeometry geometry,
            AnalysisResults analysis, BuildingCode code = BuildingCode.IBC2021)
        {
            var p = geometry.Params;
            var report = new ComplianceReport();

            report.ApplicableCodes.Add(new CodeReference
            {
                Code = "IBC 2021", Section = "Chapter 16", Description = "Structural Design"
            });
            report.ApplicableCodes.Add(new CodeReference
            {
                Code = "IBC 2021", Section = "Chapter 23", Description = "Wood Construction"
            });
            report.ApplicableCodes.Add(new CodeReference
            {
                Code = "ASCE 7-22", Section = "Chapters 2-7,26-30", Description = "Loads"
            });
            report.ApplicableCodes.Add(new CodeReference
            {
                Code = "NDS 2024", Section = "All", Description = "Wood Design"
            });

            // Height limits — IBC Table 504.3 (Type VB, Group S-1)
            double maxHeight = 40; // feet for Type VB agricultural
            if (p.PeakHeight > maxHeight)
            {
                report.Issues.Add(new ComplianceIssue
                {
                    Severity = IssueSeverity.Error,
                    Description = $"Building height {p.PeakHeight:F1}' exceeds {maxHeight}' limit for Type VB",
                    CodeSection = "IBC §504.3",
                    Resolution = "Reduce height or upgrade construction type",
                    RequiresEngineerReview = true
                });
            }

            // Area limits — IBC Table 506.2 (Type VB, S-1: 17,500 sf single story)
            double area = p.BuildingWidth * p.BuildingLength;
            double maxArea = 17500; // Type VB, S-1, single story with sprinklers
            if (area > maxArea)
            {
                report.Issues.Add(new ComplianceIssue
                {
                    Severity = IssueSeverity.Warning,
                    Description = $"Building area {area:F0} sf may exceed {maxArea} sf limit for Type VB, S-1",
                    CodeSection = "IBC §506.2",
                    Resolution = "Verify occupancy classification; agricultural may be exempt per IBC §312",
                    RequiresEngineerReview = true
                });
            }

            // Agricultural exemption — IBC §312
            report.Issues.Add(new ComplianceIssue
            {
                Severity = IssueSeverity.Info,
                Description = "Agricultural buildings may be exempt from certain IBC provisions per §312",
                CodeSection = "IBC §312",
                Resolution = "Verify with AHJ (Authority Having Jurisdiction)"
            });

            // Post spacing — check lateral system adequacy
            if (p.BaySpacing > 12)
            {
                report.Issues.Add(new ComplianceIssue
                {
                    Severity = IssueSeverity.Warning,
                    Description = $"Bay spacing {p.BaySpacing}' exceeds typical 12' maximum for girt spans",
                    CodeSection = "NDS §3.3 / Girt capacity",
                    Resolution = "Verify girt bending and deflection for increased span"
                });
            }

            // Eave height vs post size
            if (p.EaveHeight > 14 && p.PostWidthInches < 6)
            {
                report.Issues.Add(new ComplianceIssue
                {
                    Severity = IssueSeverity.Warning,
                    Description = $"Eave height {p.EaveHeight}' with {p.PostSize} posts — verify column capacity",
                    CodeSection = "NDS §3.7, §3.9",
                    Resolution = "Consider upgrading to 6x6 or larger posts",
                    RequiresEngineerReview = true
                });
            }

            // Roof slope — minimum for metal panels
            if (p.RoofPitchRise < 1)
            {
                report.Issues.Add(new ComplianceIssue
                {
                    Severity = IssueSeverity.Error,
                    Description = $"Roof pitch {p.RoofPitchRise}/12 is below minimum for metal panels",
                    CodeSection = "IBC §1507, Manufacturer specs",
                    Resolution = "Increase roof pitch to minimum 1/12 for standing seam, 3/12 for exposed fastener"
                });
            }
            else if (p.RoofPitchRise < 3)
            {
                report.Issues.Add(new ComplianceIssue
                {
                    Severity = IssueSeverity.Warning,
                    Description = $"Roof pitch {p.RoofPitchRise}/12 — verify panel type compatibility",
                    CodeSection = "IBC §1507",
                    Resolution = "Standing seam panels required below 3/12; verify sealant requirements"
                });
            }

            // Girt spacing for cladding
            if (p.GirtSpacing > 36)
            {
                report.Issues.Add(new ComplianceIssue
                {
                    Severity = IssueSeverity.Warning,
                    Description = $"Girt spacing {p.GirtSpacing}\" may exceed panel span capacity",
                    CodeSection = "Panel manufacturer specs",
                    Resolution = "Verify cladding span rating or reduce girt spacing"
                });
            }

            // Purlin spacing for cladding
            if (p.PurlinSpacing > 48)
            {
                report.Issues.Add(new ComplianceIssue
                {
                    Severity = IssueSeverity.Warning,
                    Description = $"Purlin spacing {p.PurlinSpacing}\" may exceed roof panel span capacity",
                    CodeSection = "Panel manufacturer specs",
                    Resolution = "Verify roof panel span rating or reduce purlin spacing"
                });
            }

            // Uplift warnings
            if (analysis?.Warnings != null)
            {
                foreach (var warning in analysis.Warnings)
                {
                    report.Issues.Add(new ComplianceIssue
                    {
                        Severity = IssueSeverity.Warning,
                        Description = warning,
                        CodeSection = "ASCE 7-22 Load Combinations",
                        Resolution = "Engineer to verify connections and hold-downs",
                        RequiresEngineerReview = true
                    });
                }
            }

            // Egress — basic check
            int doorCount = p.Doors?.Count ?? 0;
            if (doorCount == 0 || !p.Doors.Exists(d => d.Type == DoorType.Walk || d.Type == DoorType.Double))
            {
                report.Issues.Add(new ComplianceIssue
                {
                    Severity = IssueSeverity.Warning,
                    Description = "No personnel door (walk/double) found — verify egress requirements",
                    CodeSection = "IBC Chapter 10 - Egress",
                    Resolution = "Add at least one 3'×6'8\" walk door for egress"
                });
            }

            // Foundation / frost depth
            report.Issues.Add(new ComplianceIssue
            {
                Severity = IssueSeverity.Info,
                Description = "Post embedment must extend below frost line per local requirements",
                CodeSection = "IBC §1809.5",
                Resolution = "Verify local frost depth with building department"
            });

            report.IsCompliant = report.ErrorCount == 0;
            report.Summary = $"Code Check: {report.Issues.Count} items " +
                             $"({report.ErrorCount} errors, {report.WarningCount} warnings) — " +
                             (report.IsCompliant ? "COMPLIANT" : "NON-COMPLIANT — review required");

            return report;
        }
    }
}
