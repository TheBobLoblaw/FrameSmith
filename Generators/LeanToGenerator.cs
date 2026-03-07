using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates lean-to structures in all views: plan, front/side elevation, 3D wireframe.
    /// Each lean-to is drawn relative to its attachment point on the main building.
    /// </summary>
    public static class LeanToGenerator
    {
        // ═══════════════════════════════════════════════
        // Plan View
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Draws lean-to footprint, posts, and dashed roof outline in plan view.
        /// </summary>
        public static int GeneratePlan(Transaction tr, BlockTableRecord btr,
            LeanToGeometry ltGeo, Vector3d offset)
        {
            int count = 0;
            var lt = ltGeo.LeanTo;
            var c = ltGeo.Corners;

            // ── Footprint outline (slab) ──
            var footprint = new List<Point2d>
            {
                DrawingHelpers.Offset2d(c.AttachStart.X, c.AttachStart.Y, offset),
                DrawingHelpers.Offset2d(c.AttachEnd.X, c.AttachEnd.Y, offset),
                DrawingHelpers.Offset2d(c.OuterEnd.X, c.OuterEnd.Y, offset),
                DrawingHelpers.Offset2d(c.OuterStart.X, c.OuterStart.Y, offset),
            };
            DrawingHelpers.AddPolyline(tr, btr, footprint, LayerManager.Layers.Slab, closed: true);
            count++;

            // ── Outer wall line ──
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(c.OuterStart.X, c.OuterStart.Y, offset),
                DrawingHelpers.Offset(c.OuterEnd.X, c.OuterEnd.Y, offset),
                LayerManager.Layers.Girts);
            count++;

            // ── End walls ──
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(c.AttachStart.X, c.AttachStart.Y, offset),
                DrawingHelpers.Offset(c.OuterStart.X, c.OuterStart.Y, offset),
                LayerManager.Layers.Girts);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(c.AttachEnd.X, c.AttachEnd.Y, offset),
                DrawingHelpers.Offset(c.OuterEnd.X, c.OuterEnd.Y, offset),
                LayerManager.Layers.Girts);
            count++;

            // ── Posts (outer row) ──
            foreach (var post in ltGeo.Posts)
            {
                double hw = post.PostWidth / 2.0;
                double hd = post.PostDepth / 2.0;

                DrawingHelpers.AddFilledRect(tr, btr,
                    DrawingHelpers.Offset2d(post.X, post.Y, offset),
                    post.PostWidth, post.PostDepth,
                    LayerManager.Layers.Posts);
                count++;

                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(post.X - hw, post.Y - hd, offset),
                    post.PostWidth, post.PostDepth,
                    LayerManager.Layers.Posts);
                count++;
            }

            // ── Roof outline (dashed) ──
            // Lean-to roof shown as dashed rectangle in plan
            var roofOutline = new List<Point2d>
            {
                DrawingHelpers.Offset2d(c.AttachStart.X, c.AttachStart.Y, offset),
                DrawingHelpers.Offset2d(c.AttachEnd.X, c.AttachEnd.Y, offset),
                DrawingHelpers.Offset2d(c.OuterEnd.X, c.OuterEnd.Y, offset),
                DrawingHelpers.Offset2d(c.OuterStart.X, c.OuterStart.Y, offset),
            };
            Polyline roofPl = DrawingHelpers.AddPolyline(tr, btr, roofOutline, LayerManager.Layers.Roof, closed: true);
            TrySetLinetype(roofPl, "DASHED", "Unable to set lean-to roof linetype");
            count++;

            // ── Label ──
            double cx = (c.AttachStart.X + c.OuterEnd.X) / 2.0;
            double cy = (c.AttachStart.Y + c.OuterEnd.Y) / 2.0;
            string label = $"LEAN-TO\\n{lt.Width:F0}' x {ltGeo.EffectiveLength:F0}'";
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(cx, cy, offset),
                label, 0.75, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        // ═══════════════════════════════════════════════
        // Front Elevation (shows lean-tos on Left/Right sidewalls)
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Draws lean-to profile in front elevation view.
        /// Only visible if lean-to is on a sidewall (Left or Right).
        /// In front elevation: X = building width axis, Y = height.
        /// </summary>
        public static int GenerateFrontElevation(Transaction tr, BlockTableRecord btr,
            LeanToGeometry ltGeo, BarnGeometry mainGeo, Vector3d offset)
        {
            int count = 0;
            var lt = ltGeo.LeanTo;

            if (lt.AttachmentWall != WallSide.Left && lt.AttachmentWall != WallSide.Right)
                return 0;

            double attachX, outerX;
            if (lt.AttachmentWall == WallSide.Left)
            {
                attachX = 0;
                outerX = -lt.Width;
            }
            else
            {
                attachX = mainGeo.Params.BuildingWidth;
                outerX = mainGeo.Params.BuildingWidth + lt.Width;
            }

            // Ground line extension
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(Math.Min(attachX, outerX) - 2, 0, offset),
                DrawingHelpers.Offset(Math.Max(attachX, outerX) + 2, 0, offset),
                LayerManager.Layers.Slab);
            count++;

            // Outer post
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(outerX, 0, offset),
                DrawingHelpers.Offset(outerX, lt.EaveHeight, offset),
                LayerManager.Layers.Posts);
            count++;

            // Lean-to roof slope line (outer eave to tie-in)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(outerX, lt.EaveHeight, offset),
                DrawingHelpers.Offset(attachX, ltGeo.TieInHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Tie-in point marker (horizontal dash on main wall)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(attachX - 0.5, ltGeo.TieInHeight, offset),
                DrawingHelpers.Offset(attachX + 0.5, ltGeo.TieInHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Outer eave line (horizontal)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(outerX, lt.EaveHeight, offset),
                DrawingHelpers.Offset(outerX, lt.EaveHeight, offset), // fascia point
                LayerManager.Layers.Roof);

            // Girts on enclosed walls
            if (lt.Type == LeanToType.FullyEnclosed || lt.EnclosedWalls[0])
            {
                foreach (var girt in ltGeo.Girts)
                {
                    DrawingHelpers.AddLine(tr, btr,
                        DrawingHelpers.Offset(attachX, girt.Elevation, offset),
                        DrawingHelpers.Offset(outerX, girt.Elevation, offset),
                        LayerManager.Layers.Girts);
                    count++;
                }
            }

            // Dimension: lean-to width
            double dimY = -3.0;
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(attachX, dimY, offset),
                DrawingHelpers.Offset(outerX, dimY, offset),
                DrawingHelpers.Offset((attachX + outerX) / 2, dimY, offset),
                LayerManager.Layers.Dims);
            count++;

            // Dimension: lean-to eave height
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(outerX + (lt.AttachmentWall == WallSide.Left ? -3 : 3), 0, offset),
                DrawingHelpers.Offset(outerX + (lt.AttachmentWall == WallSide.Left ? -3 : 3), lt.EaveHeight, offset),
                DrawingHelpers.Offset(outerX + (lt.AttachmentWall == WallSide.Left ? -3 : 3), lt.EaveHeight / 2, offset),
                LayerManager.Layers.Dims);
            count++;

            return count;
        }

        // ═══════════════════════════════════════════════
        // Side Elevation (shows lean-tos on Front/Back endwalls)
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Draws lean-to profile in side elevation view.
        /// Only visible if lean-to is on an endwall (Front or Back).
        /// In side elevation: X = building length axis, Y = height.
        /// </summary>
        public static int GenerateSideElevation(Transaction tr, BlockTableRecord btr,
            LeanToGeometry ltGeo, BarnGeometry mainGeo, Vector3d offset)
        {
            int count = 0;
            var lt = ltGeo.LeanTo;

            if (lt.AttachmentWall != WallSide.Front && lt.AttachmentWall != WallSide.Back)
                return 0;

            double attachY, outerY;
            if (lt.AttachmentWall == WallSide.Front)
            {
                attachY = 0;
                outerY = -lt.Width;
            }
            else
            {
                attachY = mainGeo.Params.BuildingLength;
                outerY = mainGeo.Params.BuildingLength + lt.Width;
            }

            // Ground line extension
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(Math.Min(attachY, outerY) - 2, 0, offset),
                DrawingHelpers.Offset(Math.Max(attachY, outerY) + 2, 0, offset),
                LayerManager.Layers.Slab);
            count++;

            // Outer post
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(outerY, 0, offset),
                DrawingHelpers.Offset(outerY, lt.EaveHeight, offset),
                LayerManager.Layers.Posts);
            count++;

            // Roof slope
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(outerY, lt.EaveHeight, offset),
                DrawingHelpers.Offset(attachY, ltGeo.TieInHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Tie-in marker
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(attachY - 0.5, ltGeo.TieInHeight, offset),
                DrawingHelpers.Offset(attachY + 0.5, ltGeo.TieInHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Girts if enclosed
            if (lt.Type == LeanToType.FullyEnclosed || lt.EnclosedWalls[0])
            {
                foreach (var girt in ltGeo.Girts)
                {
                    DrawingHelpers.AddLine(tr, btr,
                        DrawingHelpers.Offset(attachY, girt.Elevation, offset),
                        DrawingHelpers.Offset(outerY, girt.Elevation, offset),
                        LayerManager.Layers.Girts);
                    count++;
                }
            }

            return count;
        }

        // ═══════════════════════════════════════════════
        // Side Elevation for sidewall lean-tos
        // ═══════════════════════════════════════════════

        /// <summary>
        /// In side elevation, lean-tos on sidewalls appear as a flat roof line
        /// running the length of the lean-to at the lean-to eave/tie-in heights.
        /// </summary>
        public static int GenerateSideElevationSidewall(Transaction tr, BlockTableRecord btr,
            LeanToGeometry ltGeo, BarnGeometry mainGeo, Vector3d offset)
        {
            int count = 0;
            var lt = ltGeo.LeanTo;

            if (lt.AttachmentWall != WallSide.Left && lt.AttachmentWall != WallSide.Right)
                return 0;

            double start = lt.GetEffectiveStart();
            double end = lt.GetEffectiveEnd(mainGeo.Params);

            // Posts at each bay position (in side elevation X = along building length)
            foreach (double bayPos in ltGeo.BayPositions)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(bayPos, 0, offset),
                    DrawingHelpers.Offset(bayPos, lt.EaveHeight, offset),
                    LayerManager.Layers.Posts);
                count++;
            }

            // Eave line running along lean-to length
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(start, lt.EaveHeight, offset),
                DrawingHelpers.Offset(end, lt.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Roof top line (tie-in height, shown as dashed behind main roof)
            Line tieInLine = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(start, ltGeo.TieInHeight, offset),
                DrawingHelpers.Offset(end, ltGeo.TieInHeight, offset),
                LayerManager.Layers.Roof);
            TrySetLinetype(tieInLine, "DASHED", "Unable to set lean-to tie-in linetype");
            count++;

            return count;
        }

        // ═══════════════════════════════════════════════
        // 3D Wireframe
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Generates full 3D wireframe for a lean-to: posts, girts, purlins,
        /// roof slope, and tie-in line.
        /// </summary>
        public static int Generate3D(Transaction tr, BlockTableRecord btr,
            LeanToGeometry ltGeo)
        {
            int count = 0;
            var lt = ltGeo.LeanTo;
            var c = ltGeo.Corners;
            string lyr = LayerManager.Layers.Wire3D;

            // ── Posts (vertical lines) ──
            foreach (var post in ltGeo.Posts)
            {
                DrawingHelpers.AddLine3D(tr, btr,
                    post.X, post.Y, 0,
                    post.X, post.Y, post.Height,
                    LayerManager.Layers.Posts);
                count++;
            }

            // ── Outer eave line (connecting outer post tops) ──
            var eave = ltGeo.GetOuterEaveLine();
            DrawingHelpers.AddLine3D(tr, btr,
                eave.X1, eave.Y1, eave.Z1,
                eave.X2, eave.Y2, eave.Z2,
                LayerManager.Layers.Roof);
            count++;

            // ── Tie-in line (where roof meets main building wall) ──
            var tieIn = ltGeo.GetTieInLine();
            DrawingHelpers.AddLine3D(tr, btr,
                tieIn.X1, tieIn.Y1, tieIn.Z1,
                tieIn.X2, tieIn.Y2, tieIn.Z2,
                LayerManager.Layers.Roof);
            count++;

            // ── Roof slope lines at each bay position (rafters) ──
            foreach (double bayPos in ltGeo.BayPositions)
            {
                var (outerX, outerY, outerZ, tieX, tieY, tieZ) = GetRoofSlopeAtBay(ltGeo, bayPos);
                DrawingHelpers.AddLine3D(tr, btr,
                    outerX, outerY, outerZ,
                    tieX, tieY, tieZ,
                    LayerManager.Layers.Trusses);
                count++;
            }

            // ── Girts on outer wall ──
            if (lt.Type == LeanToType.FullyEnclosed || lt.EnclosedWalls[0])
            {
                foreach (var girt in ltGeo.Girts)
                {
                    DrawingHelpers.AddLine3D(tr, btr,
                        c.OuterStart.X, c.OuterStart.Y, girt.Elevation,
                        c.OuterEnd.X, c.OuterEnd.Y, girt.Elevation,
                        LayerManager.Layers.Girts);
                    count++;
                }
            }

            // ── Girts on end walls (if enclosed) ──
            if (lt.Type == LeanToType.FullyEnclosed || lt.EnclosedWalls[1])
            {
                foreach (var girt in ltGeo.Girts)
                {
                    DrawingHelpers.AddLine3D(tr, btr,
                        c.AttachStart.X, c.AttachStart.Y, girt.Elevation,
                        c.OuterStart.X, c.OuterStart.Y, girt.Elevation,
                        LayerManager.Layers.Girts);
                    count++;
                }
            }
            if (lt.Type == LeanToType.FullyEnclosed || lt.EnclosedWalls[2])
            {
                foreach (var girt in ltGeo.Girts)
                {
                    DrawingHelpers.AddLine3D(tr, btr,
                        c.AttachEnd.X, c.AttachEnd.Y, girt.Elevation,
                        c.OuterEnd.X, c.OuterEnd.Y, girt.Elevation,
                        LayerManager.Layers.Girts);
                    count++;
                }
            }

            // ── Purlins (running along lean-to length at each purlin height) ──
            foreach (var purlin in ltGeo.Purlins)
            {
                var (x1, y1, x2, y2) = GetPurlinEndpoints(ltGeo, purlin);
                DrawingHelpers.AddLine3D(tr, btr,
                    x1, y1, purlin.Height,
                    x2, y2, purlin.Height,
                    LayerManager.Layers.Purlins);
                count++;
            }

            // ── Slab outline (ground level) ──
            DrawingHelpers.AddLine3D(tr, btr, c.AttachStart.X, c.AttachStart.Y, 0, c.AttachEnd.X, c.AttachEnd.Y, 0, LayerManager.Layers.Slab);
            DrawingHelpers.AddLine3D(tr, btr, c.AttachEnd.X, c.AttachEnd.Y, 0, c.OuterEnd.X, c.OuterEnd.Y, 0, LayerManager.Layers.Slab);
            DrawingHelpers.AddLine3D(tr, btr, c.OuterEnd.X, c.OuterEnd.Y, 0, c.OuterStart.X, c.OuterStart.Y, 0, LayerManager.Layers.Slab);
            DrawingHelpers.AddLine3D(tr, btr, c.OuterStart.X, c.OuterStart.Y, 0, c.AttachStart.X, c.AttachStart.Y, 0, LayerManager.Layers.Slab);
            count += 4;

            return count;
        }

        // ═══════════════════════════════════════════════
        // Helper methods
        // ═══════════════════════════════════════════════

        /// <summary>
        /// Gets the roof slope line endpoints at a specific bay position.
        /// Returns (outerX, outerY, outerZ, tieInX, tieInY, tieInZ).
        /// </summary>
        private static (double, double, double, double, double, double) GetRoofSlopeAtBay(
            LeanToGeometry ltGeo, double bayPos)
        {
            var lt = ltGeo.LeanTo;
            double w = lt.Width;
            double bw = ltGeo.MainParams.BuildingWidth;
            double bl = ltGeo.MainParams.BuildingLength;
            double eaveZ = lt.EaveHeight;
            double tieZ = ltGeo.TieInHeight;

            switch (lt.AttachmentWall)
            {
                case WallSide.Left:
                    return (-w, bayPos, eaveZ, 0, bayPos, tieZ);
                case WallSide.Right:
                    return (bw + w, bayPos, eaveZ, bw, bayPos, tieZ);
                case WallSide.Front:
                    return (bayPos, -w, eaveZ, bayPos, 0, tieZ);
                case WallSide.Back:
                    return (bayPos, bl + w, eaveZ, bayPos, bl, tieZ);
                default:
                    return (0, 0, eaveZ, 0, 0, tieZ);
            }
        }

        /// <summary>
        /// Gets purlin endpoints running along the lean-to length at a given distance from outer edge.
        /// </summary>
        private static (double X1, double Y1, double X2, double Y2) GetPurlinEndpoints(
            LeanToGeometry ltGeo, LeanToPurlin purlin)
        {
            var lt = ltGeo.LeanTo;
            var c = ltGeo.Corners;
            double frac = purlin.FractionFromEave;

            // Interpolate between outer edge and attachment wall
            double x1 = c.OuterStart.X + frac * (c.AttachStart.X - c.OuterStart.X);
            double y1 = c.OuterStart.Y + frac * (c.AttachStart.Y - c.OuterStart.Y);
            double x2 = c.OuterEnd.X + frac * (c.AttachEnd.X - c.OuterEnd.X);
            double y2 = c.OuterEnd.Y + frac * (c.AttachEnd.Y - c.OuterEnd.Y);

            return (x1, y1, x2, y2);
        }

        private static void TrySetLinetype(Entity entity, string linetype, string context)
        {
            try
            {
                entity.Linetype = linetype;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                WarningCollector.ReportCurrent(context, ex);
            }
            catch (InvalidOperationException ex)
            {
                WarningCollector.ReportCurrent(context, ex);
            }
            catch (ArgumentException ex)
            {
                WarningCollector.ReportCurrent(context, ex);
            }
        }
    }
}
