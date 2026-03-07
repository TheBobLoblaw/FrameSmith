using System.Linq;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;

namespace PoleBarnGenerator.Generators.Services
{
    /// <summary>
    /// Draws reusable dimension scaffolding for the main orthographic views.
    /// </summary>
    public static class DimensionScaffoldingService
    {
        public static int AddPlanDimensions(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double dimOffset = 3.0;
            double minX = geo.FootprintOutline.Min(pt => pt.X);
            double maxX = geo.FootprintOutline.Max(pt => pt.X);
            double minY = geo.FootprintOutline.Min(pt => pt.Y);
            double maxY = geo.FootprintOutline.Max(pt => pt.Y);

            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(minX, minY, offset),
                DrawingHelpers.Offset(maxX, minY, offset),
                DrawingHelpers.Offset((minX + maxX) / 2, minY - dimOffset, offset),
                LayerManager.Layers.Dims);
            count++;

            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(minX, minY, offset),
                DrawingHelpers.Offset(minX, maxY, offset),
                DrawingHelpers.Offset(minX - dimOffset, (minY + maxY) / 2, offset),
                LayerManager.Layers.Dims);
            count++;

            for (int i = 0; i < geo.BayPositions.Count - 1; i++)
            {
                double y1 = geo.BayPositions[i];
                double y2 = geo.BayPositions[i + 1];

                DrawingHelpers.AddAlignedDim(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth, y1, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, y2, offset),
                    DrawingHelpers.Offset(p.BuildingWidth + dimOffset, (y1 + y2) / 2, offset),
                    LayerManager.Layers.Dims);
                count++;
            }

            return count;
        }

        public static int AddFrontDimensions(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double dimOff = 3.0;

            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth / 2, -dimOff, offset),
                LayerManager.Layers.Dims);
            count++;

            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                DrawingHelpers.Offset(-dimOff, p.EaveHeight / 2, offset),
                LayerManager.Layers.Dims);
            count++;

            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth / 2, 0, offset),
                DrawingHelpers.Offset(p.BuildingWidth / 2, geo.PeakHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth / 2 + dimOff, geo.PeakHeight / 2, offset),
                LayerManager.Layers.Dims);
            count++;

            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth * 0.25, p.EaveHeight + geo.RoofRise * 0.6, offset),
                $"{p.RoofPitchDisplay} PITCH",
                0.75, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        public static int AddSideDimensions(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double dimOff = 3.0;

            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(p.BuildingLength, 0, offset),
                DrawingHelpers.Offset(p.BuildingLength / 2, -dimOff, offset),
                LayerManager.Layers.Dims);
            count++;

            for (int i = 0; i < geo.BayPositions.Count - 1; i++)
            {
                double x1 = geo.BayPositions[i];
                double x2 = geo.BayPositions[i + 1];
                DrawingHelpers.AddAlignedDim(tr, btr,
                    DrawingHelpers.Offset(x1, 0, offset),
                    DrawingHelpers.Offset(x2, 0, offset),
                    DrawingHelpers.Offset((x1 + x2) / 2, -dimOff * 2, offset),
                    LayerManager.Layers.Dims);
                count++;
            }

            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, 0, offset),
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                DrawingHelpers.Offset(-dimOff, p.EaveHeight / 2, offset),
                LayerManager.Layers.Dims);
            count++;

            return count;
        }
    }
}
