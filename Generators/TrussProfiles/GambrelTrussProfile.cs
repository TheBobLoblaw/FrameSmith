using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators.TrussProfiles
{
    /// <summary>
    /// Gambrel (barn) roof — two slopes per side.
    /// Steep lower slope (60°) transitions to shallow upper slope (25°) at the break point.
    /// Creates the classic barn appearance with maximum usable loft space.
    /// </summary>
    public class GambrelTrussProfile : ITrussProfile
    {
        public string Name => "Gambrel (Barn)";
        public string Description => "Classic dual-slope barn roof. Steep lower walls maximize loft space. Ideal for horse barns and traditional agriculture buildings.";

        // Default gambrel parameters (stored on BarnParameters via TrussParams dictionary)
        private const double DefaultLowerAngle = 60.0;  // degrees from horizontal
        private const double DefaultUpperAngle = 25.0;  // degrees from horizontal
        private const double DefaultBreakRatio = 0.35;   // break point as ratio of half-span from eave

        public double CalculatePeakHeight(double eaveHeight, double span, double pitchRise)
        {
            double halfSpan = span / 2.0;
            double breakDist = halfSpan * DefaultBreakRatio;
            double lowerRise = breakDist * Math.Tan(DefaultLowerAngle * Math.PI / 180.0);
            double upperRun = halfSpan - breakDist;
            double upperRise = upperRun * Math.Tan(DefaultUpperAngle * Math.PI / 180.0);
            return eaveHeight + lowerRise + upperRise;
        }

        private void GetGambrelPoints(BarnParameters p,
            out double breakX, out double breakZ, out double peakZ)
        {
            double halfSpan = p.BuildingWidth / 2.0;
            double breakDist = halfSpan * DefaultBreakRatio;
            breakX = breakDist;
            double lowerRise = breakDist * Math.Tan(DefaultLowerAngle * Math.PI / 180.0);
            breakZ = p.EaveHeight + lowerRise;
            double upperRun = halfSpan - breakDist;
            double upperRise = upperRun * Math.Tan(DefaultUpperAngle * Math.PI / 180.0);
            peakZ = breakZ + upperRise;
        }

        public TrussProfile ComputeTruss(BarnParameters p, double bayPosition)
        {
            GetGambrelPoints(p, out double breakX, out double breakZ, out double peakZ);
            double halfWidth = p.BuildingWidth / 2.0;

            return new TrussProfile
            {
                BayPosition = bayPosition,
                LeftEaveX = -p.OverhangEave,
                RightEaveX = p.BuildingWidth + p.OverhangEave,
                LeftEaveZ = p.EaveHeight,
                RightEaveZ = p.EaveHeight,
                PeakX = halfWidth,
                PeakZ = peakZ,
                BottomChordZ = p.EaveHeight,
                // Store break points in extra fields
                GambrelBreakX = breakX,
                GambrelBreakZ = breakZ
            };
        }

        public int RenderFrontElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            GetGambrelPoints(p, out double breakX, out double breakZ, out double peakZ);
            double halfW = p.BuildingWidth / 2.0;

            // Left side: eave → break point (steep)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                DrawingHelpers.Offset(breakX, breakZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Left side: break point → peak (shallow)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(breakX, breakZ, offset),
                DrawingHelpers.Offset(halfW, peakZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Right side: peak → break point (shallow)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(halfW, peakZ, offset),
                DrawingHelpers.Offset(p.BuildingWidth - breakX, breakZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Right side: break point → eave (steep)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth - breakX, breakZ, offset),
                DrawingHelpers.Offset(p.BuildingWidth, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Eave extensions
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

            return count;
        }

        public int RenderSideElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            GetGambrelPoints(p, out _, out double breakZ, out double peakZ);

            // Eave line
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Break line (visible from side as horizontal line at break height)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, breakZ, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, breakZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Ridge line at peak
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, peakZ, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, peakZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Gable fascia
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(-p.OverhangGable, peakZ, offset),
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, peakZ, offset),
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
            GetGambrelPoints(p, out double breakX, out double breakZ, out double peakZ);
            double halfW = p.BuildingWidth / 2.0;

            // Bottom chord
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // Left lower slope
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                breakX, y, breakZ, LayerManager.Layers.Trusses);
            count++;

            // Left upper slope
            DrawingHelpers.AddLine3D(tr, btr, breakX, y, breakZ,
                halfW, y, peakZ, LayerManager.Layers.Trusses);
            count++;

            // Right upper slope
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, peakZ,
                p.BuildingWidth - breakX, y, breakZ, LayerManager.Layers.Trusses);
            count++;

            // Right lower slope
            DrawingHelpers.AddLine3D(tr, btr, p.BuildingWidth - breakX, y, breakZ,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // King post
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, p.EaveHeight,
                halfW, y, peakZ, LayerManager.Layers.Trusses);
            count++;

            // Vertical at break points
            DrawingHelpers.AddLine3D(tr, btr, breakX, y, p.EaveHeight,
                breakX, y, breakZ, LayerManager.Layers.Trusses);
            count++;
            DrawingHelpers.AddLine3D(tr, btr, p.BuildingWidth - breakX, y, p.EaveHeight,
                p.BuildingWidth - breakX, y, breakZ, LayerManager.Layers.Trusses);
            count++;

            return count;
        }

        public int RenderPlanRoofOutline(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            GetGambrelPoints(p, out double breakX, out _, out _);

            // Break lines visible in plan view (dashed lines parallel to ridge)
            Database db = btr.Database;
            try { db.LoadLineTypeFile("DASHED", "acad.lin"); }
            catch (Autodesk.AutoCAD.Runtime.Exception) { }

            // Left break line
            Line leftBreak = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(breakX, -p.OverhangGable, offset),
                DrawingHelpers.Offset(breakX, p.BuildingLength + p.OverhangGable, offset),
                LayerManager.Layers.Roof);
            leftBreak.Linetype = "DASHED";
            count++;

            // Right break line
            Line rightBreak = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth - breakX, -p.OverhangGable, offset),
                DrawingHelpers.Offset(p.BuildingWidth - breakX, p.BuildingLength + p.OverhangGable, offset),
                LayerManager.Layers.Roof);
            rightBreak.Linetype = "DASHED";
            count++;

            return count;
        }

        public List<string> GetParameterNames() => new List<string>
        {
            "GambrelLowerAngle",
            "GambrelUpperAngle",
            "GambrelBreakRatio"
        };
    }
}
