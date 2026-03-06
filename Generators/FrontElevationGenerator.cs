using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Generators.Renderers;
using PoleBarnGenerator.Generators.TrussProfiles;
using PoleBarnGenerator.Utils;
using System.Collections.Generic;
using System;

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

            // ── Intermediate floor lines (multi-story) ──
            foreach (double floorLevel in geo.FloorLevels)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, floorLevel, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, floorLevel, offset),
                    LayerManager.Layers.Floor);
                count++;
            }

            // ── Roof profile (strategy pattern) ──
            count += geo.TrussProfile.RenderFrontElevation(tr, btr, geo, offset);

            // ── Curved roof profile cue ──
            if (p.CurvedWall.Enabled)
            {
                double roofMid = geo.PeakHeight + Math.Max(0.5, p.CurvedWall.ArcAngleDegrees / 90.0);
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, p.EaveHeight, offset),
                    DrawingHelpers.Offset(p.BuildingWidth / 2.0, roofMid, offset),
                    LayerManager.Layers.Curved);
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth / 2.0, roofMid, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, p.EaveHeight, offset),
                    LayerManager.Layers.Curved);
                count += 2;
            }

            // ── Door openings on endwalls (front & back) ──
            foreach (var door in p.Doors)
            {
                if (door.Wall == WallSide.Front || door.Wall == WallSide.Back)
                {
                    try
                    {
                        var renderer = RendererFactory.GetDoorRenderer(door.Type);
                        count += renderer.RenderElevation(tr, btr, door, p.EaveHeight, offset);
                    }
                    catch (System.Exception) { /* skip failed opening render */ }
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
                        count += renderer.RenderElevation(tr, btr, window, p.EaveHeight, offset);
                    }
                    catch (System.Exception) { /* skip failed opening render */ }
                }
            }

            // ── Expansion joint detail callouts ──
            if (geo.ExpansionJoints.Count > 0)
            {
                double markerX = p.BuildingWidth + 1.0;
                for (int i = 0; i < geo.ExpansionJoints.Count; i++)
                {
                    double y = p.EaveHeight * (0.25 + 0.15 * i);
                    DrawingHelpers.AddLine(tr, btr,
                        DrawingHelpers.Offset(markerX - 0.3, y - 0.4, offset),
                        DrawingHelpers.Offset(markerX + 0.3, y + 0.4, offset),
                        LayerManager.Layers.Joint);
                    DrawingHelpers.AddLine(tr, btr,
                        DrawingHelpers.Offset(markerX - 0.3, y + 0.4, offset),
                        DrawingHelpers.Offset(markerX + 0.3, y - 0.4, offset),
                        LayerManager.Layers.Joint);
                    DrawingHelpers.AddText(tr, btr,
                        DrawingHelpers.Offset(markerX + 1.4, y, offset),
                        $"EJ {geo.ExpansionJoints[i].JointType} {geo.ExpansionJoints[i].GapWidth:F2}'",
                        0.45, LayerManager.Layers.JointDetail);
                    count += 3;
                }
            }

            // ── Floor connection detailing note ──
            if (p.NumberOfFloors > 1)
            {
                string detail = p.FloorConnection == FloorConnectionType.ContinuousPost
                    ? "POSTS CONTINUOUS THROUGH FLOORS"
                    : "POSTS SPLICED @ FLOOR LINES";
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth * 0.5, p.EaveHeight + 1.5, offset),
                    $"{detail} | FLOOR BEAM: {p.FloorBeamSize}",
                    0.5, LayerManager.Layers.Floor);
                count++;
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
                count += AddVentilationOpenings(tr, btr, geo, offset);
                count += AddEquipmentDetails(tr, btr, geo, offset);
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

        private static int AddVentilationOpenings(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            var v = p.Ventilation;
            if (v?.IsEnabled != true && p.DairyBarn?.IsEnabled != true)
            {
                return 0;
            }

            if (v?.RidgeVentEnabled == true)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth * 0.25, geo.PeakHeight - 0.2, offset),
                    DrawingHelpers.Offset(p.BuildingWidth * 0.75, geo.PeakHeight - 0.2, offset),
                    LayerManager.Layers.Vent);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth * 0.5, geo.PeakHeight + 0.4, offset),
                    "RIDGE VENT", 0.35, LayerManager.Layers.Vent);
                count += 2;
            }

            int louvers = Math.Max(2, v?.WallLouverCount ?? 2);
            double spacing = p.BuildingWidth / (louvers + 1);
            for (int i = 1; i <= louvers; i++)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(i * spacing - 0.6, p.EaveHeight * 0.55, offset),
                    1.2, 0.9, LayerManager.Layers.Vent);
                count++;
            }

            int cupolas = Math.Max(0, v?.CupolaCount ?? 0);
            if (cupolas > 0)
            {
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(1.0, p.EaveHeight + 1.2, offset),
                    $"CUPOLAS: {cupolas} @ {v.CupolaAirflowCfmEach:F0} CFM", 0.35, LayerManager.Layers.Vent);
                count++;
            }

            return count;
        }

        private static int AddEquipmentDetails(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            var equip = p.EquipmentStorage;
            if (equip?.IsEnabled != true)
            {
                return 0;
            }

            if (equip.CraneRail.IsEnabled)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, equip.CraneRail.RailHeight, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, equip.CraneRail.RailHeight, offset),
                    LayerManager.Layers.Crane);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth * 0.5, equip.CraneRail.RailHeight + 0.4, offset),
                    $"CRANE RAIL {equip.CraneRail.CapacityTons:F1}T", 0.35, LayerManager.Layers.Crane);
                count += 2;
            }

            if (equip.LargeDoor.IsEnabled &&
                (equip.LargeDoor.Wall == WallSide.Front || equip.LargeDoor.Wall == WallSide.Back))
            {
                double x0 = Math.Max(0, (p.BuildingWidth - equip.LargeDoor.Width) / 2.0);
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(x0, 0, offset),
                    equip.LargeDoor.Width, equip.LargeDoor.Height, LayerManager.Layers.Equip);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(x0 + equip.LargeDoor.Width / 2.0, equip.LargeDoor.Height + 0.5, offset),
                    $"{equip.LargeDoor.DoorType} DOOR {equip.LargeDoor.Width:F0}'", 0.35, LayerManager.Layers.Equip);
                count += 2;
            }

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
