using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Generators.Services;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates the side elevation view and coordinates focused drawing services.
    /// </summary>
    public static class SideElevationGenerator
    {
        public static int Generate(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset, Editor ed, WarningCollector warnings)
        {
            int count = 0;
            var p = geo.Params;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-2, 0, offset),
                DrawingHelpers.Offset(p.BuildingLength + 2, 0, offset),
                LayerManager.Layers.Slab);
            count++;

            foreach (double bayY in geo.BayPositions)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(bayY, 0, offset),
                    DrawingHelpers.Offset(bayY, p.EaveHeight, offset),
                    LayerManager.Layers.Posts);
                count++;
            }

            foreach (var girt in geo.Girts)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, girt.Elevation, offset),
                    DrawingHelpers.Offset(p.BuildingLength, girt.Elevation, offset),
                    LayerManager.Layers.Girts);
                count++;
            }

            foreach (double floorLevel in geo.FloorLevels)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, floorLevel, offset),
                    DrawingHelpers.Offset(p.BuildingLength, floorLevel, offset),
                    LayerManager.Layers.Floor);
                count++;
            }

            count += geo.TrussProfile.RenderSideElevation(tr, btr, geo, offset);

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

            count += OpeningDrawingService.DrawSideElevationOpenings(tr, btr, geo, offset, ed, warnings);

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

            foreach (var ltGeo in geo.LeanToGeometries)
            {
                try
                {
                    count += LeanToGenerator.GenerateSideElevation(tr, btr, ltGeo, geo, offset);
                    count += LeanToGenerator.GenerateSideElevationSidewall(tr, btr, ltGeo, geo, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Lean-to side elevation generation failed", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Lean-to side elevation generation unexpected failure", ex);
                }
            }

            foreach (var porchGeo in geo.PorchGeometries)
            {
                try
                {
                    count += PorchGenerator.GenerateSideElevation(tr, btr, porchGeo, geo, offset);
                    count += PorchGenerator.GenerateSideElevationSidewall(tr, btr, porchGeo, geo, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Porch side elevation generation failed", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Porch side elevation generation unexpected failure", ex);
                }
            }

            count += ExteriorDetailDrawingService.AddSideElevationExteriorDetails(tr, btr, geo, offset, ed, warnings);

            if (p.AddDimensions)
            {
                count += DimensionScaffoldingService.AddSideDimensions(tr, btr, geo, offset);
            }

            count += ViewLabelGenerator.AddViewLabel(tr, btr,
                "SIDE ELEVATION", "1/4\" = 1'-0\"",
                DrawingHelpers.Offset(p.BuildingLength / 2.0, -3, offset));

            return count;
        }
    }
}
