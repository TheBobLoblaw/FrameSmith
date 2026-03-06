using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

namespace PoleBarnGenerator.Utils
{
    /// <summary>
    /// Common drawing operations used across all generators.
    /// All methods require an active Transaction and BlockTableRecord (model space).
    /// </summary>
    public static class DrawingHelpers
    {
        /// <summary>
        /// Adds a Line entity to model space.
        /// </summary>
        public static Line AddLine(Transaction tr, BlockTableRecord btr,
            Point3d start, Point3d end, string layer)
        {
            Line line = new Line(start, end);
            LayerManager.SetLayer(line, layer);
            btr.AppendEntity(line);
            tr.AddNewlyCreatedDBObject(line, true);
            return line;
        }

        /// <summary>
        /// Adds a closed rectangular polyline (plan view post, slab outline, etc.)
        /// </summary>
        public static Polyline AddRectangle(Transaction tr, BlockTableRecord btr,
            Point2d corner, double width, double height, string layer)
        {
            Polyline pl = new Polyline(4);
            pl.AddVertexAt(0, corner, 0, 0, 0);
            pl.AddVertexAt(1, new Point2d(corner.X + width, corner.Y), 0, 0, 0);
            pl.AddVertexAt(2, new Point2d(corner.X + width, corner.Y + height), 0, 0, 0);
            pl.AddVertexAt(3, new Point2d(corner.X, corner.Y + height), 0, 0, 0);
            pl.Closed = true;

            LayerManager.SetLayer(pl, layer);
            btr.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
            return pl;
        }

        /// <summary>
        /// Adds a filled solid rectangle (for post cross-sections in plan view).
        /// </summary>
        public static Solid AddFilledRect(Transaction tr, BlockTableRecord btr,
            Point2d center, double width, double height, string layer)
        {
            double hw = width / 2.0;
            double hh = height / 2.0;

            // AutoCAD Solid uses a specific vertex order (bow-tie pattern)
            Solid solid = new Solid(
                new Point3d(center.X - hw, center.Y - hh, 0),
                new Point3d(center.X + hw, center.Y - hh, 0),
                new Point3d(center.X - hw, center.Y + hh, 0),
                new Point3d(center.X + hw, center.Y + hh, 0)
            );

            LayerManager.SetLayer(solid, layer);
            btr.AppendEntity(solid);
            tr.AddNewlyCreatedDBObject(solid, true);
            return solid;
        }

        /// <summary>
        /// Adds a polyline from a list of 2D points.
        /// </summary>
        public static Polyline AddPolyline(Transaction tr, BlockTableRecord btr,
            List<Point2d> points, string layer, bool closed = false)
        {
            Polyline pl = new Polyline(points.Count);
            for (int i = 0; i < points.Count; i++)
            {
                pl.AddVertexAt(i, points[i], 0, 0, 0);
            }
            pl.Closed = closed;

            LayerManager.SetLayer(pl, layer);
            btr.AppendEntity(pl);
            tr.AddNewlyCreatedDBObject(pl, true);
            return pl;
        }

        /// <summary>
        /// Adds an Arc entity from start/end angles (radians).
        /// </summary>
        public static Arc AddArc(Transaction tr, BlockTableRecord btr,
            Point2d center, double radius, double startAngle, double endAngle, string layer)
        {
            Arc arc = new Arc(new Point3d(center.X, center.Y, 0), radius, startAngle, endAngle);
            LayerManager.SetLayer(arc, layer);
            btr.AppendEntity(arc);
            tr.AddNewlyCreatedDBObject(arc, true);
            return arc;
        }

        /// <summary>
        /// Adds a single-line MText annotation.
        /// </summary>
        public static MText AddText(Transaction tr, BlockTableRecord btr,
            Point3d position, string content, double textHeight, string layer)
        {
            MText mtext = new MText();
            mtext.Location = position;
            mtext.Contents = content;
            mtext.TextHeight = textHeight;
            mtext.Attachment = AttachmentPoint.MiddleCenter;

            LayerManager.SetLayer(mtext, layer);
            btr.AppendEntity(mtext);
            tr.AddNewlyCreatedDBObject(mtext, true);
            return mtext;
        }

        /// <summary>
        /// Adds an aligned dimension between two points.
        /// </summary>
        public static AlignedDimension AddAlignedDim(Transaction tr, BlockTableRecord btr,
            Point3d pt1, Point3d pt2, Point3d dimLinePoint, string layer)
        {
            AlignedDimension dim = new AlignedDimension(pt1, pt2, dimLinePoint, "", ObjectId.Null);

            LayerManager.SetLayer(dim, layer);
            btr.AppendEntity(dim);
            tr.AddNewlyCreatedDBObject(dim, true);
            return dim;
        }

        /// <summary>
        /// Adds a 3D line between two Point3d positions.
        /// Used by the wireframe generator.
        /// </summary>
        public static Line AddLine3D(Transaction tr, BlockTableRecord btr,
            double x1, double y1, double z1, double x2, double y2, double z2, string layer)
        {
            return AddLine(tr, btr,
                new Point3d(x1, y1, z1),
                new Point3d(x2, y2, z2),
                layer);
        }

        /// <summary>
        /// Applies a 2D offset to a point (used to place views side by side).
        /// </summary>
        public static Point3d Offset(double x, double y, Vector3d viewOffset)
        {
            return new Point3d(x + viewOffset.X, y + viewOffset.Y, 0);
        }

        public static Point2d Offset2d(double x, double y, Vector3d viewOffset)
        {
            return new Point2d(x + viewOffset.X, y + viewOffset.Y);
        }
    }
}
