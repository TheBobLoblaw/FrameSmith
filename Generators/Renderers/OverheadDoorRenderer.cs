using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Models.StructuralCalculations;
using PoleBarnGenerator.Utils;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators.Renderers
{
    /// <summary>
    /// Renders overhead (garage/sectional) doors.
    /// Plan: Frame outline with track indicators inside building.
    /// Elevation: Sectional panels, frame, structural header above.
    /// </summary>
    public class OverheadDoorRenderer : IOpeningRenderer
    {
        private const double FrameWidth = 0.25; // feet — visual frame thickness
        private const double TrackDepth = 1.5;  // feet — track extends into building

        public int RenderPlan(Transaction tr, BlockTableRecord btr,
                              DoorOpening door, WallGeometry wall, Vector3d offset)
        {
            int count = 0;
            double halfW = door.Width / 2.0;
            double left = door.CenterOffset - halfW;
            double right = door.CenterOffset + halfW;
            double depth = wall.WallThickness;

            // Door frame outline on wall
            var framePts = new List<Point2d>
            {
                Pt(wall.ToPlan(left, depth / 2), offset),
                Pt(wall.ToPlan(left, -depth / 2), offset),
                Pt(wall.ToPlan(right, -depth / 2), offset),
                Pt(wall.ToPlan(right, depth / 2), offset),
            };
            DrawingHelpers.AddPolyline(tr, btr, framePts, LayerManager.Layers.Doors, closed: true);
            count++;

            // Track lines extending inward (LEFT track)
            var trackL = DrawingHelpers.AddLine(tr, btr,
                Off(wall.ToPlan(left + FrameWidth, -depth / 2), offset),
                Off(wall.ToPlan(left + FrameWidth, -depth / 2 - TrackDepth), offset),
                LayerManager.Layers.Doors);
            SetLinetype(trackL, "CENTER");
            count++;

            // RIGHT track
            var trackR = DrawingHelpers.AddLine(tr, btr,
                Off(wall.ToPlan(right - FrameWidth, -depth / 2), offset),
                Off(wall.ToPlan(right - FrameWidth, -depth / 2 - TrackDepth), offset),
                LayerManager.Layers.Doors);
            SetLinetype(trackR, "CENTER");
            count++;

            // Track type indicator: additional dashes for HighLift/VerticalLift
            if (door.TrackType == TrackType.HighLift || door.TrackType == TrackType.VerticalLift)
            {
                double extraDepth = door.TrackType == TrackType.VerticalLift ? TrackDepth * 2 : TrackDepth * 1.5;
                var extL = DrawingHelpers.AddLine(tr, btr,
                    Off(wall.ToPlan(left + FrameWidth, -depth / 2 - TrackDepth), offset),
                    Off(wall.ToPlan(left + FrameWidth, -depth / 2 - extraDepth), offset),
                    LayerManager.Layers.Doors);
                SetLinetype(extL, "DASHED");
                count++;

                var extR = DrawingHelpers.AddLine(tr, btr,
                    Off(wall.ToPlan(right - FrameWidth, -depth / 2 - TrackDepth), offset),
                    Off(wall.ToPlan(right - FrameWidth, -depth / 2 - extraDepth), offset),
                    LayerManager.Layers.Doors);
                SetLinetype(extR, "DASHED");
                count++;
            }

            return count;
        }

        public int RenderElevation(Transaction tr, BlockTableRecord btr,
                                   DoorOpening door, double wallHeight, Vector3d offset)
        {
            int count = 0;
            double left = door.CenterOffset - door.Width / 2.0;
            double right = left + door.Width;

            // Frame outline (3-sided — no bottom for overhead)
            var framePts = new List<Point2d>
            {
                P2(left, 0, offset),
                P2(left, door.Height, offset),
                P2(right, door.Height, offset),
                P2(right, 0, offset),
            };
            var frame = DrawingHelpers.AddPolyline(tr, btr, framePts, LayerManager.Layers.Doors, closed: false);
            frame.LineWeight = LineWeight.LineWeight030;
            count++;

            // Sectional panels — horizontal lines dividing door into equal sections
            int panelCount = (int)(door.Height / 2.0); // ~2' panels
            if (panelCount < 2) panelCount = 2;
            double panelHeight = door.Height / panelCount;

            for (int i = 1; i < panelCount; i++)
            {
                double y = i * panelHeight;
                var panelLine = DrawingHelpers.AddLine(tr, btr,
                    P3(left + FrameWidth, y, offset),
                    P3(right - FrameWidth, y, offset),
                    LayerManager.Layers.Doors);
                panelLine.LineWeight = LineWeight.LineWeight018;
                count++;
            }

            // Structural header above opening
            count += DrawHeader(tr, btr, door, offset);

            // Label
            string trackLabel = door.TrackType == TrackType.StandardLift ? "" :
                               door.TrackType == TrackType.HighLift ? " HL" : " VL";
            DrawingHelpers.AddText(tr, btr,
                P3(door.CenterOffset, door.Height / 2, offset),
                $"OH{trackLabel}\n{door.Width}' x {door.Height}'",
                0.5, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        private int DrawHeader(Transaction tr, BlockTableRecord btr,
                               DoorOpening door, Vector3d offset)
        {
            var header = HeaderSizing.CalculateHeaderSize(door.Width, LoadType.Roof);
            double headerDepthFt = header.ActualDepth / 12.0;
            double left = door.CenterOffset - door.Width / 2.0;
            double right = left + door.Width;
            double bottom = door.Height;
            double top = bottom + headerDepthFt;

            // Header rectangle (thick lines)
            var pts = new List<Point2d>
            {
                P2(left, bottom, offset),
                P2(left, top, offset),
                P2(right, top, offset),
                P2(right, bottom, offset),
            };
            var hdr = DrawingHelpers.AddPolyline(tr, btr, pts, LayerManager.Layers.Headers, closed: true);
            hdr.LineWeight = LineWeight.LineWeight030;

            // Header label
            string desc = HeaderSizing.GetHeaderDescription(header);
            DrawingHelpers.AddText(tr, btr,
                P3(door.CenterOffset, bottom + headerDepthFt / 2, offset),
                desc, 0.3, LayerManager.Layers.Anno);

            return 2;
        }

        // ── Coordinate helpers ──
        private static Point2d Pt(Point2d wallPt, Vector3d off)
            => new Point2d(wallPt.X + off.X, wallPt.Y + off.Y);

        private static Point3d Off(Point2d wallPt, Vector3d off)
            => new Point3d(wallPt.X + off.X, wallPt.Y + off.Y, 0);

        private static Point2d P2(double x, double y, Vector3d off)
            => new Point2d(x + off.X, y + off.Y);

        private static Point3d P3(double x, double y, Vector3d off)
            => new Point3d(x + off.X, y + off.Y, 0);

        private static void SetLinetype(Entity ent, string lt)
        {
            try
            {
                ent.Linetype = lt;
            }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                WarningCollector.ReportCurrent($"Unable to set overhead door linetype '{lt}'", ex);
            }
            catch (InvalidOperationException ex)
            {
                WarningCollector.ReportCurrent($"Unable to set overhead door linetype '{lt}'", ex);
            }
            catch (ArgumentException ex)
            {
                WarningCollector.ReportCurrent($"Unable to set overhead door linetype '{lt}'", ex);
            }
        }
    }
}
