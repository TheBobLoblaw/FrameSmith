using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates exterior polish details: wainscot, cupolas, and gutters.
    /// These features add visual richness targeting the residential market.
    /// </summary>
    public static class ExteriorDetailGenerator
    {
        private const string DetailLayer = "PB-DETAILS";

        // ═══════════════════════════════════════════════
        // Wainscot
        // ═══════════════════════════════════════════════

        /// <summary>Adds wainscot line to front elevation</summary>
        public static int AddWainscotFrontElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, WainscotParameters wainscot, Vector3d offset)
        {
            if (!wainscot.IsEnabled) return 0;
            int count = 0;
            var p = geo.Params;

            // Front wall wainscot (Walls[0])
            if (wainscot.Walls[0])
            {
                // Horizontal line at wainscot height
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, wainscot.Height, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, wainscot.Height, offset),
                    DetailLayer);
                count++;

                // Material indication - vertical lines for board & batten
                if (wainscot.Material == WainscotMaterial.BoardAndBatten)
                {
                    double spacing = 1.0; // 12" board spacing
                    for (double x = spacing; x < p.BuildingWidth; x += spacing)
                    {
                        Line vLine = DrawingHelpers.AddLine(tr, btr,
                            DrawingHelpers.Offset(x, 0, offset),
                            DrawingHelpers.Offset(x, wainscot.Height, offset),
                            DetailLayer);
                        // Thin lineweight for pattern
                        count++;
                    }
                }

                // Label
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth / 2, wainscot.Height / 2, offset),
                    GetMaterialLabel(wainscot.Material),
                    0.5, LayerManager.Layers.Anno);
                count++;
            }

            return count;
        }

        /// <summary>Adds wainscot line to side elevation</summary>
        public static int AddWainscotSideElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, WainscotParameters wainscot, Vector3d offset)
        {
            if (!wainscot.IsEnabled) return 0;
            int count = 0;
            var p = geo.Params;

            // Left/Right wall wainscot (Walls[2] or Walls[3])
            if (wainscot.Walls[2] || wainscot.Walls[3])
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, wainscot.Height, offset),
                    DrawingHelpers.Offset(p.BuildingLength, wainscot.Height, offset),
                    DetailLayer);
                count++;
            }

            return count;
        }

        /// <summary>Adds wainscot lines in 3D</summary>
        public static int AddWainscot3D(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, WainscotParameters wainscot)
        {
            if (!wainscot.IsEnabled) return 0;
            int count = 0;
            var p = geo.Params;
            double h = wainscot.Height;

            // Front wall
            if (wainscot.Walls[0])
            {
                DrawingHelpers.AddLine3D(tr, btr, 0, 0, h, p.BuildingWidth, 0, h, DetailLayer);
                count++;
            }
            // Back wall
            if (wainscot.Walls[1])
            {
                DrawingHelpers.AddLine3D(tr, btr, 0, p.BuildingLength, h, p.BuildingWidth, p.BuildingLength, h, DetailLayer);
                count++;
            }
            // Left wall
            if (wainscot.Walls[2])
            {
                DrawingHelpers.AddLine3D(tr, btr, 0, 0, h, 0, p.BuildingLength, h, DetailLayer);
                count++;
            }
            // Right wall
            if (wainscot.Walls[3])
            {
                DrawingHelpers.AddLine3D(tr, btr, p.BuildingWidth, 0, h, p.BuildingWidth, p.BuildingLength, h, DetailLayer);
                count++;
            }

            return count;
        }

        // ═══════════════════════════════════════════════
        // Cupolas
        // ═══════════════════════════════════════════════

        /// <summary>Adds cupola symbols in plan view (squares on ridge)</summary>
        public static int AddCupolaPlan(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, CupolaParameters cupola, Vector3d offset)
        {
            if (!cupola.IsEnabled) return 0;
            int count = 0;
            var p = geo.Params;

            double sizeFt = cupola.Size / 12.0; // Convert inches to feet
            double ridgeX = p.BuildingWidth / 2.0;
            double spacing = cupola.Spacing > 0 ? cupola.Spacing : p.BuildingLength / (cupola.Count + 1);

            for (int i = 0; i < cupola.Count; i++)
            {
                double posY = spacing * (i + 1);
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(ridgeX - sizeFt / 2, posY - sizeFt / 2, offset),
                    sizeFt, sizeFt, DetailLayer);
                count++;

                // X mark inside to indicate cupola
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(ridgeX - sizeFt / 2, posY - sizeFt / 2, offset),
                    DrawingHelpers.Offset(ridgeX + sizeFt / 2, posY + sizeFt / 2, offset),
                    DetailLayer);
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(ridgeX + sizeFt / 2, posY - sizeFt / 2, offset),
                    DrawingHelpers.Offset(ridgeX - sizeFt / 2, posY + sizeFt / 2, offset),
                    DetailLayer);
                count += 2;
            }

            return count;
        }

        /// <summary>Adds cupola profile in front elevation</summary>
        public static int AddCupolaFrontElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, CupolaParameters cupola, Vector3d offset)
        {
            if (!cupola.IsEnabled) return 0;
            int count = 0;
            var p = geo.Params;

            double sizeFt = cupola.Size / 12.0;
            double cupolaHeight = sizeFt * 1.5; // Proportional height
            double ridgeX = p.BuildingWidth / 2.0;
            double peakZ = geo.PeakHeight;

            // Draw cupola profile (centered on ridge)
            // Base
            double baseLeft = ridgeX - sizeFt / 2;
            double baseRight = ridgeX + sizeFt / 2;
            double baseZ = peakZ;
            double topZ = peakZ + cupolaHeight;
            double capZ = topZ + sizeFt * 0.3; // Small roof cap

            // Walls
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(baseLeft, baseZ, offset),
                DrawingHelpers.Offset(baseLeft, topZ, offset),
                DetailLayer);
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(baseRight, baseZ, offset),
                DrawingHelpers.Offset(baseRight, topZ, offset),
                DetailLayer);
            count += 2;

            // Top plate
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(baseLeft, topZ, offset),
                DrawingHelpers.Offset(baseRight, topZ, offset),
                DetailLayer);
            count++;

            // Cupola roof (small gable)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(baseLeft - sizeFt * 0.1, topZ, offset),
                DrawingHelpers.Offset(ridgeX, capZ, offset),
                DetailLayer);
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(baseRight + sizeFt * 0.1, topZ, offset),
                DrawingHelpers.Offset(ridgeX, capZ, offset),
                DetailLayer);
            count += 2;

            // Louvers/vents (horizontal lines if vented)
            if (cupola.IsVented)
            {
                double louverSpacing = cupolaHeight / 5;
                for (int i = 1; i < 5; i++)
                {
                    double lz = baseZ + i * louverSpacing;
                    DrawingHelpers.AddLine(tr, btr,
                        DrawingHelpers.Offset(baseLeft + 0.05, lz, offset),
                        DrawingHelpers.Offset(baseRight - 0.05, lz, offset),
                        DetailLayer);
                    count++;
                }
            }

            return count;
        }

        /// <summary>Adds cupola structures in 3D</summary>
        public static int AddCupola3D(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, CupolaParameters cupola)
        {
            if (!cupola.IsEnabled) return 0;
            int count = 0;
            var p = geo.Params;

            double sizeFt = cupola.Size / 12.0;
            double cupolaHeight = sizeFt * 1.5;
            double ridgeX = p.BuildingWidth / 2.0;
            double peakZ = geo.PeakHeight;
            double spacing = cupola.Spacing > 0 ? cupola.Spacing : p.BuildingLength / (cupola.Count + 1);

            for (int i = 0; i < cupola.Count; i++)
            {
                double posY = spacing * (i + 1);
                double hs = sizeFt / 2;
                double topZ = peakZ + cupolaHeight;

                // 4 vertical edges
                DrawingHelpers.AddLine3D(tr, btr, ridgeX - hs, posY - hs, peakZ, ridgeX - hs, posY - hs, topZ, DetailLayer);
                DrawingHelpers.AddLine3D(tr, btr, ridgeX + hs, posY - hs, peakZ, ridgeX + hs, posY - hs, topZ, DetailLayer);
                DrawingHelpers.AddLine3D(tr, btr, ridgeX + hs, posY + hs, peakZ, ridgeX + hs, posY + hs, topZ, DetailLayer);
                DrawingHelpers.AddLine3D(tr, btr, ridgeX - hs, posY + hs, peakZ, ridgeX - hs, posY + hs, topZ, DetailLayer);
                count += 4;

                // 4 top edges
                DrawingHelpers.AddLine3D(tr, btr, ridgeX - hs, posY - hs, topZ, ridgeX + hs, posY - hs, topZ, DetailLayer);
                DrawingHelpers.AddLine3D(tr, btr, ridgeX + hs, posY - hs, topZ, ridgeX + hs, posY + hs, topZ, DetailLayer);
                DrawingHelpers.AddLine3D(tr, btr, ridgeX + hs, posY + hs, topZ, ridgeX - hs, posY + hs, topZ, DetailLayer);
                DrawingHelpers.AddLine3D(tr, btr, ridgeX - hs, posY + hs, topZ, ridgeX - hs, posY - hs, topZ, DetailLayer);
                count += 4;

                // Cupola roof peak
                double capZ = topZ + sizeFt * 0.3;
                DrawingHelpers.AddLine3D(tr, btr, ridgeX - hs, posY - hs, topZ, ridgeX, posY, capZ, DetailLayer);
                DrawingHelpers.AddLine3D(tr, btr, ridgeX + hs, posY - hs, topZ, ridgeX, posY, capZ, DetailLayer);
                DrawingHelpers.AddLine3D(tr, btr, ridgeX + hs, posY + hs, topZ, ridgeX, posY, capZ, DetailLayer);
                DrawingHelpers.AddLine3D(tr, btr, ridgeX - hs, posY + hs, topZ, ridgeX, posY, capZ, DetailLayer);
                count += 4;
            }

            return count;
        }

        // ═══════════════════════════════════════════════
        // Gutters
        // ═══════════════════════════════════════════════

        /// <summary>Adds gutter lines in plan view along eaves</summary>
        public static int AddGutterPlan(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, GutterParameters gutter, Vector3d offset)
        {
            if (!gutter.IsEnabled) return 0;
            int count = 0;
            var p = geo.Params;
            double gutterWidth = 0.5; // 6" gutter width in plan

            // Front eave gutter
            if (gutter.Eaves[0])
            {
                var pts = new List<Point2d>
                {
                    DrawingHelpers.Offset2d(-p.OverhangEave, -p.OverhangGable, offset),
                    DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave, -p.OverhangGable, offset),
                    DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave, -p.OverhangGable - gutterWidth, offset),
                    DrawingHelpers.Offset2d(-p.OverhangEave, -p.OverhangGable - gutterWidth, offset),
                };
                DrawingHelpers.AddPolyline(tr, btr, pts, DetailLayer, closed: true);
                count++;
            }

            // Back eave gutter
            if (gutter.Eaves[1])
            {
                var pts = new List<Point2d>
                {
                    DrawingHelpers.Offset2d(-p.OverhangEave, p.BuildingLength + p.OverhangGable, offset),
                    DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave, p.BuildingLength + p.OverhangGable, offset),
                    DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave, p.BuildingLength + p.OverhangGable + gutterWidth, offset),
                    DrawingHelpers.Offset2d(-p.OverhangEave, p.BuildingLength + p.OverhangGable + gutterWidth, offset),
                };
                DrawingHelpers.AddPolyline(tr, btr, pts, DetailLayer, closed: true);
                count++;
            }

            // Left eave gutter
            if (gutter.Eaves[2])
            {
                var pts = new List<Point2d>
                {
                    DrawingHelpers.Offset2d(-p.OverhangEave, -p.OverhangGable, offset),
                    DrawingHelpers.Offset2d(-p.OverhangEave, p.BuildingLength + p.OverhangGable, offset),
                    DrawingHelpers.Offset2d(-p.OverhangEave - gutterWidth, p.BuildingLength + p.OverhangGable, offset),
                    DrawingHelpers.Offset2d(-p.OverhangEave - gutterWidth, -p.OverhangGable, offset),
                };
                DrawingHelpers.AddPolyline(tr, btr, pts, DetailLayer, closed: true);
                count++;
            }

            // Right eave gutter
            if (gutter.Eaves[3])
            {
                var pts = new List<Point2d>
                {
                    DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave, -p.OverhangGable, offset),
                    DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave, p.BuildingLength + p.OverhangGable, offset),
                    DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave + gutterWidth, p.BuildingLength + p.OverhangGable, offset),
                    DrawingHelpers.Offset2d(p.BuildingWidth + p.OverhangEave + gutterWidth, -p.OverhangGable, offset),
                };
                DrawingHelpers.AddPolyline(tr, btr, pts, DetailLayer, closed: true);
                count++;
            }

            // Downspout markers
            foreach (var ds in gutter.Downspouts)
            {
                var dsPos = GetDownspoutPlanPosition(p, ds);
                DrawingHelpers.AddFilledRect(tr, btr,
                    DrawingHelpers.Offset2d(dsPos.X, dsPos.Y, offset),
                    0.25, 0.25, DetailLayer);
                count++;
            }

            return count;
        }

        /// <summary>Adds gutter profile in front elevation</summary>
        public static int AddGutterFrontElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, GutterParameters gutter, Vector3d offset)
        {
            if (!gutter.IsEnabled) return 0;
            int count = 0;
            var p = geo.Params;

            // Gutter profile at eave edges (small L-shaped cross section)
            double gutterDepth = 0.4; // ~5" gutter depth

            // Left eave gutter
            if (gutter.Eaves[2])
            {
                double x = -p.OverhangEave;
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(x, p.EaveHeight, offset),
                    DrawingHelpers.Offset(x, p.EaveHeight - gutterDepth, offset),
                    DetailLayer);
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(x, p.EaveHeight - gutterDepth, offset),
                    DrawingHelpers.Offset(x + 0.5, p.EaveHeight - gutterDepth, offset),
                    DetailLayer);
                count += 2;
            }

            // Right eave gutter
            if (gutter.Eaves[3])
            {
                double x = p.BuildingWidth + p.OverhangEave;
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(x, p.EaveHeight, offset),
                    DrawingHelpers.Offset(x, p.EaveHeight - gutterDepth, offset),
                    DetailLayer);
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(x, p.EaveHeight - gutterDepth, offset),
                    DrawingHelpers.Offset(x - 0.5, p.EaveHeight - gutterDepth, offset),
                    DetailLayer);
                count += 2;
            }

            return count;
        }

        // ── Helpers ──

        private static (double X, double Y) GetDownspoutPlanPosition(BarnParameters p, DownspoutLocation ds)
        {
            switch (ds.Wall)
            {
                case WallSide.Front: return (ds.Position, -p.OverhangGable);
                case WallSide.Back: return (ds.Position, p.BuildingLength + p.OverhangGable);
                case WallSide.Left: return (-p.OverhangEave, ds.Position);
                case WallSide.Right: return (p.BuildingWidth + p.OverhangEave, ds.Position);
                default: return (0, 0);
            }
        }

        private static string GetMaterialLabel(WainscotMaterial material)
        {
            switch (material)
            {
                case WainscotMaterial.BoardAndBatten: return "BOARD & BATTEN";
                case WainscotMaterial.Metal: return "METAL WAINSCOT";
                case WainscotMaterial.Vinyl: return "VINYL WAINSCOT";
                case WainscotMaterial.Wood: return "WOOD WAINSCOT";
                default: return "WAINSCOT";
            }
        }
    }
}
