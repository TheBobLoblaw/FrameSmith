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

            // ── Door openings on endwalls (front & back) ──
            foreach (var door in p.Doors)
            {
                if (door.Wall == WallSide.Front || door.Wall == WallSide.Back)
                {
                    try
                    {
                    var renderer = RendererFactory.GetDoorRenderer(door.Type);
                    }
                    catch (System.Exception) { /* skip failed opening render */ }
                    count += renderer.RenderElevation(tr, btr, door, p.EaveHeight, offset);
                }
            }

            // ── Window openings on endwalls (front & back) ──
            foreach (var window in p.Windows)
            {
                if (window.Wall == WallSide.Front || window.Wall == WallSide.Back)
                {
                    try
                    {
                    var renderer = RendererFactory.GetWindowRenderer(window.Type);
                    }
                    catch (System.Exception) { /* skip failed opening render */ }
                    count += renderer.RenderElevation(tr, btr, window, p.EaveHeight, offset);
                }
            }


            // ── Lean-To profiles (sidewall lean-tos visible in front elevation) ──
            foreach (var ltGeo in geo.LeanToGeometries)
            {
                try
                {
                    count += LeanToGenerator.GenerateFrontElevation(tr, btr, ltGeo, geo, offset);
                }
                catch (System.Exception) { /* skip failed lean-to render */ }
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
