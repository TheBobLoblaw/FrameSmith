using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Generators.Renderers;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;

namespace PoleBarnGenerator.Generators.Services
{
    /// <summary>
    /// Draws door and window openings for plan and elevation views.
    /// </summary>
    public static class OpeningDrawingService
    {
        public static int DrawPlanOpenings(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset, Editor ed, WarningCollector warnings)
        {
            int count = 0;
            var p = geo.Params;

            foreach (var door in p.Doors)
            {
                try
                {
                    var wallGeo = new WallGeometry(p, door.Wall);
                    var renderer = RendererFactory.GetDoorRenderer(door.Type);
                    count += renderer.RenderPlan(tr, btr, door, wallGeo, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Plan opening render failed for door {door.Type}", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Plan opening render unexpected failure for door {door.Type}", ex);
                }
            }

            foreach (var window in p.Windows)
            {
                try
                {
                    var wallGeo = new WallGeometry(p, window.Wall);
                    var renderer = RendererFactory.GetWindowRenderer(window.Type);
                    count += renderer.RenderPlan(tr, btr, window, wallGeo, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Plan opening render failed for window {window.Type}", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Plan opening render unexpected failure for window {window.Type}", ex);
                }
            }

            return count;
        }

        public static int DrawFrontElevationOpenings(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset, Editor ed, WarningCollector warnings)
        {
            int count = 0;
            var p = geo.Params;

            foreach (var door in p.Doors)
            {
                if (door.Wall != WallSide.Front)
                {
                    continue;
                }

                try
                {
                    var renderer = RendererFactory.GetDoorRenderer(door.Type);
                    count += renderer.RenderElevation(tr, btr, door, p.EaveHeight, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Front elevation opening render failed for door {door.Type}", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Front elevation opening render unexpected failure for door {door.Type}", ex);
                }
            }

            foreach (var window in p.Windows)
            {
                if (window.Wall != WallSide.Front)
                {
                    continue;
                }

                try
                {
                    var renderer = RendererFactory.GetWindowRenderer(window.Type);
                    count += renderer.RenderElevation(tr, btr, window, p.EaveHeight, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Front elevation opening render failed for window {window.Type}", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Front elevation opening render unexpected failure for window {window.Type}", ex);
                }
            }

            return count;
        }

        public static int DrawSideElevationOpenings(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset, Editor ed, WarningCollector warnings)
        {
            int count = 0;
            var p = geo.Params;

            foreach (var door in p.Doors)
            {
                if (door.Wall != WallSide.Left)
                {
                    continue;
                }

                try
                {
                    var renderer = RendererFactory.GetDoorRenderer(door.Type);
                    count += renderer.RenderElevation(tr, btr, door, p.EaveHeight, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Side elevation opening render failed for door {door.Type}", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Side elevation opening render unexpected failure for door {door.Type}", ex);
                }
            }

            foreach (var window in p.Windows)
            {
                if (window.Wall != WallSide.Left)
                {
                    continue;
                }

                try
                {
                    var renderer = RendererFactory.GetWindowRenderer(window.Type);
                    count += renderer.RenderElevation(tr, btr, window, p.EaveHeight, offset);
                }
                catch (Autodesk.AutoCAD.Runtime.Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Side elevation opening render failed for window {window.Type}", ex);
                }
                catch (Exception ex)
                {
                    WarningCollector.Report(ed, warnings, $"Side elevation opening render unexpected failure for window {window.Type}", ex);
                }
            }

            return count;
        }
    }
}
