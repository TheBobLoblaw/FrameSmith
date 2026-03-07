using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators.TrussProfiles
{
    /// <summary>
    /// Scissor truss — bottom chord slopes upward creating a vaulted ceiling interior.
    /// Popular for barndominiums and living spaces.
    /// Bottom chord pitch is typically half the roof pitch.
    /// </summary>
    public class ScissorTrussProfile : ITrussProfile
    {
        public string Name => "Scissor (Vaulted)";
        public string Description => "Vaulted ceiling truss with upward-sloping bottom chord. Creates dramatic interior volume. Popular for barndominiums and living spaces.";

        private const double BottomChordPitchRatio = 0.5; // Bottom chord pitch = half of roof pitch

        public double CalculatePeakHeight(double eaveHeight, double span, double pitchRise)
        {
            // Same peak as common gable
            return eaveHeight + (span / 2.0) * (pitchRise / 12.0);
        }

        private double GetBottomChordPeakZ(BarnParameters p)
        {
            double bottomPitch = p.RoofPitchRise * BottomChordPitchRatio;
            return p.EaveHeight + (p.BuildingWidth / 2.0) * (bottomPitch / 12.0);
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
                BottomChordZ = p.EaveHeight, // At eaves; slopes up to center
                ScissorBottomPeakZ = GetBottomChordPeakZ(p)
            };
        }

        public int RenderFrontElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double halfW = p.BuildingWidth / 2.0;
            double overhangDrop = p.OverhangEave * (p.RoofPitchRise / 12.0);
            double bottomPeakZ = GetBottomChordPeakZ(p);

            // Top chords (same as common gable)
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

            // Bottom chord — slopes upward to center (vaulted ceiling line, dashed)
            Database db = btr.Database;
            try { db.LoadLineTypeFile("DASHED", "acad.lin"); }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                WarningCollector.ReportCurrent("Failed to load DASHED linetype for scissor truss profile", ex);
            }

            Line leftBottom = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                DrawingHelpers.Offset(halfW, bottomPeakZ, offset),
                LayerManager.Layers.Trusses);
            leftBottom.Linetype = "DASHED";
            count++;

            Line rightBottom = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(halfW, bottomPeakZ, offset),
                DrawingHelpers.Offset(p.BuildingWidth, p.EaveHeight, offset),
                LayerManager.Layers.Trusses);
            rightBottom.Linetype = "DASHED";
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

            // Fascia
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

            return count;
        }

        public int RenderSideElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            // Side elevation is identical to common gable (same exterior profile)
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
            double bottomPeakZ = GetBottomChordPeakZ(p);

            // Left top chord
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                halfW, y, geo.PeakHeight, LayerManager.Layers.Trusses);
            count++;

            // Right top chord
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, geo.PeakHeight,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // Left bottom chord (sloping upward)
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                halfW, y, bottomPeakZ, LayerManager.Layers.Trusses);
            count++;

            // Right bottom chord (sloping downward)
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, bottomPeakZ,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // Vertical at center (shortened king post)
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, bottomPeakZ,
                halfW, y, geo.PeakHeight, LayerManager.Layers.Trusses);
            count++;

            // Web members (cross pattern creating "scissor" look)
            double qx = halfW / 2.0;
            double topQz = p.EaveHeight + geo.RoofRise / 2.0;
            double botQz = p.EaveHeight + (bottomPeakZ - p.EaveHeight) / 2.0;

            DrawingHelpers.AddLine3D(tr, btr, qx, y, botQz,
                qx, y, topQz, LayerManager.Layers.Trusses);
            count++;
            DrawingHelpers.AddLine3D(tr, btr, p.BuildingWidth - qx, y, botQz,
                p.BuildingWidth - qx, y, topQz, LayerManager.Layers.Trusses);
            count++;

            return count;
        }

        public int RenderPlanRoofOutline(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            // Same as common gable in plan view
            return 0;
        }

        public List<string> GetParameterNames() => new List<string>
        {
            "ScissorBottomPitchRatio"
        };
    }
}
