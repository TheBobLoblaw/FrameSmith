using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace PoleBarnGenerator.Utils
{
    /// <summary>
    /// Generates standardized view labels for each drawing view.
    /// Format:
    ///   ─────────────── 
    ///   VIEW NAME
    ///   SCALE: 1/4" = 1'-0"
    /// </summary>
    public static class ViewLabelGenerator
    {
        private const double LineLength = 8.0;   // Label underline length in feet
        private const double TextGap = 0.3;       // Gap between elements

        /// <summary>
        /// Adds a professional view label below a drawing view.
        /// </summary>
        public static int AddViewLabel(Transaction tr, BlockTableRecord btr,
            string viewName, string scale, Point3d position)
        {
            int count = 0;
            double cx = position.X;
            double y = position.Y;

            // Top line
            double halfLine = LineLength / 2.0;
            DrawingHelpers.AddLine(tr, btr,
                new Point3d(cx - halfLine, y, 0),
                new Point3d(cx + halfLine, y, 0),
                LayerManager.Layers.TextTitle);
            count++;

            // View name (FS-TITLE style, larger text)
            y -= TextGap;
            DrawingHelpers.AddText(tr, btr,
                new Point3d(cx, y, 0),
                viewName, 0.75, LayerManager.Layers.TextTitle);
            count++;

            // Scale text (FS-STANDARD style, smaller)
            y -= TextGap * 2.5;
            DrawingHelpers.AddText(tr, btr,
                new Point3d(cx, y, 0),
                $"SCALE: {scale}", 0.375, LayerManager.Layers.Anno);
            count++;

            return count;
        }
    }
}
