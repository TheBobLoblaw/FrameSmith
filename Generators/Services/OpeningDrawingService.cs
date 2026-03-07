using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Generators.Renderers;
using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.Generators.Services
{
    /// <summary>
    /// Draws door and window openings for plan and elevation views.
    /// </summary>
    public static class OpeningDrawingService
    {
        public static int DrawPlanOpenings(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
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
                catch (Exception)
                {
                    // Skip failed opening render.
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
                catch (Exception)
                {
                    // Skip failed opening render.
                }
            }

            return count;
        }

        public static int DrawFrontElevationOpenings(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;

            foreach (var door in p.Doors)
            {
                if (door.Wall != WallSide.Front && door.Wall != WallSide.Back)
                {
                    continue;
                }

                try
                {
                    var renderer = RendererFactory.GetDoorRenderer(door.Type);
                    count += renderer.RenderElevation(tr, btr, door, p.EaveHeight, offset);
                }
                catch (Exception)
                {
                    // Skip failed opening render.
                }
            }

            foreach (var window in p.Windows)
            {
                if (window.Wall != WallSide.Front && window.Wall != WallSide.Back)
                {
                    continue;
                }

                try
                {
                    var renderer = RendererFactory.GetWindowRenderer(window.Type);
                    count += renderer.RenderElevation(tr, btr, window, p.EaveHeight, offset);
                }
                catch (Exception)
                {
                    // Skip failed opening render.
                }
            }

            return count;
        }

        public static int DrawSideElevationOpenings(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;

            foreach (var door in p.Doors)
            {
                if (door.Wall != WallSide.Left && door.Wall != WallSide.Right)
                {
                    continue;
                }

                try
                {
                    var renderer = RendererFactory.GetDoorRenderer(door.Type);
                    count += renderer.RenderElevation(tr, btr, door, p.EaveHeight, offset);
                }
                catch (Exception)
                {
                    // Skip failed opening render.
                }
            }

            foreach (var window in p.Windows)
            {
                if (window.Wall != WallSide.Left && window.Wall != WallSide.Right)
                {
                    continue;
                }

                try
                {
                    var renderer = RendererFactory.GetWindowRenderer(window.Type);
                    count += renderer.RenderElevation(tr, btr, window, p.EaveHeight, offset);
                }
                catch (Exception)
                {
                    // Skip failed opening render.
                }
            }

            return count;
        }
    }
}
