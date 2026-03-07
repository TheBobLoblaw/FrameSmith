using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Generators.Services;
using PoleBarnGenerator.Generators.TrussProfiles;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates the plan (top-down) view and coordinates specialized drawing services.
    /// </summary>
    public static class PlanViewGenerator
    {
        public static int Generate(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset, Editor ed, WarningCollector warnings)
        {
            int count = 0;
            var p = geo.Params;

            Database db = btr.Database;
            try { db.LoadLineTypeFile("CENTER", "acad.lin"); }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                WarningCollector.Report(ed, warnings, "Failed to load CENTER linetype for plan view", ex);
            }
            try { db.LoadLineTypeFile("DASHED", "acad.lin"); }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                WarningCollector.Report(ed, warnings, "Failed to load DASHED linetype for plan view", ex);
            }

            if (geo.WallSegments.Any(s => s.IsArc))
            {
                foreach (var segment in geo.WallSegments)
                {
                    if (segment.IsArc)
                    {
                        DrawingHelpers.AddArc(tr, btr,
                            DrawingHelpers.Offset2d(segment.ArcCenter.X, segment.ArcCenter.Y, offset),
                            segment.ArcRadius, segment.StartAngle, segment.EndAngle,
                            LayerManager.Layers.Curved);
                    }
                    else
                    {
                        DrawingHelpers.AddLine(tr, btr,
                            DrawingHelpers.Offset(segment.Start.X, segment.Start.Y, offset),
                            DrawingHelpers.Offset(segment.End.X, segment.End.Y, offset),
                            LayerManager.Layers.Slab);
                    }
                    count++;
                }
            }
            else
            {
                DrawingHelpers.AddPolyline(tr, btr,
                    geo.FootprintOutline.Select(pt => DrawingHelpers.Offset2d(pt.X, pt.Y, offset)).ToList(),
                    LayerManager.Layers.Slab, closed: true);
                count++;
            }

            foreach (var post in geo.Posts.Where(ps => ps.IsPlanInstance))
            {
                double hw = post.PostWidth / 2.0;
                double hd = post.PostDepth / 2.0;

                DrawingHelpers.AddFilledRect(tr, btr,
                    DrawingHelpers.Offset2d(post.X, post.Y, offset),
                    post.PostWidth, post.PostDepth,
                    LayerManager.Layers.Posts);
                count++;

                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(post.X - hw, post.Y - hd, offset),
                    post.PostWidth, post.PostDepth,
                    LayerManager.Layers.Posts);
                count++;
            }

            foreach (var segment in geo.WallSegments)
            {
                if (segment.IsArc)
                {
                    DrawingHelpers.AddArc(tr, btr,
                        DrawingHelpers.Offset2d(segment.ArcCenter.X, segment.ArcCenter.Y, offset),
                        segment.ArcRadius, segment.StartAngle, segment.EndAngle,
                        LayerManager.Layers.Curved);
                }
                else
                {
                    DrawingHelpers.AddLine(tr, btr,
                        DrawingHelpers.Offset(segment.Start.X, segment.Start.Y, offset),
                        DrawingHelpers.Offset(segment.End.X, segment.End.Y, offset),
                        LayerManager.Layers.Girts);
                }
                count++;
            }

            if (p.TrussType != TrussType.MonoSlope)
            {
                Line ridgeLine = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth / 2.0, 0 - p.OverhangGable, offset),
                    DrawingHelpers.Offset(p.BuildingWidth / 2.0, p.BuildingLength + p.OverhangGable, offset),
                    LayerManager.Layers.Trusses);
                ridgeLine.Linetype = "CENTER";
                count++;
            }

            count += geo.TrussProfile.RenderPlanRoofOutline(tr, btr, geo, offset);

            foreach (double bayY in geo.BayPositions)
            {
                if (bayY == 0 || bayY == p.BuildingLength) continue;

                Line bayLine = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, bayY, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, bayY, offset),
                    LayerManager.Layers.Trusses);
                bayLine.Linetype = "DASHED";
                count++;
            }

            var roofOutline = new List<Point2d>
            {
                DrawingHelpers.Offset2d(-p.OverhangEave, -p.OverhangGable, offset),
                DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave, -p.OverhangGable, offset),
                DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave, p.BuildingLength + p.OverhangGable, offset),
                DrawingHelpers.Offset2d(-p.OverhangEave, p.BuildingLength + p.OverhangGable, offset),
            };
            DrawingHelpers.AddPolyline(tr, btr, roofOutline, LayerManager.Layers.Roof, closed: true);
            count++;

            if (geo.FloorFraming.Count > 0)
            {
                foreach (var frame in geo.FloorFraming)
                {
                    if (frame.IsArc)
                    {
                        DrawingHelpers.AddArc(tr, btr,
                            DrawingHelpers.Offset2d(frame.ArcCenter.X, frame.ArcCenter.Y, offset),
                            frame.ArcRadius, frame.StartAngle, frame.EndAngle,
                            LayerManager.Layers.Floor);
                    }
                    else
                    {
                        DrawingHelpers.AddLine(tr, btr,
                            DrawingHelpers.Offset(frame.Start.X, frame.Start.Y, offset),
                            DrawingHelpers.Offset(frame.End.X, frame.End.Y, offset),
                            LayerManager.Layers.Floor);
                    }
                    count++;
                }
            }

            foreach (var joint in geo.ExpansionJoints)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(-p.OverhangEave, joint.Location, offset),
                    DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, joint.Location, offset),
                    LayerManager.Layers.Joint);
                count++;

                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth * 0.5, joint.Location + 0.5, offset),
                    $"EJ {joint.GapWidth:F2}' {joint.JointType}",
                    0.5, LayerManager.Layers.JointDetail);
                count++;
            }

            foreach (var valley in geo.RoofIntersectionPoints)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(valley.X, valley.Y, offset),
                    DrawingHelpers.Offset(p.BuildingWidth / 2.0, p.BuildingLength / 2.0, offset),
                    LayerManager.Layers.RoofHidden);
                count++;
            }

            count += OpeningDrawingService.DrawPlanOpenings(tr, btr, geo, offset, ed, warnings);

            if (p.AddDimensions)
            {
                count += DimensionScaffoldingService.AddPlanDimensions(tr, btr, geo, offset);
            }

            foreach (var ltGeo in geo.LeanToGeometries)
            {
                try
                {
                    count += LeanToGenerator.GeneratePlan(tr, btr, ltGeo, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Lean-to plan generation failed", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Lean-to plan generation unexpected failure", ex);
                }
            }

            foreach (var porchGeo in geo.PorchGeometries)
            {
                try
                {
                    count += PorchGenerator.GeneratePlan(tr, btr, porchGeo, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Porch plan generation failed", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Porch plan generation unexpected failure", ex);
                }
            }

            count += ExteriorDetailDrawingService.AddPlanExteriorDetails(tr, btr, geo, offset, ed, warnings);

            if (geo.InteriorGeometry != null)
            {
                try
                {
                    var interior = geo.InteriorGeometry;

                    if (interior.StallLayout != null)
                        count += InteriorGenerator.GenerateHorseStalls(tr, btr, geo, interior.StallLayout, offset);

                    if (interior.LoftGeometry != null)
                        count += InteriorGenerator.GenerateLoft(tr, btr, geo, interior.LoftGeometry, offset);

                    if (interior.PartitionGeometries?.Count > 0)
                        count += InteriorGenerator.GeneratePartitions(tr, btr, geo, interior.PartitionGeometries, offset);

                    if (interior.WorkshopFeatures?.Count > 0)
                        count += InteriorGenerator.GenerateWorkshopFeatures(tr, btr, geo, interior.WorkshopFeatures, offset);

                    if (interior.DairyLayout != null)
                        count += InteriorGenerator.GenerateDairyLayout(tr, btr, geo, interior.DairyLayout, offset);

                    if (interior.EquipmentLayout != null)
                        count += InteriorGenerator.GenerateEquipmentStorageLayout(tr, btr, interior.EquipmentLayout, offset);

                    if (interior.DrainageLayout != null)
                        count += InteriorGenerator.GenerateDrainageLayout(tr, btr, interior.DrainageLayout, offset);

                    if (interior.GrainStorageLayout != null)
                        count += InteriorGenerator.GenerateGrainStorageLayout(tr, btr, interior.GrainStorageLayout, offset);

                    if (interior.MachineryLayout != null)
                        count += InteriorGenerator.GenerateMachineryLayout(tr, btr, interior.MachineryLayout, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Interior plan generation failed", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Interior plan generation unexpected failure", ex);
                }
            }

            count += GridBubbleGenerator.Generate(tr, btr, geo, offset);

            count += ViewLabelGenerator.AddViewLabel(tr, btr,
                "PLAN VIEW", "1/4\" = 1'-0\"",
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, -5, offset));

            return count;
        }
    }
}
