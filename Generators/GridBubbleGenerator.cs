using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates a professional structural grid system with lettered/numbered bubbles.
    /// Lettered grids (A, B, C...) along the length (bay lines).
    /// Numbered grids (1, 2, 3...) along the width.
    /// Grid lines extend 1' past building outline with bubbles at the ends.
    /// </summary>
    public static class GridBubbleGenerator
    {
        private const double GridExtension = 1.0;  // 1' past building
        private const double BubbleRadius = 0.375;  // 3/8" diameter at scale → 0.375' radius visual

        /// <summary>
        /// Generates grid system for plan view.
        /// </summary>
        public static int Generate(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;

            double bldgW = p.BuildingWidth;
            double bldgL = p.BuildingLength;

            // Numbered grids along width (1, 2) — left and right walls
            count += AddGridLine(tr, btr, offset, 0, -GridExtension, 0, bldgL + GridExtension,
                "1", true);
            count += AddGridLine(tr, btr, offset, bldgW, -GridExtension, bldgW, bldgL + GridExtension,
                "2", true);

            // Lettered grids along length (A, B, C...) — bay lines
            int bayCount = geo.NumBays;
            double baySpacing = geo.ActualBaySpacing;
            for (int i = 0; i <= bayCount; i++)
            {
                double y = i * baySpacing;
                string label = ((char)('A' + i)).ToString();
                count += AddGridLine(tr, btr, offset, -GridExtension, y, bldgW + GridExtension, y,
                    label, false);
            }

            return count;
        }

        private static int AddGridLine(Transaction tr, BlockTableRecord btr, Vector3d offset,
            double x1, double y1, double x2, double y2, string label, bool isVertical)
        {
            int count = 0;

            // Grid line on CENTER linetype layer
            DrawingHelpers.AddLine(tr, btr,
                new Point3d(x1 + offset.X, y1 + offset.Y, 0),
                new Point3d(x2 + offset.X, y2 + offset.Y, 0),
                LayerManager.Layers.Grid);
            count++;

            // Bubble at the start (bottom or left)
            double bx, by;
            if (isVertical)
            {
                bx = x1 + offset.X;
                by = y1 + offset.Y - BubbleRadius * 2;
            }
            else
            {
                bx = x1 + offset.X - BubbleRadius * 2;
                by = y1 + offset.Y;
            }

            // Draw bubble circle
            Circle bubble = new Circle(new Point3d(bx, by, 0), Vector3d.ZAxis, BubbleRadius);
            LayerManager.SetLayer(bubble, LayerManager.Layers.GridBubbles);
            btr.AppendEntity(bubble);
            tr.AddNewlyCreatedDBObject(bubble, true);
            count++;

            // Label text inside bubble
            DrawingHelpers.AddText(tr, btr,
                new Point3d(bx, by, 0),
                label, 0.25, LayerManager.Layers.GridText);
            count++;

            return count;
        }
    }
}
