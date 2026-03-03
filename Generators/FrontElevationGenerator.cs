using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Generators.Renderers;
using PoleBarnGenerator.Utils;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates the front elevation view (looking at the front endwall).
    /// Shows: posts, girts, roof profile with peak, door/window openings,
    /// eave overhang, and dimensions.
    /// 
    /// Coordinate mapping: X = building width, Y = height (elevation)
    /// </summary>
    public static class FrontElevationGenerator
    {
        public static int Generate(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;

            // ── Ground line ──
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-2, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth + 2, 0, offset),
                LayerManager.Layers.Slab);
            count++;

            // ── Corner posts (vertical lines) ──
            // Left post
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                LayerManager.Layers.Posts);
            count++;

            // Right post
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth, p.EaveHeight, offset),
                LayerManager.Layers.Posts);
            count++;

            // Center post (if present on endwall)
            if (p.BuildingWidth > 24)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth / 2, 0, offset),
                    DrawingHelpers.Offset(p.BuildingWidth / 2, geo.PeakHeight, offset),
                    LayerManager.Layers.Posts);
                count++;
            }

            // ── Girts (horizontal lines between posts) ──
            foreach (var girt in geo.Girts)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, girt.Elevation, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, girt.Elevation, offset),
                    LayerManager.Layers.Girts);
                count++;
            }

            // ── Roof profile ──
            double halfW = p.BuildingWidth / 2.0;

            // Left eave to peak
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight - p.OverhangEave * (p.RoofPitchRise / 12.0), offset),
                DrawingHelpers.Offset(halfW, geo.PeakHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Peak to right eave
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(halfW, geo.PeakHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight - p.OverhangEave * (p.RoofPitchRise / 12.0), offset),
                LayerManager.Layers.Roof);
            count++;

            // Eave lines (horizontal at top of wall)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight, offset),
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Overhang fascia (vertical lines at eave ends)
            double overhangDrop = p.OverhangEave * (p.RoofPitchRise / 12.0);
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight, offset),
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            // ── Door openings on front wall ──
            foreach (var door in p.Doors)
            {
                if (door.Wall == WallSide.Front)
                {
                    var renderer = RendererFactory.GetDoorRenderer(door.Type);
                    count += renderer.RenderElevation(tr, btr, door, p.EaveHeight, offset);
                }
            }

            // ── Window openings on front wall ──
            foreach (var window in p.Windows)
            {
                if (window.Wall == WallSide.Front)
                {
                    var renderer = RendererFactory.GetWindowRenderer(window.Type);
                    count += renderer.RenderElevation(tr, btr, window, p.EaveHeight, offset);
                }
            }

            // ── Dimensions ──
            if (p.AddDimensions)
            {
                count += AddFrontDimensions(tr, btr, geo, offset);
            }

            // ── View label ──
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, -3, offset),
                "FRONT ELEVATION", 1.5, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        private static int DrawDoorElevation(Transaction tr, BlockTableRecord btr,
            DoorOpening door, Vector3d offset)
        {
            double left = door.CenterOffset - door.Width / 2.0;
            var pts = new List<Point2d>
            {
                DrawingHelpers.Offset2d(left, 0, offset),
                DrawingHelpers.Offset2d(left, door.Height, offset),
                DrawingHelpers.Offset2d(left + door.Width, door.Height, offset),
                DrawingHelpers.Offset2d(left + door.Width, 0, offset),
            };
            DrawingHelpers.AddPolyline(tr, btr, pts, LayerManager.Layers.Doors, closed: false);

            // Door type label
            string label = door.Type == DoorType.Overhead ? "OH" :
                          door.Type == DoorType.Sliding ? "SL" : "WK";
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(door.CenterOffset, door.Height / 2, offset),
                $"{label}\n{door.Width}' x {door.Height}'",
                0.5, LayerManager.Layers.Anno);

            return 2;
        }

        private static int DrawWindowElevation(Transaction tr, BlockTableRecord btr,
            WindowOpening win, Vector3d offset)
        {
            double left = win.CenterOffset - win.Width / 2.0;
            DrawingHelpers.AddRectangle(tr, btr,
                DrawingHelpers.Offset2d(left, win.SillHeight, offset),
                win.Width, win.Height,
                LayerManager.Layers.Windows);

            // Window cross lines
            double cx = win.CenterOffset;
            double cy = win.SillHeight + win.Height / 2.0;
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(left, cy, offset),
                DrawingHelpers.Offset(left + win.Width, cy, offset),
                LayerManager.Layers.Windows);
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(cx, win.SillHeight, offset),
                DrawingHelpers.Offset(cx, win.SillHeight + win.Height, offset),
                LayerManager.Layers.Windows);

            return 3;
        }

        private static int AddFrontDimensions(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double dimOff = 3.0;

            // Building width
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth / 2, -dimOff, offset),
                LayerManager.Layers.Dims);
            count++;

            // Eave height (left side)
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                DrawingHelpers.Offset(-dimOff, p.EaveHeight / 2, offset),
                LayerManager.Layers.Dims);
            count++;

            // Peak height (center)
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth / 2, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth / 2, geo.PeakHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth / 2 + dimOff, geo.PeakHeight / 2, offset),
                LayerManager.Layers.Dims);
            count++;

            // Pitch annotation
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth * 0.25, p.EaveHeight + geo.RoofRise * 0.6, offset),
                $"{p.RoofPitchDisplay} PITCH",
                0.75, LayerManager.Layers.Anno);
            count++;

            return count;
        }
    }
}
