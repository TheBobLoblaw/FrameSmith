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
    /// Generates the front elevation view and coordinates focused drawing services.
    /// </summary>
    public static class FrontElevationGenerator
    {
        public static int Generate(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset, Editor ed, WarningCollector warnings)
        {
            int count = 0;
            var p = geo.Params;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-2, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth + 2, 0, offset),
                LayerManager.Layers.Slab);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                LayerManager.Layers.Posts);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth, p.EaveHeight, offset),
                LayerManager.Layers.Posts);
            count++;

            if (p.BuildingWidth > 24)
            {
                double centerPostHeight = p.TrussType == TrussType.MonoSlope
                    ? geo.PeakHeight - (geo.PeakHeight - p.EaveHeight) * 0.5
                    : geo.PeakHeight;

                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth / 2, 0, offset),
                    DrawingHelpers.Offset(p.BuildingWidth / 2, centerPostHeight, offset),
                    LayerManager.Layers.Posts);
                count++;
            }

            foreach (var girt in geo.Girts)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, girt.Elevation, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, girt.Elevation, offset),
                    LayerManager.Layers.Girts);
                count++;
            }

            foreach (double floorLevel in geo.FloorLevels)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, floorLevel, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, floorLevel, offset),
                    LayerManager.Layers.Floor);
                count++;
            }

            count += geo.TrussProfile.RenderFrontElevation(tr, btr, geo, offset);

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

            count += OpeningDrawingService.DrawFrontElevationOpenings(tr, btr, geo, offset, ed, warnings);

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

            foreach (var ltGeo in geo.LeanToGeometries)
            {
                try
                {
                    count += LeanToGenerator.GenerateFrontElevation(tr, btr, ltGeo, geo, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Lean-to front elevation generation failed", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Lean-to front elevation generation unexpected failure", ex);
                }
            }

            foreach (var porchGeo in geo.PorchGeometries)
            {
                try
                {
                    count += PorchGenerator.GenerateFrontElevation(tr, btr, porchGeo, geo, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Porch front elevation generation failed", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, "Porch front elevation generation unexpected failure", ex);
                }
            }

            count += ExteriorDetailDrawingService.AddFrontElevationExteriorDetails(tr, btr, geo, offset, ed, warnings);

            if (p.AddDimensions)
            {
                count += DimensionScaffoldingService.AddFrontDimensions(tr, btr, geo, offset);
            }

            count += ViewLabelGenerator.AddViewLabel(tr, btr,
                "FRONT ELEVATION", "1/4\" = 1'-0\"",
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, -3, offset));

            return count;
        }
    }
}
