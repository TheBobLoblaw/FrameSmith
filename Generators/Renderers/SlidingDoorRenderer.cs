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
    /// Renders sliding barn doors.
    /// Plan: Track line along wall with direction arrow, door panel indication.
    /// Elevation: Panel divisions, track hardware at top, structural header.
    /// </summary>
    public class SlidingDoorRenderer : IOpeningRenderer
    {
        private const double TrackExtension = 0.5; // extra track past opening (feet)

        public int RenderPlan(Transaction tr, BlockTableRecord btr,
                              DoorOpening door, WallGeometry wall, Vector3d offset)
        {
            int count = 0;
            double halfW = door.Width / 2.0;
            double left = door.CenterOffset - halfW;
            double right = door.CenterOffset + halfW;
            double depth = wall.WallThickness;

            // Opening frame (jambs only — no door leaf blocking opening in plan)
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

            // Track line extending to the right of the opening (slide direction)
            // Track runs along the outside of the wall
            double trackStart = left - TrackExtension;
            double trackEnd = right + door.Width + TrackExtension;
            // Clamp to wall length
            if (trackEnd > wall.WallLength) trackEnd = wall.WallLength - 0.25;
            if (trackStart < 0.25) trackStart = 0.25;

            var trackLine = DrawingHelpers.AddLine(tr, btr,
                Off(wall.ToPlan(trackStart, depth / 2 + 0.15), offset),
                Off(wall.ToPlan(trackEnd, depth / 2 + 0.15), offset),
                LayerManager.Layers.Doors);
            TrySetLinetype(trackLine, "CENTER", "Unable to set sliding door track linetype");
            count++;

            // Slide direction arrow (triangle pointing right from opening)
            double arrowX = right + door.Width * 0.5;
            double arrowY = depth / 2 + 0.15;
            double arrowSize = 0.4;
            var arrowPts = new List<Point2d>
            {
                Pt(wall.ToPlan(arrowX, arrowY), offset),
                Pt(wall.ToPlan(arrowX - arrowSize, arrowY + arrowSize * 0.4), offset),
                Pt(wall.ToPlan(arrowX - arrowSize, arrowY - arrowSize * 0.4), offset),
            };
            DrawingHelpers.AddPolyline(tr, btr, arrowPts, LayerManager.Layers.Doors, closed: true);
            count++;

            // Door panel shown in slid-open position (dashed rectangle beside opening)
            var panelPts = new List<Point2d>
            {
                Pt(wall.ToPlan(right + 0.1, depth / 2 + 0.05), offset),
                Pt(wall.ToPlan(right + 0.1, -depth / 2 - 0.05), offset),
                Pt(wall.ToPlan(right + door.Width - 0.1, -depth / 2 - 0.05), offset),
                Pt(wall.ToPlan(right + door.Width - 0.1, depth / 2 + 0.05), offset),
            };
            var panel = DrawingHelpers.AddPolyline(tr, btr, panelPts, LayerManager.Layers.Doors, closed: true);
            TrySetLinetype(panel, "DASHED", "Unable to set sliding door panel linetype");
            count++;

            return count;
        }

        public int RenderElevation(Transaction tr, BlockTableRecord btr,
                                   DoorOpening door, double wallHeight, Vector3d offset)
        {
            int count = 0;
            double left = door.CenterOffset - door.Width / 2.0;
            double right = left + door.Width;

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

            // Track line at top
            double trackY = door.Height - 0.15;
            var track = DrawingHelpers.AddLine(tr, btr,
                P3(left - TrackExtension, trackY, offset),
                P3(right + door.Width + TrackExtension, trackY, offset),
                LayerManager.Layers.Doors);
            TrySetLinetype(track, "CENTER", "Unable to set sliding door elevation track linetype");
            count++;

            // Vertical panel divisions (typically 2-4 panels)
            int panels = door.Width > 8 ? 4 : door.Width > 5 ? 3 : 2;
            double panelW = door.Width / panels;
            for (int i = 1; i < panels; i++)
            {
                var div = DrawingHelpers.AddLine(tr, btr,
                    P3(left + i * panelW, 0, offset),
                    P3(left + i * panelW, door.Height, offset),
                    LayerManager.Layers.Doors);
                div.LineWeight = LineWeight.LineWeight018;
                count++;
            }

            // Header
            count += DrawHeader(tr, btr, door, offset);

            // Label
            DrawingHelpers.AddText(tr, btr,
                P3(door.CenterOffset, door.Height / 2, offset),
                $"SL\n{door.Width}' x {door.Height}'",
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

        private static Point2d Pt(Point2d pt, Vector3d off)
            => new Point2d(pt.X + off.X, pt.Y + off.Y);
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
