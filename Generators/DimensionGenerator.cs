using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates dimension annotations for plan and elevation views.
    /// All dimensions are placed on the FS-S-DIM layer.
    /// </summary>
    public static class DimensionGenerator
    {
        private const string DimLayer = LayerManager.Layers.Dims;
        private const string AnnoLayer = LayerManager.Layers.Anno;
        private const double DimOffset = 3.0;   // feet offset from building edge to first dim line
        private const double DimSpacing = 2.0;  // feet between stacked dim lines

        // ─────────────────────────────────────────────
        // Plan View Dimensions
        // ─────────────────────────────────────────────

        /// <summary>
        /// Adds plan view dimensions: overall building size and bay spacing.
        /// </summary>
        public static int AddPlanDimensions(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;

            // ── Bay spacing dims along the right sidewall ──
            double bayDimX = p.BuildingWidth + DimOffset;
            for (int i = 0; i < geo.BayPositions.Count - 1; i++)
            {
                double y1 = geo.BayPositions[i];
                double y2 = geo.BayPositions[i + 1];

                DrawingHelpers.AddAlignedDim(tr, btr,
                    DrawingHelpers.Offset(bayDimX, y1, offset),
                    DrawingHelpers.Offset(bayDimX, y2, offset),
                    DrawingHelpers.Offset(bayDimX, (y1 + y2) / 2.0, offset),
                    DimLayer);
                count++;
            }

            // ── Overall length dim (further out from bay dims) ──
            double overallLenX = bayDimX + DimSpacing;
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(overallLenX, 0, offset),
                DrawingHelpers.Offset(overallLenX, p.BuildingLength, offset),
                DrawingHelpers.Offset(overallLenX, p.BuildingLength / 2.0, offset),
                DimLayer);
            count++;

            // ── Overall width dim along the front endwall ──
            double widthDimY = -DimOffset;
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, widthDimY, offset),
                DrawingHelpers.Offset(p.BuildingWidth, widthDimY, offset),
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, widthDimY, offset),
                DimLayer);
            count++;

            return count;
        }

        // ─────────────────────────────────────────────
        // Front Elevation Dimensions
        // ─────────────────────────────────────────────

        /// <summary>
        /// Adds front elevation dimensions: eave height, peak height, overall width,
        /// and roof pitch annotation.
        /// </summary>
        public static int AddFrontElevationDimensions(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;

            // ── Eave height (left side) ──
            double heightDimX = -DimOffset;
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(heightDimX, 0, offset),
                DrawingHelpers.Offset(heightDimX, p.EaveHeight, offset),
                DrawingHelpers.Offset(heightDimX, p.EaveHeight / 2.0, offset),
                DimLayer);
            count++;

            // ── Peak height (further left) ──
            double peakDimX = heightDimX - DimSpacing;
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(peakDimX, 0, offset),
                DrawingHelpers.Offset(peakDimX, geo.PeakHeight, offset),
                DrawingHelpers.Offset(peakDimX, geo.PeakHeight / 2.0, offset),
                DimLayer);
            count++;

            // ── Roof rise (right side, eave to peak) ──
            double riseDimX = p.BuildingWidth + DimOffset;
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(riseDimX, p.EaveHeight, offset),
                DrawingHelpers.Offset(riseDimX, geo.PeakHeight, offset),
                DrawingHelpers.Offset(riseDimX, (p.EaveHeight + geo.PeakHeight) / 2.0, offset),
                DimLayer);
            count++;

            // ── Overall width along ground ──
            double widthDimY = -DimOffset;
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, widthDimY, offset),
                DrawingHelpers.Offset(p.BuildingWidth, widthDimY, offset),
                DrawingHelpers.Offset(p.BuildingWidth / 2.0, widthDimY, offset),
                DimLayer);
            count++;

            // ── Roof pitch annotation ──
            count += AddRoofPitchAnnotation(tr, btr, geo, offset);

            return count;
        }

        // ─────────────────────────────────────────────
        // Side Elevation Dimensions
        // ─────────────────────────────────────────────

        /// <summary>
        /// Adds side elevation dimensions: eave height, bay spacing, overall length.
        /// </summary>
        public static int AddSideElevationDimensions(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;

            // ── Eave height (left side) ──
            double heightDimX = -DimOffset;
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(heightDimX, 0, offset),
                DrawingHelpers.Offset(heightDimX, p.EaveHeight, offset),
                DrawingHelpers.Offset(heightDimX, p.EaveHeight / 2.0, offset),
                DimLayer);
            count++;

            // ── Bay spacing dims along the bottom ──
            double bayDimY = -DimOffset;
            for (int i = 0; i < geo.BayPositions.Count - 1; i++)
            {
                double x1 = geo.BayPositions[i];
                double x2 = geo.BayPositions[i + 1];

                DrawingHelpers.AddAlignedDim(tr, btr,
                    DrawingHelpers.Offset(x1, bayDimY, offset),
                    DrawingHelpers.Offset(x2, bayDimY, offset),
                    DrawingHelpers.Offset((x1 + x2) / 2.0, bayDimY, offset),
                    DimLayer);
                count++;
            }

            // ── Overall length (further below bay dims) ──
            double overallDimY = bayDimY - DimSpacing;
            DrawingHelpers.AddAlignedDim(tr, btr,
                DrawingHelpers.Offset(0, overallDimY, offset),
                DrawingHelpers.Offset(p.BuildingLength, overallDimY, offset),
                DrawingHelpers.Offset(p.BuildingLength / 2.0, overallDimY, offset),
                DimLayer);
            count++;

            return count;
        }

        // ─────────────────────────────────────────────
        // Roof Pitch Annotation
        // ─────────────────────────────────────────────

        /// <summary>
        /// Adds a roof pitch text annotation (e.g., "4/12") on the front elevation
        /// roof slope with a small pitch triangle symbol.
        /// </summary>
        public static int AddRoofPitchAnnotation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;

            double halfW = p.BuildingWidth / 2.0;
            double midRoofX = halfW * 0.5;  // quarter point on left roof slope
            double midRoofY = p.EaveHeight + (geo.RoofRise * 0.5);

            // Pitch label text
            string pitchText = $"{p.RoofPitchRise:0.#}/12";
            double textHeight = Math.Max(0.5, p.BuildingWidth * 0.02);

            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(midRoofX, midRoofY + 1.0, offset),
                pitchText, textHeight, AnnoLayer);
            count++;

            // Small pitch triangle: scaled proportionally
            double triScale = Math.Max(1.0, p.BuildingWidth * 0.04);
            double triBaseX = midRoofX - triScale * 0.5;
            double triBaseY = midRoofY - 2.0;

            // Horizontal leg (run)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(triBaseX, triBaseY, offset),
                DrawingHelpers.Offset(triBaseX + triScale, triBaseY, offset),
                AnnoLayer);
            count++;

            // Vertical leg (rise)
            double triRise = triScale * (p.RoofPitchRise / 12.0);
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(triBaseX + triScale, triBaseY, offset),
                DrawingHelpers.Offset(triBaseX + triScale, triBaseY + triRise, offset),
                AnnoLayer);
            count++;

            // Hypotenuse (slope)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(triBaseX, triBaseY, offset),
                DrawingHelpers.Offset(triBaseX + triScale, triBaseY + triRise, offset),
                AnnoLayer);
            count++;

            return count;
        }

        // ─────────────────────────────────────────────
        // Dimension Style
        // ─────────────────────────────────────────────

        /// <summary>
        /// Ensures a "FS-DIM" dimension style exists using StyleManager as the single
        /// authoritative source for style definitions.
        /// </summary>
        public static ObjectId EnsureDimensionStyle(Transaction tr, Database db)
        {
            return StyleManager.GetDimensionStyleId(tr, db);
        }

        /// <summary>
        /// Sets FS-DIM as the current dimension style on the database.
        /// Call once before generating any dimensions.
        /// </summary>
        public static void SetCurrentDimStyle(Transaction tr, Database db)
        {
            ObjectId styleId = EnsureDimensionStyle(tr, db);
            db.Dimstyle = styleId;
        }
    }
}
