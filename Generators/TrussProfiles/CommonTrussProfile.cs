using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators.TrussProfiles
{
    /// <summary>
    /// Standard gable roof — symmetrical slopes meeting at center ridge.
    /// This is the default and most common pole barn truss type.
    /// </summary>
    public class CommonTrussProfile : ITrussProfile
    {
        public string Name => "Common Gable";
        public string Description => "Standard symmetrical gable roof with peak at center. The most common pole barn roof type.";

        public double CalculatePeakHeight(double eaveHeight, double span, double pitchRise)
        {
            return eaveHeight + (span / 2.0) * (pitchRise / 12.0);
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

            // Left slope: overhang to peak
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                DrawingHelpers.Offset(halfW, geo.PeakHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Right slope: peak to overhang
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(halfW, geo.PeakHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            // Eave extensions and fascia
            count += RenderEaveDetails(tr, btr, p, offset);

            return count;
        }

        public int RenderSideElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double roofTopZ = p.EaveHeight + p.OverhangEave * (p.RoofPitchRise / 12.0);

            // Eave line (flat across length)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Ridge line (top of roof slope visible from side)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, roofTopZ, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, roofTopZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Gable fascia
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(-p.OverhangGable, roofTopZ, offset),
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, roofTopZ, offset),
                LayerManager.Layers.Roof);
            count++;

            return count;
        }

        public int Render3DTruss(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, TrussProfile truss)
        {
            int count = 0;
            var p = geo.Params;
            double y = truss.BayPosition;
            double halfW = p.BuildingWidth / 2.0;

            // Bottom chord
            DrawingHelpers.AddLine3D(tr, btr, 0, y, truss.BottomChordZ,
                p.BuildingWidth, y, truss.BottomChordZ, LayerManager.Layers.Trusses);
            count++;

            // Left top chord
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                halfW, y, geo.PeakHeight, LayerManager.Layers.Trusses);
            count++;

            // Right top chord
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, geo.PeakHeight,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // King post
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, truss.BottomChordZ,
                halfW, y, geo.PeakHeight, LayerManager.Layers.Trusses);
            count++;

            // Web members at quarter points
            double qx = halfW / 2.0;
            double qz = p.EaveHeight + geo.RoofRise / 2.0;
            DrawingHelpers.AddLine3D(tr, btr, qx, y, truss.BottomChordZ,
                qx, y, qz, LayerManager.Layers.Trusses);
            count++;
            DrawingHelpers.AddLine3D(tr, btr, p.BuildingWidth - qx, y, truss.BottomChordZ,
                p.BuildingWidth - qx, y, qz, LayerManager.Layers.Trusses);
            count++;

            return count;
        }

        public int RenderPlanRoofOutline(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            // Standard rectangular roof outline with center ridge — handled by PlanViewGenerator
            return 0;
        }

        public List<string> GetParameterNames() => new List<string>();

        private int RenderEaveDetails(Transaction tr, BlockTableRecord btr,
            BarnParameters p, Vector3d offset)
        {
            int count = 0;
            double overhangDrop = p.OverhangEave * (p.RoofPitchRise / 12.0);

            // Left eave extension
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight, offset),
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Right eave extension
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Left fascia
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight, offset),
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            // Right fascia
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            return count;
        }
    }
}
