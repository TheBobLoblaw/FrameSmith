using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Generators.Renderers;
using PoleBarnGenerator.Utils;
using System;
using PoleBarnGenerator.Generators.TrussProfiles;
using System.Collections.Generic;
using System.Linq;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates the plan (top-down) view showing:
    /// - Post locations as filled rectangles
    /// - Slab/foundation outline
    /// - Wall girt lines along sidewalls and endwalls
    /// - Truss ridge line (dashed center line)
    /// - Door/window openings as breaks in wall lines
    /// - Bay spacing dimensions
    /// </summary>
    public static class PlanViewGenerator
    {
        public static int Generate(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;

            // Load linetypes to prevent crash if not already present in drawing
            Database db = btr.Database;
            try { db.LoadLineTypeFile("CENTER", "acad.lin"); }
            catch (Autodesk.AutoCAD.Runtime.Exception) { /* already loaded */ }
            try { db.LoadLineTypeFile("DASHED", "acad.lin"); }
            catch (Autodesk.AutoCAD.Runtime.Exception) { /* already loaded */ }

            // ── Slab / Foundation outline ──
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

            // ── Posts ──
            foreach (var post in geo.Posts.Where(ps => ps.IsPlanInstance))
            {
                double hw = post.PostWidth / 2.0;
                double hd = post.PostDepth / 2.0;

                DrawingHelpers.AddFilledRect(tr, btr,
                    DrawingHelpers.Offset2d(post.X, post.Y, offset),
                    post.PostWidth, post.PostDepth,
                    LayerManager.Layers.Posts);
                count++;

                // Also draw a rectangle outline around each post
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(post.X - hw, post.Y - hd, offset),
                    post.PostWidth, post.PostDepth,
                    LayerManager.Layers.Posts);
                count++;
            }

            // ── Wall lines ──
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

            // ── Ridge line (dashed center line) — varies by truss type ──
            if (p.TrussType != TrussType.MonoSlope)
            {
                Line ridgeLine = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth / 2.0, 0 - p.OverhangGable, offset),
                    DrawingHelpers.Offset(p.BuildingWidth / 2.0, p.BuildingLength + p.OverhangGable, offset),
                    LayerManager.Layers.Trusses);
                ridgeLine.Linetype = "CENTER";
                count++;
            }

            // ── Truss-type-specific plan roof outlines ──
            count += geo.TrussProfile.RenderPlanRoofOutline(tr, btr, geo, offset);

            // ── Bay lines (dashed) ──
            foreach (double bayY in geo.BayPositions)
            {
                if (bayY == 0 || bayY == p.BuildingLength) continue; // skip endwalls

                Line bayLine = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, bayY, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, bayY, offset),
                    LayerManager.Layers.Trusses);
                bayLine.Linetype = "DASHED";
                count++;
            }

            // ── Roof overhang outline ──
            var roofOutline = new List<Point2d>
            {
                DrawingHelpers.Offset2d(-p.OverhangEave, -p.OverhangGable, offset),
                DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave, -p.OverhangGable, offset),
                DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave, p.BuildingLength + p.OverhangGable, offset),
                DrawingHelpers.Offset2d(-p.OverhangEave, p.BuildingLength + p.OverhangGable, offset),
            };
            DrawingHelpers.AddPolyline(tr, btr, roofOutline, LayerManager.Layers.Roof, closed: true);
            count++;

            // ── Floor framing (multi-story) ──
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

            // ── Expansion joints ──
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

            // ── Roof intersections (valleys/hips for compound footprints) ──
            foreach (var valley in geo.RoofIntersectionPoints)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(valley.X, valley.Y, offset),
                    DrawingHelpers.Offset(p.BuildingWidth / 2.0, p.BuildingLength / 2.0, offset),
                    LayerManager.Layers.RoofHidden);
                count++;
            }

            // ── Door openings (shown as breaks/rectangles on walls) ──
            foreach (var door in p.Doors)
            {
                try
                {
                    var wallGeo = new WallGeometry(p, door.Wall);
                    var renderer = RendererFactory.GetDoorRenderer(door.Type);
                    count += renderer.RenderPlan(tr, btr, door, wallGeo, offset);
                }
                catch (System.Exception) { /* skip failed opening render */ }
            }

            // ── Window openings ──
            foreach (var window in p.Windows)
            {
                try
                {
                    var wallGeo = new WallGeometry(p, window.Wall);
                    var renderer = RendererFactory.GetWindowRenderer(window.Type);
                    count += renderer.RenderPlan(tr, btr, window, wallGeo, offset);
                }
                catch (System.Exception) { /* skip failed opening render */ }
            }

            // ── Dimensions ──
            if (p.AddDimensions)
            {
                count += AddPlanDimensions(tr, btr, geo, offset);
            }


            // ── Lean-To structures ──
            foreach (var ltGeo in geo.LeanToGeometries)
            {
                try
                {
                    count += LeanToGenerator.GeneratePlan(tr, btr, ltGeo, offset);
                }
                catch (System.Exception) { /* skip failed lean-to render */ }
            }

            // ── Porch structures ──
            foreach (var porchGeo in geo.PorchGeometries)
            {
                try
                {
                    count += PorchGenerator.GeneratePlan(tr, btr, porchGeo, offset);
                }
                catch (System.Exception) { /* skip failed porch render */ }
            }

            // ── Exterior details ──
            try
            {
                count += ExteriorDetailGenerator.AddCupolaPlan(tr, btr, geo, p.Cupola, offset);
                count += ExteriorDetailGenerator.AddGutterPlan(tr, btr, geo, p.Gutters, offset);
            }
            catch (System.Exception) { /* skip failed detail render */ }

            // ── Interior features ──
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
                }
                catch (System.Exception) { /* skip failed interior render */ }
            }

            // ── Grid system ──
            count += GridBubbleGenerator.Generate(tr, btr, geo, offset);

            // ── View label ──
            count += ViewLabelGenerator.AddViewLabel(tr, btr,
                "PLAN VIEW", "1/4\" = 1'-0\"",
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, -5, offset));

            return count;
        }



        private static int AddPlanDimensions(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double dimOffset = 3.0;
            double minX = geo.FootprintOutline.Min(pt => pt.X);
            double maxX = geo.FootprintOutline.Max(pt => pt.X);
            double minY = geo.FootprintOutline.Min(pt => pt.Y);
            double maxY = geo.FootprintOutline.Max(pt => pt.Y);

            // Overall width dimension (bottom)
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(minX, minY, offset),
                DrawingHelpers.Offset(maxX, minY, offset),
                DrawingHelpers.Offset((minX + maxX) / 2, minY - dimOffset, offset),
                LayerManager.Layers.Dims);
            count++;

            // Overall length dimension (left side)
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(minX, minY, offset),
                DrawingHelpers.Offset(minX, maxY, offset),
                DrawingHelpers.Offset(minX - dimOffset, (minY + maxY) / 2, offset),
                LayerManager.Layers.Dims);
            count++;

            // Bay spacing dims (right side)
            for (int i = 0; i < geo.BayPositions.Count - 1; i++)
            {
                double y1 = geo.BayPositions[i];
                double y2 = geo.BayPositions[i + 1];

                DrawingHelpers.AddAlignedDim(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth, y1, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, y2, offset),
                    DrawingHelpers.Offset(p.BuildingWidth + dimOffset, (y1 + y2) / 2, offset),
                    LayerManager.Layers.Dims);
                count++;
            }

            return count;
        }
    }
}
