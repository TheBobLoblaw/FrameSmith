using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Generators.Renderers;
using PoleBarnGenerator.Utils;
using System;
using PoleBarnGenerator.Generators.TrussProfiles;
using System.Collections.Generic;

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
            DrawingHelpers.AddRectangle(tr, btr,
                DrawingHelpers.Offset2d(0, 0, offset),
                p.BuildingWidth, p.BuildingLength,
                LayerManager.Layers.Slab);
            count++;

            // ── Posts ──
            foreach (var post in geo.Posts)
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

            // ── Wall lines (girts shown as wall outlines in plan) ──
            // Left sidewall
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(0, p.BuildingLength, offset),
                LayerManager.Layers.Girts);
            count++;

            // Right sidewall
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth, p.BuildingLength, offset),
                LayerManager.Layers.Girts);
            count++;

            // Front endwall
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth, 0, offset),
                LayerManager.Layers.Girts);
            count++;

            // Back endwall
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(0, p.BuildingLength, offset),
                DrawingHelpers.Offset(p.BuildingWidth, p.BuildingLength, offset),
                LayerManager.Layers.Girts);
            count++;

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

            // ── View label ──
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, -5, offset),
                "PLAN VIEW", 1.5, LayerManager.Layers.Anno);
            count++;

            return count;
        }



        private static int AddPlanDimensions(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double dimOffset = 3.0;

            // Overall width dimension (bottom)
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth / 2, -dimOffset, offset),
                LayerManager.Layers.Dims);
            count++;

            // Overall length dimension (left side)
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(0, p.BuildingLength, offset),
                DrawingHelpers.Offset(-dimOffset, p.BuildingLength / 2, offset),
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
