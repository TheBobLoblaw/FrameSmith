using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Models.StructuralCalculations;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators.Renderers
{
    /// <summary>
    /// Renders Dutch (split) doors.
    /// Plan: Dual swing arcs (upper and lower halves can swing independently).
    /// Elevation: Horizontal split line at SplitHeight, separate frame sections.
    /// </summary>
    public class DutchDoorRenderer : IOpeningRenderer
    {
        public int RenderPlan(Transaction tr, BlockTableRecord btr,
                              DoorOpening door, WallGeometry wall, Vector3d offset)
        {
            int count = 0;
            double halfW = door.Width / 2.0;
            double left = door.CenterOffset - halfW;
            double right = door.CenterOffset + halfW;
            double depth = wall.WallThickness;

            // Frame jambs
            DrawingHelpers.AddLine(tr, btr,
                Off(wall.ToPlan(left, depth / 2), offset),
                Off(wall.ToPlan(left, -depth / 2), offset),
                LayerManager.Layers.Doors);
            count++;
            DrawingHelpers.AddLine(tr, btr,
                Off(wall.ToPlan(right, depth / 2), offset),
                Off(wall.ToPlan(right, -depth / 2), offset),
                LayerManager.Layers.Doors);
            count++;

            // Two swing arcs (one solid for lower, one dashed for upper)
            // Both swing from the same hinge side
            double hingePos = door.HandingDirection == HandingDirection.Left ? left : right;
            double freePos = door.HandingDirection == HandingDirection.Left ? right : left;
            double radius = Math.Abs(freePos - hingePos);
            if (radius < 0.1) return count;

            Point2d hingePt = wall.ToPlan(hingePos, 0);
            Vector2d perpDir = door.SwingDirection == SwingDirection.In
                ? wall.InwardDirection() : wall.OutwardDirection();

            Point2d leafEnd = new Point2d(hingePt.X + perpDir.X * radius,
                                          hingePt.Y + perpDir.Y * radius);
            Point2d freePt = wall.ToPlan(freePos, 0);

            // Lower half arc (solid)
            double startAng = Math.Atan2(freePt.Y - hingePt.Y, freePt.X - hingePt.X);
            double endAng = Math.Atan2(leafEnd.Y - hingePt.Y, leafEnd.X - hingePt.X);
            Point3d center3d = new Point3d(hingePt.X + offset.X, hingePt.Y + offset.Y, 0);

            Arc arcLower = new Arc(center3d, radius, startAng, endAng);
            LayerManager.SetLayer(arcLower, LayerManager.Layers.Doors);
            TrySetLinetype(arcLower, "DASHED", "Unable to set dutch door lower arc linetype");
            btr.AppendEntity(arcLower);
            tr.AddNewlyCreatedDBObject(arcLower, true);
            count++;

            // Upper half arc (slightly smaller radius to distinguish)
            double upperRadius = radius * 0.85;
            Arc arcUpper = new Arc(center3d, upperRadius, startAng, endAng);
            LayerManager.SetLayer(arcUpper, LayerManager.Layers.Doors);
            TrySetLinetype(arcUpper, "DASHED", "Unable to set dutch door upper arc linetype");
            arcUpper.LineWeight = LineWeight.LineWeight013;
            btr.AppendEntity(arcUpper);
            tr.AddNewlyCreatedDBObject(arcUpper, true);
            count++;

            // Door leaf lines (two overlapping at different lengths)
            var leafLower = DrawingHelpers.AddLine(tr, btr,
                Off(hingePt, offset),
                new Point3d(hingePt.X + perpDir.X * radius + offset.X,
                            hingePt.Y + perpDir.Y * radius + offset.Y, 0),
                LayerManager.Layers.Doors);
            leafLower.LineWeight = LineWeight.LineWeight030;
            count++;

            var leafUpper = DrawingHelpers.AddLine(tr, btr,
                Off(hingePt, offset),
                new Point3d(hingePt.X + perpDir.X * upperRadius + offset.X,
                            hingePt.Y + perpDir.Y * upperRadius + offset.Y, 0),
                LayerManager.Layers.Doors);
            leafUpper.LineWeight = LineWeight.LineWeight018;
            count++;

            return count;
        }

        public int RenderElevation(Transaction tr, BlockTableRecord btr,
                                   DoorOpening door, double wallHeight, Vector3d offset)
        {
            int count = 0;
            double left = door.CenterOffset - door.Width / 2.0;
            double right = left + door.Width;

            // Full frame outline
            var framePts = new List<Point2d>
            {
                P2(left, 0, offset),
                P2(left, door.Height, offset),
                P2(right, door.Height, offset),
                P2(right, 0, offset),
            };
            var frame = DrawingHelpers.AddPolyline(tr, btr, framePts, LayerManager.Layers.Doors, closed: true);
            frame.LineWeight = LineWeight.LineWeight030;
            count++;

            // Horizontal split line at SplitHeight (thick, prominent)
            var splitLine = DrawingHelpers.AddLine(tr, btr,
                P3(left, door.SplitHeight, offset),
                P3(right, door.SplitHeight, offset),
                LayerManager.Layers.Doors);
            splitLine.LineWeight = LineWeight.LineWeight030;
            count++;

            // Handle on lower half (latch side)
            double handleX = door.HandingDirection == HandingDirection.Left
                ? right - 0.5 : left + 0.5;
            DrawingHelpers.AddLine(tr, btr,
                P3(handleX - 0.15, door.SplitHeight * 0.5, offset),
                P3(handleX + 0.15, door.SplitHeight * 0.5, offset),
                LayerManager.Layers.Doors);
            count++;

            // Handle on upper half
            double upperMid = door.SplitHeight + (door.Height - door.SplitHeight) / 2.0;
            DrawingHelpers.AddLine(tr, btr,
                P3(handleX - 0.15, upperMid, offset),
                P3(handleX + 0.15, upperMid, offset),
                LayerManager.Layers.Doors);
            count++;

            // Header
            count += DrawHeader(tr, btr, door, offset);

            // Label
            DrawingHelpers.AddText(tr, btr,
                P3(door.CenterOffset, door.Height / 2, offset),
                $"DT\n{door.Width}' x {door.Height}'",
                0.5, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        private int DrawHeader(Transaction tr, BlockTableRecord btr,
                               DoorOpening door, Vector3d offset)
        {
            var header = HeaderSizing.CalculateHeaderSize(door.Width, LoadType.Roof);
            double hDepth = header.ActualDepth / 12.0;
            double left = door.CenterOffset - door.Width / 2.0;

            DrawingHelpers.AddRectangle(tr, btr,
                P2(left, door.Height, offset), door.Width, hDepth,
                LayerManager.Layers.Headers);

            DrawingHelpers.AddText(tr, btr,
                P3(door.CenterOffset, door.Height + hDepth / 2, offset),
                HeaderSizing.GetHeaderDescription(header), 0.3, LayerManager.Layers.Anno);

            return 2;
        }

        private static Point3d Off(Point2d pt, Vector3d off)
            => new Point3d(pt.X + off.X, pt.Y + off.Y, 0);
        private static Point2d P2(double x, double y, Vector3d off)
            => new Point2d(x + off.X, y + off.Y);
        private static Point3d P3(double x, double y, Vector3d off)
            => new Point3d(x + off.X, y + off.Y, 0);

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
