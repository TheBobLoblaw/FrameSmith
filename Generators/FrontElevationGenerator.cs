using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Generators.Renderers;
using PoleBarnGenerator.Generators.TrussProfiles;
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
                // For mono-slope, center post height is mid-slope; for gable types it's at peak
                double centerPostHeight;
                if (p.TrussType == TrussType.MonoSlope)
                    centerPostHeight = geo.PeakHeight - (geo.PeakHeight - p.EaveHeight) * 0.5;
                else
                    centerPostHeight = geo.PeakHeight;

                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth / 2, 0, offset),
                    DrawingHelpers.Offset(p.BuildingWidth / 2, centerPostHeight, offset),
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

            // ── Roof profile (strategy pattern) ──
            count += geo.TrussProfile.RenderFrontElevation(tr, btr, geo, offset);

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

            // ── Porch profiles (sidewall porches visible in front elevation) ──
            foreach (var porchGeo in geo.PorchGeometries)
            {
                try
                {
                    count += PorchGenerator.GenerateFrontElevation(tr, btr, porchGeo, geo, offset);
                }
                catch (System.Exception) { /* skip failed porch render */ }
            }

            // ── Exterior details ──
            try
            {
                count += ExteriorDetailGenerator.AddWainscotFrontElevation(tr, btr, geo, p.Wainscot, offset);
                count += ExteriorDetailGenerator.AddCupolaFrontElevation(tr, btr, geo, p.Cupola, offset);
                count += ExteriorDetailGenerator.AddGutterFrontElevation(tr, btr, geo, p.Gutters, offset);
            }
            catch (System.Exception) { /* skip failed detail render */ }

            // ── Dimensions ──
            if (p.AddDimensions)
            {
                count += AddFrontDimensions(tr, btr, geo, offset);
            }

            // ── View label ──
            count += ViewLabelGenerator.AddViewLabel(tr, btr,
                "FRONT ELEVATION", "1/4\" = 1'-0\"",
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, -3, offset));

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
