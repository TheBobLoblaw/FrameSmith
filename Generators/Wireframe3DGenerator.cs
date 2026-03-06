using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Generators.TrussProfiles;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

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
                    post.X, post.Y, post.BaseElevation,
                    post.X, post.Y, post.TopElevation > 0 ? post.TopElevation : post.Height,
                    lyr);
                count++;
            }

            // ── Girts (horizontal members connecting posts along each wall) ──
            foreach (var girt in geo.Girts)
            {
                double z = girt.Elevation;
                foreach (var segment in geo.WallSegments)
                {
                    if (segment.IsArc)
                    {
                        int divisions = Math.Max(8, (int)Math.Ceiling(Math.Abs(segment.EndAngle - segment.StartAngle) * 12));
                        double sweep = segment.EndAngle - segment.StartAngle;
                        Point3d? last = null;
                        for (int i = 0; i <= divisions; i++)
                        {
                            double t = (double)i / divisions;
                            double angle = segment.StartAngle + sweep * t;
                            var point = new Point3d(
                                segment.ArcCenter.X + segment.ArcRadius * Math.Cos(angle),
                                segment.ArcCenter.Y + segment.ArcRadius * Math.Sin(angle),
                                z);
                            if (last.HasValue)
                            {
                                DrawingHelpers.AddLine(tr, btr, last.Value, point, lyr);
                                count++;
                            }
                            last = point;
                        }
                    }
                    else
                    {
                        DrawingHelpers.AddLine3D(tr, btr,
                            segment.Start.X, segment.Start.Y, z,
                            segment.End.X, segment.End.Y, z,
                            lyr);
                        count++;
                    }
                }
            }

            // ── Trusses at each bay (strategy pattern) ──
            foreach (var truss in geo.Trusses)
            {
                count += geo.TrussProfile.Render3DTruss(tr, btr, geo, truss);
            }

            // ── Ridge line (connecting all truss peaks) ──
            double halfW = p.BuildingWidth / 2.0;
            if (geo.BayPositions.Count >= 2)
            {
                // For mono-slope, ridge is at high wall (X=0)
                double ridgeX = p.TrussType == TrussType.MonoSlope ? 0 : halfW;
                DrawingHelpers.AddLine3D(tr, btr,
                    ridgeX, 0, geo.PeakHeight,
                    ridgeX, p.BuildingLength, geo.PeakHeight,
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

            // ── Floor framing rings ──
            foreach (var floor in geo.FloorFraming)
            {
                if (floor.IsArc)
                {
                    int divisions = Math.Max(8, (int)Math.Ceiling(Math.Abs(floor.EndAngle - floor.StartAngle) * 12));
                    double sweep = floor.EndAngle - floor.StartAngle;
                    Point3d? last = null;
                    for (int i = 0; i <= divisions; i++)
                    {
                        double t = (double)i / divisions;
                        double angle = floor.StartAngle + sweep * t;
                        var point = new Point3d(
                            floor.ArcCenter.X + floor.ArcRadius * Math.Cos(angle),
                            floor.ArcCenter.Y + floor.ArcRadius * Math.Sin(angle),
                            floor.Elevation);
                        if (last.HasValue)
                        {
                            DrawingHelpers.AddLine(tr, btr, last.Value, point, LayerManager.Layers.Floor);
                            count++;
                        }
                        last = point;
                    }
                }
                else
                {
                    DrawingHelpers.AddLine3D(tr, btr,
                        floor.Start.X, floor.Start.Y, floor.Elevation,
                        floor.End.X, floor.End.Y, floor.Elevation,
                        LayerManager.Layers.Floor);
                    count++;
                }
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

            // ── Porch 3D wireframes ──
            foreach (var porchGeo in geo.PorchGeometries)
            {
                try
                {
                    count += PorchGenerator.Generate3D(tr, btr, porchGeo);
                }
                catch (System.Exception) { /* skip failed porch render */ }
            }

            // ── Exterior details 3D ──
            try
            {
                count += ExteriorDetailGenerator.AddWainscot3D(tr, btr, geo, p.Wainscot);
                count += ExteriorDetailGenerator.AddCupola3D(tr, btr, geo, p.Cupola);
            }
            catch (System.Exception) { /* skip failed detail render */ }

            // ── Expansion joint separations ──
            foreach (var joint in geo.ExpansionJoints)
            {
                DrawingHelpers.AddLine3D(tr, btr,
                    0, joint.Location, 0,
                    0, joint.Location, p.EaveHeight,
                    LayerManager.Layers.Joint);
                DrawingHelpers.AddLine3D(tr, btr,
                    p.BuildingWidth, joint.Location, 0,
                    p.BuildingWidth, joint.Location, p.EaveHeight,
                    LayerManager.Layers.Joint);
                count += 2;
            }

            // ── Slab outline (ground level) ──
            foreach (var segment in geo.WallSegments)
            {
                if (segment.IsArc)
                {
                    int divisions = Math.Max(8, (int)Math.Ceiling(Math.Abs(segment.EndAngle - segment.StartAngle) * 12));
                    double sweep = segment.EndAngle - segment.StartAngle;
                    Point3d? last = null;
                    for (int i = 0; i <= divisions; i++)
                    {
                        double t = (double)i / divisions;
                        double angle = segment.StartAngle + sweep * t;
                        var point = new Point3d(
                            segment.ArcCenter.X + segment.ArcRadius * Math.Cos(angle),
                            segment.ArcCenter.Y + segment.ArcRadius * Math.Sin(angle),
                            0);
                        if (last.HasValue)
                        {
                            DrawingHelpers.AddLine(tr, btr, last.Value, point, LayerManager.Layers.Slab);
                            count++;
                        }
                        last = point;
                    }
                }
                else
                {
                    DrawingHelpers.AddLine3D(tr, btr,
                        segment.Start.X, segment.Start.Y, 0,
                        segment.End.X, segment.End.Y, 0,
                        LayerManager.Layers.Slab);
                    count++;
                }
            }

            return count;
        }
    }
}
