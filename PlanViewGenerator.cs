using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
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

            // ── Ridge line (dashed center line) ──
            Line ridgeLine = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, 0 - p.OverhangGable, offset),
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, p.BuildingLength + p.OverhangGable, offset),
                LayerManager.Layers.Trusses);
            ridgeLine.Linetype = "CENTER";
            count++;

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
                count += DrawDoorInPlan(tr, btr, door, p, offset);
            }

            // ── Window openings ──
            foreach (var window in p.Windows)
            {
                count += DrawWindowInPlan(tr, btr, window, p, offset);
            }

            // ── Dimensions ──
            if (p.AddDimensions)
            {
                count += AddPlanDimensions(tr, btr, geo, offset);
            }

            // ── View label ──
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, -5, offset),
                "PLAN VIEW", 1.5, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        private static int DrawDoorInPlan(Transaction tr, BlockTableRecord btr,
            DoorOpening door, BarnParameters p, Vector3d offset)
        {
            int count = 0;
            double halfW = door.Width / 2.0;
            double doorDepth = 0.5; // visual thickness in plan

            double x, y, w, h;

            switch (door.Wall)
            {
                case WallSide.Front:
                    x = door.CenterOffset - halfW;
                    y = -doorDepth / 2;
                    w = door.Width;
                    h = doorDepth;
                    break;
                case WallSide.Back:
                    x = door.CenterOffset - halfW;
                    y = p.BuildingLength - doorDepth / 2;
                    w = door.Width;
                    h = doorDepth;
                    break;
                case WallSide.Left:
                    x = -doorDepth / 2;
                    y = door.CenterOffset - halfW;
                    w = doorDepth;
                    h = door.Width;
                    break;
                case WallSide.Right:
                    x = p.BuildingWidth - doorDepth / 2;
                    y = door.CenterOffset - halfW;
                    w = doorDepth;
                    h = door.Width;
                    break;
                default:
                    return 0;
            }

            DrawingHelpers.AddRectangle(tr, btr,
                DrawingHelpers.Offset2d(x, y, offset), w, h,
                LayerManager.Layers.Doors);
            count++;

            return count;
        }

        private static int DrawWindowInPlan(Transaction tr, BlockTableRecord btr,
            WindowOpening win, BarnParameters p, Vector3d offset)
        {
            // Windows shown as small rectangles on wall lines (similar to doors but smaller)
            double halfW = win.Width / 2.0;
            double winDepth = 0.33;

            double x, y, w, h;

            switch (win.Wall)
            {
                case WallSide.Front:
                    x = win.CenterOffset - halfW; y = -winDepth / 2; w = win.Width; h = winDepth; break;
                case WallSide.Back:
                    x = win.CenterOffset - halfW; y = p.BuildingLength - winDepth / 2; w = win.Width; h = winDepth; break;
                case WallSide.Left:
                    x = -winDepth / 2; y = win.CenterOffset - halfW; w = winDepth; h = win.Width; break;
                case WallSide.Right:
                    x = p.BuildingWidth - winDepth / 2; y = win.CenterOffset - halfW; w = winDepth; h = win.Width; break;
                default: return 0;
            }

            DrawingHelpers.AddRectangle(tr, btr,
                DrawingHelpers.Offset2d(x, y, offset), w, h,
                LayerManager.Layers.Windows);
            return 1;
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
