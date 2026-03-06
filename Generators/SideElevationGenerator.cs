using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Generators.Renderers;
using PoleBarnGenerator.Generators.TrussProfiles;
using PoleBarnGenerator.Utils;
using System;

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

            // ── Intermediate floor lines ──
            foreach (double floorLevel in geo.FloorLevels)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, floorLevel, offset),
                    DrawingHelpers.Offset(p.BuildingLength, floorLevel, offset),
                    LayerManager.Layers.Floor);
                count++;
            }

            // ── Roof line (strategy pattern) ──
            count += geo.TrussProfile.RenderSideElevation(tr, btr, geo, offset);

            // ── Curved roof profile cue ──
            if (p.CurvedWall.Enabled)
            {
                double crest = p.EaveHeight + Math.Max(0.5, p.CurvedWall.ArcAngleDegrees / 120.0);
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, p.EaveHeight, offset),
                    DrawingHelpers.Offset(p.BuildingLength / 2.0, crest, offset),
                    LayerManager.Layers.Curved);
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(p.BuildingLength / 2.0, crest, offset),
                    DrawingHelpers.Offset(p.BuildingLength, p.EaveHeight, offset),
                    LayerManager.Layers.Curved);
                count += 2;
            }

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

            // ── Expansion joints ──
            foreach (var joint in geo.ExpansionJoints)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(joint.Location, 0, offset),
                    DrawingHelpers.Offset(joint.Location, p.EaveHeight, offset),
                    LayerManager.Layers.Joint);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(joint.Location, p.EaveHeight + 0.75, offset),
                    $"EJ {joint.GapWidth:F2}'",
                    0.45, LayerManager.Layers.JointDetail);
                count += 2;
            }

            if (p.NumberOfFloors > 1)
            {
                string detail = p.FloorConnection == FloorConnectionType.ContinuousPost
                    ? "CONTINUOUS POSTS"
                    : "SPLICED POSTS @ FLOORS";
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(p.BuildingLength / 2.0, p.EaveHeight + 1.5, offset),
                    $"{detail} | BEAM {p.FloorBeamSize}",
                    0.45, LayerManager.Layers.Floor);
                count++;
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

            // ── Porch profiles ──
            foreach (var porchGeo in geo.PorchGeometries)
            {
                try
                {
                    count += PorchGenerator.GenerateSideElevation(tr, btr, porchGeo, geo, offset);
                    count += PorchGenerator.GenerateSideElevationSidewall(tr, btr, porchGeo, geo, offset);
                }
                catch (System.Exception) { /* skip failed porch render */ }
            }

            // ── Exterior details ──
            try
            {
                count += ExteriorDetailGenerator.AddWainscotSideElevation(tr, btr, geo, p.Wainscot, offset);
            }
            catch (System.Exception) { /* skip failed detail render */ }

            // ── Dimensions ──
            if (p.AddDimensions)
            {
                count += AddSideDimensions(tr, btr, geo, offset);
            }

            // ── View label ──
            count += ViewLabelGenerator.AddViewLabel(tr, btr,
                "SIDE ELEVATION", "1/4\" = 1'-0\"",
                DrawingHelpers.Offset(p.BuildingLength / 2.0, -3, offset));

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
