using System.Collections.Generic;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;

namespace PoleBarnGenerator.Generators.Renderers
{
    /// <summary>
    /// Renders double-hung windows with two operable sashes.
    /// </summary>
    public class DoubleHungWindowRenderer : IWindowRenderer
    {
        private const double FrameWidth = 0.17; // feet

        public int RenderPlan(Transaction tr, BlockTableRecord btr,
                              WindowOpening window, WallGeometry wall, Vector3d offset)
        {
            int count = 0;
            double halfW = window.Width / 2.0;
            double left = window.CenterOffset - halfW;
            double right = window.CenterOffset + halfW;
            double depth = 0.35;

            var outerPts = new List<Point2d>
            {
                Pt(wall.ToPlan(left, depth / 2), offset),
                Pt(wall.ToPlan(left, -depth / 2), offset),
                Pt(wall.ToPlan(right, -depth / 2), offset),
                Pt(wall.ToPlan(right, depth / 2), offset),
            };
            DrawingHelpers.AddPolyline(tr, btr, outerPts, LayerManager.Layers.Windows, closed: true);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                Off(wall.ToPlan(left + FrameWidth, 0), offset),
                Off(wall.ToPlan(right - FrameWidth, 0), offset),
                LayerManager.Layers.Windows);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                Off(wall.ToPlan(window.CenterOffset, depth / 2 - 0.02), offset),
                Off(wall.ToPlan(window.CenterOffset, -depth / 2 + 0.02), offset),
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

            DrawingHelpers.AddRectangle(tr, btr,
                P2(left, bottom, offset),
                window.Width, window.Height,
                LayerManager.Layers.Windows);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                P3(left, bottom, offset),
                P3(right, bottom, offset),
                LayerManager.Layers.Windows);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                P3(left + FrameWidth, midY, offset),
                P3(right - FrameWidth, midY, offset),
                LayerManager.Layers.Windows);
            count++;

            DrawingHelpers.AddRectangle(tr, btr,
                P2(left + FrameWidth, midY + FrameWidth * 0.5, offset),
                window.Width - (2 * FrameWidth), window.Height / 2.0 - (1.5 * FrameWidth),
                LayerManager.Layers.Windows);
            count++;

            DrawingHelpers.AddRectangle(tr, btr,
                P2(left + FrameWidth, bottom + FrameWidth, offset),
                window.Width - (2 * FrameWidth), window.Height / 2.0 - (1.5 * FrameWidth),
                LayerManager.Layers.Windows);
            count++;

            if (window.HasGrid && window.GridPattern != GridPattern.None)
            {
                double cx = (left + right) / 2.0;
                DrawingHelpers.AddLine(tr, btr,
                    P3(cx, bottom + FrameWidth, offset),
                    P3(cx, top - FrameWidth, offset),
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
