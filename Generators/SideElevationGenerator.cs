using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Generators.Renderers;
using PoleBarnGenerator.Utils;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates the side elevation view (looking at a sidewall).
    /// Shows: posts at each bay, girts, roof slope, door/window openings,
    /// bay spacing dimensions.
    /// 
    /// Coordinate mapping: X = building length, Y = height
    /// </summary>
    public static class SideElevationGenerator
    {
        public static int Generate(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;

            // ── Ground line ──
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-2, 0, offset),
                DrawingHelpers.Offset(p.BuildingLength + 2, 0, offset),
                LayerManager.Layers.Slab);
            count++;

            // ── Posts at each bay ──
            foreach (double bayY in geo.BayPositions)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(bayY, 0, offset),
                    DrawingHelpers.Offset(bayY, p.EaveHeight, offset),
                    LayerManager.Layers.Posts);
                count++;
            }

            // ── Girts ──
            foreach (var girt in geo.Girts)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, girt.Elevation, offset),
                    DrawingHelpers.Offset(p.BuildingLength, girt.Elevation, offset),
                    LayerManager.Layers.Girts);
                count++;
            }

            // ── Roof line (flat in side view — eave to eave with gable overhang) ──
            // Left gable overhang
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Eave line across full length
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Right gable overhang
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingLength, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Roof slope line (showing the roof pitch from the side)
            // In side elevation, you see the roof as a sloped line from eave up to ridge height
            double roofTopY = p.EaveHeight + p.OverhangEave * (p.RoofPitchRise / 12.0);
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, roofTopY, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, roofTopY, offset),
                LayerManager.Layers.Roof);
            count++;

            // Fascia at gable ends
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(-p.OverhangGable, roofTopY, offset),
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, roofTopY, offset),
                LayerManager.Layers.Roof);
            count++;

            // ── Door openings on sidewalls ──
            // Shows both Left and Right wall openings in the side elevation
            foreach (var door in p.Doors)
            {
                if (door.Wall == WallSide.Left || door.Wall == WallSide.Right)
                {
                    try
                    {
                        var renderer = RendererFactory.GetDoorRenderer(door.Type);
                        count += renderer.RenderElevation(tr, btr, door, p.EaveHeight, offset);
                    }
                    catch (System.Exception) { /* skip failed opening render */ }
                }
            }

            // ── Window openings on sidewalls ──
            foreach (var window in p.Windows)
            {
                if (window.Wall == WallSide.Left || window.Wall == WallSide.Right)
                {
                    try
                    {
                        var renderer = RendererFactory.GetWindowRenderer(window.Type);
                        count += renderer.RenderElevation(tr, btr, window, p.EaveHeight, offset);
                    }
                    catch (System.Exception) { /* skip failed opening render */ }
                }
            }


            // ── Lean-To profiles ──
            foreach (var ltGeo in geo.LeanToGeometries)
            {
                try
                {
                    // Endwall lean-tos shown as profile in side elevation
                    count += LeanToGenerator.GenerateSideElevation(tr, btr, ltGeo, geo, offset);
                    // Sidewall lean-tos shown as flat roof lines
                    count += LeanToGenerator.GenerateSideElevationSidewall(tr, btr, ltGeo, geo, offset);
                }
                catch (System.Exception) { /* skip failed lean-to render */ }
            }

            // ── Dimensions ──
            if (p.AddDimensions)
            {
                count += AddSideDimensions(tr, btr, geo, offset);
            }

            // ── View label ──
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(p.BuildingLength / 2.0, -3, offset),
                "SIDE ELEVATION", 1.5, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        private static int AddSideDimensions(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double dimOff = 3.0;

            // Overall length
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(p.BuildingLength, 0, offset),
                DrawingHelpers.Offset(p.BuildingLength / 2, -dimOff, offset),
                LayerManager.Layers.Dims);
            count++;

            // Bay spacing
            for (int i = 0; i < geo.BayPositions.Count - 1; i++)
            {
                double x1 = geo.BayPositions[i];
                double x2 = geo.BayPositions[i + 1];
                DrawingHelpers.AddAlignedDim(tr, btr,
                    DrawingHelpers.Offset(x1, 0, offset),
                    DrawingHelpers.Offset(x2, 0, offset),
                    DrawingHelpers.Offset((x1 + x2) / 2, -dimOff * 2, offset),
                    LayerManager.Layers.Dims);
                count++;
            }

            // Eave height
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                DrawingHelpers.Offset(-dimOff, p.EaveHeight / 2, offset),
                LayerManager.Layers.Dims);
            count++;

            return count;
        }
    }
}
