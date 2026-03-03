using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators.Renderers
{
    /// <summary>
    /// Renders single-hung windows (and serves as fallback for other window types).
    /// Plan: Double-line frame indication on wall.
    /// Elevation: Frame outline, meeting rail at mid-height, sill detail, optional grid.
    /// </summary>
    public class SingleHungWindowRenderer : IWindowRenderer
    {
        private const double FrameWidth = 0.17; // feet

        public int RenderPlan(Transaction tr, BlockTableRecord btr,
                              WindowOpening window, WallGeometry wall, Vector3d offset)
        {
            int count = 0;
            double halfW = window.Width / 2.0;
            double left = window.CenterOffset - halfW;
            double right = window.CenterOffset + halfW;
            double depth = 0.33; // thinner than doors

            // Outer frame rectangle
            var outerPts = new List<Point2d>
            {
                Pt(wall.ToPlan(left, depth / 2), offset),
                Pt(wall.ToPlan(left, -depth / 2), offset),
                Pt(wall.ToPlan(right, -depth / 2), offset),
                Pt(wall.ToPlan(right, depth / 2), offset),
            };
            DrawingHelpers.AddPolyline(tr, btr, outerPts, LayerManager.Layers.Windows, closed: true);
            count++;

            // Inner glass line (center of frame)
            DrawingHelpers.AddLine(tr, btr,
                Off(wall.ToPlan(left + FrameWidth, 0), offset),
                Off(wall.ToPlan(right - FrameWidth, 0), offset),
                LayerManager.Layers.Windows);
            count++;

            return count;
        }

        public int RenderElevation(Transaction tr, BlockTableRecord btr,
                                   WindowOpening window, double wallHeight, Vector3d offset)
        {
            int count = 0;
            double left = window.CenterOffset - window.Width / 2.0;
            double right = left + window.Width;
            double bottom = window.SillHeight;
            double top = bottom + window.Height;
            double midY = bottom + window.Height / 2.0;

            // Outer frame
            var frame = DrawingHelpers.AddRectangle(tr, btr,
                P2(left, bottom, offset), window.Width, window.Height,
                LayerManager.Layers.Windows);
            frame.LineWeight = LineWeight.LineWeight030;
            count++;

            // Sill (thick line slightly below frame, extending past sides)
            double sillExt = 0.15;
            var sill = DrawingHelpers.AddLine(tr, btr,
                P3(left - sillExt, bottom, offset),
                P3(right + sillExt, bottom, offset),
                LayerManager.Layers.Windows);
            sill.LineWeight = LineWeight.LineWeight030;
            count++;

            // Meeting rail (horizontal line at mid-height — where upper & lower sash meet)
            var meetingRail = DrawingHelpers.AddLine(tr, btr,
                P3(left + FrameWidth, midY, offset),
                P3(right - FrameWidth, midY, offset),
                LayerManager.Layers.Windows);
            meetingRail.LineWeight = LineWeight.LineWeight018;
            count++;

            // Inner sash frames (upper fixed, lower operable)
            // Upper sash inner rectangle
            DrawingHelpers.AddRectangle(tr, btr,
                P2(left + FrameWidth, midY, offset),
                window.Width - FrameWidth * 2, window.Height / 2 - FrameWidth,
                LayerManager.Layers.Windows);
            count++;

            // Lower sash inner rectangle
            DrawingHelpers.AddRectangle(tr, btr,
                P2(left + FrameWidth, bottom + FrameWidth, offset),
                window.Width - FrameWidth * 2, window.Height / 2 - FrameWidth,
                LayerManager.Layers.Windows);
            count++;

            // Grid pattern (if enabled)
            if (window.HasGrid && window.GridPattern != GridPattern.None)
            {
                count += DrawGrid(tr, btr, window, left, right, bottom, top, midY, offset);
            }

            return count;
        }

        private int DrawGrid(Transaction tr, BlockTableRecord btr,
                             WindowOpening window, double left, double right,
                             double bottom, double top, double midY, Vector3d offset)
        {
            int count = 0;
            double cx = (left + right) / 2.0;

            if (window.GridPattern == GridPattern.Colonial)
            {
                // Colonial: vertical center line in each sash + horizontal divisions
                // Upper sash vertical
                var uv = DrawingHelpers.AddLine(tr, btr,
                    P3(cx, midY, offset), P3(cx, top - FrameWidth, offset),
                    LayerManager.Layers.Windows);
                uv.LineWeight = LineWeight.LineWeight013;
                count++;

                // Lower sash vertical
                var lv = DrawingHelpers.AddLine(tr, btr,
                    P3(cx, bottom + FrameWidth, offset), P3(cx, midY, offset),
                    LayerManager.Layers.Windows);
                lv.LineWeight = LineWeight.LineWeight013;
                count++;
            }
            else if (window.GridPattern == GridPattern.Prairie)
            {
                // Prairie: border grid lines near edges of each sash
                double inset = window.Width * 0.25;
                // Left vertical
                DrawingHelpers.AddLine(tr, btr,
                    P3(left + inset, bottom + FrameWidth, offset),
                    P3(left + inset, top - FrameWidth, offset),
                    LayerManager.Layers.Windows);
                count++;
                // Right vertical
                DrawingHelpers.AddLine(tr, btr,
                    P3(right - inset, bottom + FrameWidth, offset),
                    P3(right - inset, top - FrameWidth, offset),
                    LayerManager.Layers.Windows);
                count++;
            }

            return count;
        }

        private static Point2d Pt(Point2d pt, Vector3d off)
            => new Point2d(pt.X + off.X, pt.Y + off.Y);
        private static Point3d Off(Point2d pt, Vector3d off)
            => new Point3d(pt.X + off.X, pt.Y + off.Y, 0);
        private static Point2d P2(double x, double y, Vector3d off)
            => new Point2d(x + off.X, y + off.Y);
        private static Point3d P3(double x, double y, Vector3d off)
            => new Point3d(x + off.X, y + off.Y, 0);
    }
}
