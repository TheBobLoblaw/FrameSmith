using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators.TrussProfiles
{
    /// <summary>
    /// Attic truss — creates usable storage/living space within the roof envelope.
    /// Has a flat ceiling section in the center with knee walls at the sides.
    /// Same exterior profile as common gable but with interior room framing.
    /// </summary>
    public class AtticTrussProfile : ITrussProfile
    {
        public string Name => "Attic Storage";
        public string Description => "Creates usable second-floor space within the roof. Flat ceiling center section with knee walls. Ideal for storage lofts or bonus rooms.";

        private const double DefaultAtticWidthRatio = 0.5;  // Attic room = 50% of building width
        private const double DefaultKneeWallHeight = 4.0;   // feet above eave (bottom chord)
        private const double DefaultAtticCeilingHeight = 8.0; // feet above eave

        public double CalculatePeakHeight(double eaveHeight, double span, double pitchRise)
        {
            // Same exterior as common gable
            return eaveHeight + (span / 2.0) * (pitchRise / 12.0);
        }

        private void GetAtticGeometry(BarnParameters p,
            out double atticLeft, out double atticRight,
            out double kneeWallZ, out double ceilingZ)
        {
            double halfW = p.BuildingWidth / 2.0;
            double atticWidth = p.BuildingWidth * DefaultAtticWidthRatio;
            atticLeft = halfW - atticWidth / 2.0;
            atticRight = halfW + atticWidth / 2.0;
            kneeWallZ = p.EaveHeight + DefaultKneeWallHeight;

            // Ceiling can't exceed roof slope at knee wall position
            double roofZAtKnee = p.EaveHeight + (atticLeft / halfW) * p.RoofRise;
            double maxCeiling = Math.Min(p.EaveHeight + DefaultAtticCeilingHeight, roofZAtKnee - 0.5);
            ceilingZ = Math.Max(kneeWallZ + 2.0, maxCeiling); // At least 2' above knee wall
        }

        public TrussProfile ComputeTruss(BarnParameters p, double bayPosition)
        {
            double halfWidth = p.BuildingWidth / 2.0;
            double peakHeight = CalculatePeakHeight(p.EaveHeight, p.BuildingWidth, p.RoofPitchRise);
            double overhangDrop = p.OverhangEave * (p.RoofPitchRise / 12.0);

            return new TrussProfile
            {
                BayPosition = bayPosition,
                LeftEaveX = -p.OverhangEave,
                RightEaveX = p.BuildingWidth + p.OverhangEave,
                LeftEaveZ = p.EaveHeight - overhangDrop,
                RightEaveZ = p.EaveHeight - overhangDrop,
                PeakX = halfWidth,
                PeakZ = peakHeight,
                BottomChordZ = p.EaveHeight
            };
        }

        public int RenderFrontElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double halfW = p.BuildingWidth / 2.0;
            double overhangDrop = p.OverhangEave * (p.RoofPitchRise / 12.0);
            GetAtticGeometry(p, out double aLeft, out double aRight,
                out double kneeZ, out double ceilingZ);

            // Exterior roof profile (same as common gable)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                DrawingHelpers.Offset(halfW, geo.PeakHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(halfW, geo.PeakHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            // Eave details
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight, offset),
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight, offset),
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                LayerManager.Layers.Roof);
            count++;
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            // Interior attic framing (dashed lines)
            Database db = btr.Database;
            try { db.LoadLineTypeFile("DASHED", "acad.lin"); }
            catch (Autodesk.AutoCAD.Runtime.Exception) { }

            // Attic floor (same as bottom chord)
            Line floor = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(aLeft, p.EaveHeight, offset),
                DrawingHelpers.Offset(aRight, p.EaveHeight, offset),
                LayerManager.Layers.Trusses);
            floor.Linetype = "DASHED";
            count++;

            // Left knee wall
            Line lKnee = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(aLeft, p.EaveHeight, offset),
                DrawingHelpers.Offset(aLeft, kneeZ, offset),
                LayerManager.Layers.Trusses);
            lKnee.Linetype = "DASHED";
            count++;

            // Right knee wall
            Line rKnee = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(aRight, p.EaveHeight, offset),
                DrawingHelpers.Offset(aRight, kneeZ, offset),
                LayerManager.Layers.Trusses);
            rKnee.Linetype = "DASHED";
            count++;

            // Ceiling
            Line ceiling = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(aLeft, ceilingZ, offset),
                DrawingHelpers.Offset(aRight, ceilingZ, offset),
                LayerManager.Layers.Trusses);
            ceiling.Linetype = "DASHED";
            count++;

            // Ceiling to knee wall connections
            Line lCeil = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(aLeft, kneeZ, offset),
                DrawingHelpers.Offset(aLeft, ceilingZ, offset),
                LayerManager.Layers.Trusses);
            lCeil.Linetype = "DASHED";
            count++;

            Line rCeil = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(aRight, kneeZ, offset),
                DrawingHelpers.Offset(aRight, ceilingZ, offset),
                LayerManager.Layers.Trusses);
            rCeil.Linetype = "DASHED";
            count++;

            return count;
        }

        public int RenderSideElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            // Same exterior as common gable
            var common = new CommonTrussProfile();
            return common.RenderSideElevation(tr, btr, geo, offset);
        }

        public int Render3DTruss(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, TrussProfile truss)
        {
            int count = 0;
            var p = geo.Params;
            double y = truss.BayPosition;
            double halfW = p.BuildingWidth / 2.0;
            GetAtticGeometry(p, out double aLeft, out double aRight,
                out double kneeZ, out double ceilingZ);

            // Standard gable top chords
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                halfW, y, geo.PeakHeight, LayerManager.Layers.Trusses);
            count++;
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, geo.PeakHeight,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // Bottom chord (full span)
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // King post
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, p.EaveHeight,
                halfW, y, geo.PeakHeight, LayerManager.Layers.Trusses);
            count++;

            // Attic knee walls
            DrawingHelpers.AddLine3D(tr, btr, aLeft, y, p.EaveHeight,
                aLeft, y, kneeZ, LayerManager.Layers.Trusses);
            count++;
            DrawingHelpers.AddLine3D(tr, btr, aRight, y, p.EaveHeight,
                aRight, y, kneeZ, LayerManager.Layers.Trusses);
            count++;

            // Attic ceiling
            DrawingHelpers.AddLine3D(tr, btr, aLeft, y, ceilingZ,
                aRight, y, ceilingZ, LayerManager.Layers.Trusses);
            count++;

            // Ceiling verticals
            DrawingHelpers.AddLine3D(tr, btr, aLeft, y, kneeZ,
                aLeft, y, ceilingZ, LayerManager.Layers.Trusses);
            count++;
            DrawingHelpers.AddLine3D(tr, btr, aRight, y, kneeZ,
                aRight, y, ceilingZ, LayerManager.Layers.Trusses);
            count++;

            return count;
        }

        public int RenderPlanRoofOutline(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            // Same as common gable in plan view (interior not visible from above)
            return 0;
        }

        public List<string> GetParameterNames() => new List<string>
        {
            "AtticWidthRatio",
            "KneeWallHeight",
            "AtticCeilingHeight"
        };
    }
}
