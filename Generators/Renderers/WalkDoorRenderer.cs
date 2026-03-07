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
    /// Renders walk (man) doors and double (French) doors.
    /// Plan: 90° swing arc showing door leaf and direction.
    /// Elevation: Frame with panel, threshold, handle side indication.
    /// </summary>
    public class WalkDoorRenderer : IOpeningRenderer
    {
        private const double FrameWidth = 0.25;

        public int RenderPlan(Transaction tr, BlockTableRecord btr,
                              DoorOpening door, WallGeometry wall, Vector3d offset)
        {
            int count = 0;
            double halfW = door.Width / 2.0;
            double left = door.CenterOffset - halfW;
            double right = door.CenterOffset + halfW;

            bool isDouble = door.Type == DoorType.Double;
            double depth = wall.WallThickness;

            // Frame jambs (short lines perpendicular to wall at each side)
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

            if (isDouble)
            {
                // Two swing arcs from center
                double mid = door.CenterOffset;
                count += DrawSwingArc(tr, btr, wall, mid, left, door.SwingDirection, offset);
                count += DrawSwingArc(tr, btr, wall, mid, right, door.SwingDirection, offset);
            }
            else
            {
                // Single swing arc from hinge side
                double hingePos = door.HandingDirection == HandingDirection.Left ? left : right;
                double freePos = door.HandingDirection == HandingDirection.Left ? right : left;
                count += DrawSwingArc(tr, btr, wall, hingePos, freePos, door.SwingDirection, offset);
            }

            return count;
        }

        /// <summary>
        /// Draws a quarter-circle swing arc from hingePos toward freePos.
        /// The arc swings inward or outward based on SwingDirection.
        /// </summary>
        private int DrawSwingArc(Transaction tr, BlockTableRecord btr,
                                 WallGeometry wall, double hingeAlongWall, double freeAlongWall,
                                 SwingDirection swing, Vector3d offset)
        {
            int count = 0;
            double radius = Math.Abs(freeAlongWall - hingeAlongWall);
            if (radius < 0.1) return 0;

            // Hinge point on wall center line
            Point2d hingePt = wall.ToPlan(hingeAlongWall, 0);

            // Door leaf line (from hinge to arc endpoint perpendicular to wall)
            Vector2d perpDir = swing == SwingDirection.In ? wall.InwardDirection() : wall.OutwardDirection();
            Point2d leafEnd = new Point2d(hingePt.X + perpDir.X * radius,
                                          hingePt.Y + perpDir.Y * radius);

            var leafLine = DrawingHelpers.AddLine(tr, btr,
                Off(hingePt, offset), Off(leafEnd, offset),
                LayerManager.Layers.Doors);
            leafLine.LineWeight = LineWeight.LineWeight030;
            count++;

            // Quarter-circle arc from free-side wall position to leaf end
            Point2d freePt = wall.ToPlan(freeAlongWall, 0);
            Point3d center3d = new Point3d(hingePt.X + offset.X, hingePt.Y + offset.Y, 0);

            // Calculate start and end angles for the arc
            double startAngle = Math.Atan2(freePt.Y - hingePt.Y, freePt.X - hingePt.X);
            double endAngle = Math.Atan2(leafEnd.Y - hingePt.Y, leafEnd.X - hingePt.X);

            // Ensure we draw the shorter arc (quarter circle)
            Arc arc = new Arc(center3d, radius, startAngle, endAngle);
            LayerManager.SetLayer(arc, LayerManager.Layers.Doors);
            TrySetLinetype(arc, "DASHED", "Unable to set walk door swing arc linetype");
            arc.LineWeight = LineWeight.LineWeight013;
            btr.AppendEntity(arc);
            tr.AddNewlyCreatedDBObject(arc, true);
            count++;

            return count;
        }

        public int RenderElevation(Transaction tr, BlockTableRecord btr,
                                   DoorOpening door, double wallHeight, Vector3d offset)
        {
            int count = 0;
            double left = door.CenterOffset - door.Width / 2.0;
            double right = left + door.Width;
            bool isDouble = door.Type == DoorType.Double;

            // Frame outline
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

            // Threshold line
            DrawingHelpers.AddLine(tr, btr,
                P3(left, 0.1, offset), P3(right, 0.1, offset),
                LayerManager.Layers.Doors);
            count++;

            if (isDouble)
            {
                // Center meeting stile
                double mid = door.CenterOffset;
                DrawingHelpers.AddLine(tr, btr,
                    P3(mid, 0, offset), P3(mid, door.Height, offset),
                    LayerManager.Layers.Doors);
                count++;
            }

            // Handle indication (small circle on latch side)
            double handleX = door.HandingDirection == HandingDirection.Left
                ? right - 0.5 : left + 0.5;
            double handleY = door.Height * 0.45; // slightly below center
            // Simple dash for handle
            DrawingHelpers.AddLine(tr, btr,
                P3(handleX - 0.15, handleY, offset),
                P3(handleX + 0.15, handleY, offset),
                LayerManager.Layers.Doors);
            count++;

            // Header
            count += DrawHeader(tr, btr, door, offset);

            // Label
            string typeLabel = isDouble ? "DBL" : "WK";
            DrawingHelpers.AddText(tr, btr,
                P3(door.CenterOffset, door.Height / 2, offset),
                $"{typeLabel}\n{door.Width}' x {door.Height}'",
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
            double right = left + door.Width;

            DrawingHelpers.AddRectangle(tr, btr,
                P2(left, door.Height, offset),
                door.Width, hDepth,
                LayerManager.Layers.Headers);

            string desc = HeaderSizing.GetHeaderDescription(header);
            DrawingHelpers.AddText(tr, btr,
                P3(door.CenterOffset, door.Height + hDepth / 2, offset),
                desc, 0.3, LayerManager.Layers.Anno);

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
