using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates a 3D wireframe model of the pole barn.
    /// All entities use actual 3D coordinates (X = width, Y = length, Z = height).
    /// 
    /// Draws: posts, girts, truss outlines, purlins, ridge line.
    /// Placed at a Z-offset below the 2D views (or at true 3D coords for 3D viewing).
    /// </summary>
    public static class Wireframe3DGenerator
    {
        public static int Generate(Transaction tr, BlockTableRecord btr, BarnGeometry geo)
        {
            int count = 0;
            var p = geo.Params;
            string lyr = LayerManager.Layers.Wire3D;

            // ── Posts (vertical lines at each post location) ──
            foreach (var post in geo.Posts)
            {
                DrawingHelpers.AddLine3D(tr, btr,
                    post.X, post.Y, 0,
                    post.X, post.Y, post.Height,
                    lyr);
                count++;
            }

            // ── Girts (horizontal members connecting posts along each wall) ──
            foreach (var girt in geo.Girts)
            {
                double z = girt.Elevation;

                // Left sidewall girts (X=0, running along Y)
                DrawingHelpers.AddLine3D(tr, btr,
                    0, 0, z,
                    0, p.BuildingLength, z,
                    lyr);
                count++;

                // Right sidewall girts (X=Width, running along Y)
                DrawingHelpers.AddLine3D(tr, btr,
                    p.BuildingWidth, 0, z,
                    p.BuildingWidth, p.BuildingLength, z,
                    lyr);
                count++;

                // Front endwall girt (Y=0, running along X)
                DrawingHelpers.AddLine3D(tr, btr,
                    0, 0, z,
                    p.BuildingWidth, 0, z,
                    lyr);
                count++;

                // Back endwall girt (Y=Length, running along X)
                DrawingHelpers.AddLine3D(tr, btr,
                    0, p.BuildingLength, z,
                    p.BuildingWidth, p.BuildingLength, z,
                    lyr);
                count++;
            }

            // ── Trusses at each bay ──
            double halfW = p.BuildingWidth / 2.0;

            foreach (var truss in geo.Trusses)
            {
                double y = truss.BayPosition;

                // Bottom chord (eave to eave)
                DrawingHelpers.AddLine3D(tr, btr,
                    0, y, truss.BottomChordZ,
                    p.BuildingWidth, y, truss.BottomChordZ,
                    LayerManager.Layers.Trusses);
                count++;

                // Left top chord (left eave to peak)
                DrawingHelpers.AddLine3D(tr, btr,
                    0, y, p.EaveHeight,
                    halfW, y, geo.PeakHeight,
                    LayerManager.Layers.Trusses);
                count++;

                // Right top chord (peak to right eave)
                DrawingHelpers.AddLine3D(tr, btr,
                    halfW, y, geo.PeakHeight,
                    p.BuildingWidth, y, p.EaveHeight,
                    LayerManager.Layers.Trusses);
                count++;

                // King post (center vertical from bottom chord to peak)
                DrawingHelpers.AddLine3D(tr, btr,
                    halfW, y, truss.BottomChordZ,
                    halfW, y, geo.PeakHeight,
                    LayerManager.Layers.Trusses);
                count++;

                // Simple web members (diagonal bracing)
                // Left web: quarter point
                double qx = halfW / 2.0;
                double qz = p.EaveHeight + geo.RoofRise / 2.0;
                DrawingHelpers.AddLine3D(tr, btr,
                    qx, y, truss.BottomChordZ,
                    qx, y, qz,
                    LayerManager.Layers.Trusses);
                count++;

                // Right web: three-quarter point
                DrawingHelpers.AddLine3D(tr, btr,
                    p.BuildingWidth - qx, y, truss.BottomChordZ,
                    p.BuildingWidth - qx, y, qz,
                    LayerManager.Layers.Trusses);
                count++;
            }

            // ── Ridge line (connecting all truss peaks) ──
            if (geo.BayPositions.Count >= 2)
            {
                DrawingHelpers.AddLine3D(tr, btr,
                    halfW, 0, geo.PeakHeight,
                    halfW, p.BuildingLength, geo.PeakHeight,
                    LayerManager.Layers.Purlins);
                count++;
            }

            // ── Purlins (running along the length at each purlin location) ──
            foreach (var purlin in geo.Purlins)
            {
                DrawingHelpers.AddLine3D(tr, btr,
                    purlin.X, 0, purlin.Z,
                    purlin.X, p.BuildingLength, purlin.Z,
                    LayerManager.Layers.Purlins);
                count++;
            }

            // ── Eave lines (top plate running full length on each side) ──
            DrawingHelpers.AddLine3D(tr, btr,
                0, 0, p.EaveHeight,
                0, p.BuildingLength, p.EaveHeight,
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine3D(tr, btr,
                p.BuildingWidth, 0, p.EaveHeight,
                p.BuildingWidth, p.BuildingLength, p.EaveHeight,
                LayerManager.Layers.Roof);
            count++;


            // ── Lean-To 3D wireframes ──
            foreach (var ltGeo in geo.LeanToGeometries)
            {
                try
                {
                    count += LeanToGenerator.Generate3D(tr, btr, ltGeo);
                }
                catch (System.Exception) { /* skip failed lean-to render */ }
            }

            // ── Slab outline (ground level) ──
            DrawingHelpers.AddLine3D(tr, btr, 0, 0, 0, p.BuildingWidth, 0, 0, LayerManager.Layers.Slab);
            DrawingHelpers.AddLine3D(tr, btr, p.BuildingWidth, 0, 0, p.BuildingWidth, p.BuildingLength, 0, LayerManager.Layers.Slab);
            DrawingHelpers.AddLine3D(tr, btr, p.BuildingWidth, p.BuildingLength, 0, 0, p.BuildingLength, 0, LayerManager.Layers.Slab);
            DrawingHelpers.AddLine3D(tr, btr, 0, p.BuildingLength, 0, 0, 0, 0, LayerManager.Layers.Slab);
            count += 4;

            return count;
        }
    }
}
