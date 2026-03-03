using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates porch structures in plan, elevation, and 3D views.
    /// Similar to LeanToGenerator but with columns, railings, and residential details.
    /// </summary>
    public static class PorchGenerator
    {
        private const string PorchLayer = "PB-PORCHES";

        // ═══════════════════════════════════════════════
        // Plan View
        // ═══════════════════════════════════════════════

        public static int GeneratePlan(Transaction tr, BlockTableRecord btr,
            PorchGeometry porchGeo, Vector3d offset)
        {
            int count = 0;
            var porch = porchGeo.Porch;
            var c = porchGeo.Corners;

            // ── Porch footprint (slab/deck outline) ──
            var footprint = new List<Point2d>
            {
                DrawingHelpers.Offset2d(c.AttachStart.X, c.AttachStart.Y, offset),
                DrawingHelpers.Offset2d(c.AttachEnd.X, c.AttachEnd.Y, offset),
                DrawingHelpers.Offset2d(c.OuterEnd.X, c.OuterEnd.Y, offset),
                DrawingHelpers.Offset2d(c.OuterStart.X, c.OuterStart.Y, offset),
            };
            DrawingHelpers.AddPolyline(tr, btr, footprint, LayerManager.Layers.Slab, closed: true);
            count++;

            // ── Columns ──
            double colSize = porch.ColumnType == ColumnType.Round ? 0.5 : 0.5; // 6" columns
            foreach (var col in porchGeo.ColumnLocations)
            {
                if (porch.ColumnType == ColumnType.Round)
                {
                    // Draw circle for round columns
                    Circle circle = new Circle(
                        new Point3d(col.X + offset.X, col.Y + offset.Y, 0),
                        Vector3d.ZAxis, colSize / 2.0);
                    LayerManager.SetLayer(circle, PorchLayer);
                    btr.AppendEntity(circle);
                    tr.AddNewlyCreatedDBObject(circle, true);
                    count++;
                }
                else
                {
                    // Square/decorative columns as filled rectangles
                    DrawingHelpers.AddFilledRect(tr, btr,
                        DrawingHelpers.Offset2d(col.X, col.Y, offset),
                        colSize, colSize, PorchLayer);
                    count++;
                }
            }

            // ── Railing lines (dashed in plan) ──
            if (porch.HasRailing)
            {
                foreach (var seg in porchGeo.RailingSegments)
                {
                    Line rail = DrawingHelpers.AddLine(tr, btr,
                        DrawingHelpers.Offset(seg.X1, seg.Y1, offset),
                        DrawingHelpers.Offset(seg.X2, seg.Y2, offset),
                        PorchLayer);
                    try { rail.Linetype = "DASHED"; } catch { }
                    count++;
                }
            }

            // ── Porch roof outline (dashed) ──
            Polyline roofPl = DrawingHelpers.AddPolyline(tr, btr, footprint, LayerManager.Layers.Roof, closed: true);
            try { roofPl.Linetype = "DASHED"; } catch { }
            count++;

            // ── Label ──
            double cx = (c.AttachStart.X + c.OuterEnd.X) / 2.0;
            double cy = (c.AttachStart.Y + c.OuterEnd.Y) / 2.0;
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(cx, cy, offset),
                $"PORCH\\n{porch.Depth:F0}' x {porchGeo.EffectiveLength:F0}'",
                0.75, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        // ═══════════════════════════════════════════════
        // Front Elevation (shows porches on Left/Right sidewalls)
        // ═══════════════════════════════════════════════

        public static int GenerateFrontElevation(Transaction tr, BlockTableRecord btr,
            PorchGeometry porchGeo, BarnGeometry mainGeo, Vector3d offset)
        {
            int count = 0;
            var porch = porchGeo.Porch;

            if (porch.AttachmentWall != WallSide.Left && porch.AttachmentWall != WallSide.Right)
                return 0;

            double attachX, outerX;
            if (porch.AttachmentWall == WallSide.Left)
            {
                attachX = 0;
                outerX = -porch.Depth;
            }
            else
            {
                attachX = mainGeo.Params.BuildingWidth;
                outerX = mainGeo.Params.BuildingWidth + porch.Depth;
            }

            // Columns (vertical lines)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(outerX, 0, offset),
                DrawingHelpers.Offset(outerX, porchGeo.OuterEaveHeight, offset),
                PorchLayer);
            count++;

            // Roof slope
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(outerX, porchGeo.OuterEaveHeight, offset),
                DrawingHelpers.Offset(attachX, porchGeo.TieInHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Railing
            if (porch.HasRailing)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(outerX, porch.RailingHeight, offset),
                    DrawingHelpers.Offset(attachX, porch.RailingHeight, offset),
                    PorchLayer);
                count++;
            }

            // Dimension: porch depth
            double dimY = -3.0;
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(attachX, dimY, offset),
                DrawingHelpers.Offset(outerX, dimY, offset),
                DrawingHelpers.Offset((attachX + outerX) / 2, dimY, offset),
                LayerManager.Layers.Dims);
            count++;

            return count;
        }

        // ═══════════════════════════════════════════════
        // Side Elevation (shows porches on Front/Back endwalls)
        // ═══════════════════════════════════════════════

        public static int GenerateSideElevation(Transaction tr, BlockTableRecord btr,
            PorchGeometry porchGeo, BarnGeometry mainGeo, Vector3d offset)
        {
            int count = 0;
            var porch = porchGeo.Porch;

            if (porch.AttachmentWall != WallSide.Front && porch.AttachmentWall != WallSide.Back)
                return 0;

            double attachY, outerY;
            if (porch.AttachmentWall == WallSide.Front)
            {
                attachY = 0;
                outerY = -porch.Depth;
            }
            else
            {
                attachY = mainGeo.Params.BuildingLength;
                outerY = mainGeo.Params.BuildingLength + porch.Depth;
            }

            // Outer column
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(outerY, 0, offset),
                DrawingHelpers.Offset(outerY, porchGeo.OuterEaveHeight, offset),
                PorchLayer);
            count++;

            // Roof slope
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(outerY, porchGeo.OuterEaveHeight, offset),
                DrawingHelpers.Offset(attachY, porchGeo.TieInHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Railing
            if (porch.HasRailing)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(outerY, porch.RailingHeight, offset),
                    DrawingHelpers.Offset(attachY, porch.RailingHeight, offset),
                    PorchLayer);
                count++;
            }

            return count;
        }

        /// <summary>Shows sidewall porches as flat lines in side elevation</summary>
        public static int GenerateSideElevationSidewall(Transaction tr, BlockTableRecord btr,
            PorchGeometry porchGeo, BarnGeometry mainGeo, Vector3d offset)
        {
            int count = 0;
            var porch = porchGeo.Porch;

            if (porch.AttachmentWall != WallSide.Left && porch.AttachmentWall != WallSide.Right)
                return 0;

            double start = porch.GetEffectiveStart();
            double end = porch.GetEffectiveEnd(mainGeo.Params);

            // Columns at each column position along the length
            foreach (var col in porchGeo.ColumnLocations)
            {
                double colPos = (porch.AttachmentWall == WallSide.Left || porch.AttachmentWall == WallSide.Right)
                    ? col.Y : col.X;
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(colPos, 0, offset),
                    DrawingHelpers.Offset(colPos, porchGeo.OuterEaveHeight, offset),
                    PorchLayer);
                count++;
            }

            // Eave line
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(start, porchGeo.OuterEaveHeight, offset),
                DrawingHelpers.Offset(end, porchGeo.OuterEaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Railing
            if (porch.HasRailing)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(start, porch.RailingHeight, offset),
                    DrawingHelpers.Offset(end, porch.RailingHeight, offset),
                    PorchLayer);
                count++;
            }

            return count;
        }

        // ═══════════════════════════════════════════════
        // 3D Wireframe
        // ═══════════════════════════════════════════════

        public static int Generate3D(Transaction tr, BlockTableRecord btr,
            PorchGeometry porchGeo)
        {
            int count = 0;
            var porch = porchGeo.Porch;
            var c = porchGeo.Corners;

            // ── Columns (vertical lines) ──
            foreach (var col in porchGeo.ColumnLocations)
            {
                DrawingHelpers.AddLine3D(tr, btr,
                    col.X, col.Y, 0,
                    col.X, col.Y, porchGeo.OuterEaveHeight,
                    PorchLayer);
                count++;
            }

            // ── Outer eave line ──
            var eave = porchGeo.GetOuterEaveLine();
            DrawingHelpers.AddLine3D(tr, btr,
                eave.X1, eave.Y1, eave.Z1,
                eave.X2, eave.Y2, eave.Z2,
                LayerManager.Layers.Roof);
            count++;

            // ── Tie-in line ──
            var tieIn = porchGeo.GetTieInLine();
            DrawingHelpers.AddLine3D(tr, btr,
                tieIn.X1, tieIn.Y1, tieIn.Z1,
                tieIn.X2, tieIn.Y2, tieIn.Z2,
                LayerManager.Layers.Roof);
            count++;

            // ── Roof slope rafters at each column ──
            foreach (var col in porchGeo.ColumnLocations)
            {
                double tieX, tieY;
                switch (porch.AttachmentWall)
                {
                    case WallSide.Front: tieX = col.X; tieY = 0; break;
                    case WallSide.Back: tieX = col.X; tieY = porchGeo.MainParams.BuildingLength; break;
                    case WallSide.Left: tieX = 0; tieY = col.Y; break;
                    case WallSide.Right: tieX = porchGeo.MainParams.BuildingWidth; tieY = col.Y; break;
                    default: tieX = 0; tieY = 0; break;
                }

                DrawingHelpers.AddLine3D(tr, btr,
                    col.X, col.Y, porchGeo.OuterEaveHeight,
                    tieX, tieY, porchGeo.TieInHeight,
                    PorchLayer);
                count++;
            }

            // ── End rafters (slope lines at porch ends) ──
            // Start end
            DrawingHelpers.AddLine3D(tr, btr,
                c.OuterStart.X, c.OuterStart.Y, porchGeo.OuterEaveHeight,
                c.AttachStart.X, c.AttachStart.Y, porchGeo.TieInHeight,
                PorchLayer);
            count++;

            // End end
            DrawingHelpers.AddLine3D(tr, btr,
                c.OuterEnd.X, c.OuterEnd.Y, porchGeo.OuterEaveHeight,
                c.AttachEnd.X, c.AttachEnd.Y, porchGeo.TieInHeight,
                PorchLayer);
            count++;

            // ── Railings in 3D ──
            if (porch.HasRailing)
            {
                double rh = porch.RailingHeight;
                foreach (var seg in porchGeo.RailingSegments)
                {
                    DrawingHelpers.AddLine3D(tr, btr,
                        seg.X1, seg.Y1, rh,
                        seg.X2, seg.Y2, rh,
                        PorchLayer);
                    count++;

                    // Top rail
                    DrawingHelpers.AddLine3D(tr, btr,
                        seg.X1, seg.Y1, rh,
                        seg.X2, seg.Y2, rh,
                        PorchLayer);
                }

                // Railing posts (vertical lines at column locations on outer edge)
                foreach (var col in porchGeo.ColumnLocations)
                {
                    DrawingHelpers.AddLine3D(tr, btr,
                        col.X, col.Y, 0,
                        col.X, col.Y, rh,
                        PorchLayer);
                    count++;
                }
            }

            // ── Slab/deck outline ──
            DrawingHelpers.AddLine3D(tr, btr, c.AttachStart.X, c.AttachStart.Y, 0, c.AttachEnd.X, c.AttachEnd.Y, 0, LayerManager.Layers.Slab);
            DrawingHelpers.AddLine3D(tr, btr, c.AttachEnd.X, c.AttachEnd.Y, 0, c.OuterEnd.X, c.OuterEnd.Y, 0, LayerManager.Layers.Slab);
            DrawingHelpers.AddLine3D(tr, btr, c.OuterEnd.X, c.OuterEnd.Y, 0, c.OuterStart.X, c.OuterStart.Y, 0, LayerManager.Layers.Slab);
            DrawingHelpers.AddLine3D(tr, btr, c.OuterStart.X, c.OuterStart.Y, 0, c.AttachStart.X, c.AttachStart.Y, 0, LayerManager.Layers.Slab);
            count += 4;

            return count;
        }
    }
}
