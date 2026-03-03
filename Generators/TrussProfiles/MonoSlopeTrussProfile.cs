using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators.TrussProfiles
{
    /// <summary>
    /// Mono-slope (shed) roof — single continuous slope from high wall to low wall.
    /// Simplest and most cost-effective roof type. Good for equipment storage.
    /// Left wall is high, right wall is low (slope falls left to right).
    /// </summary>
    public class MonoSlopeTrussProfile : ITrussProfile
    {
        public string Name => "Mono-Slope (Shed)";
        public string Description => "Single-pitch shed roof sloping from high wall (left) to low wall (right). Cost-effective for equipment storage and large spans.";

        public double CalculatePeakHeight(double eaveHeight, double span, double pitchRise)
        {
            // "Peak" is the high wall = eave + full span rise
            return eaveHeight + span * (pitchRise / 12.0);
        }

        private double GetHighWallHeight(BarnParameters p)
        {
            return p.EaveHeight + p.BuildingWidth * (p.RoofPitchRise / 12.0);
        }

        public TrussProfile ComputeTruss(BarnParameters p, double bayPosition)
        {
            double highWall = GetHighWallHeight(p);

            return new TrussProfile
            {
                BayPosition = bayPosition,
                LeftEaveX = -p.OverhangEave,
                RightEaveX = p.BuildingWidth + p.OverhangEave,
                LeftEaveZ = highWall + p.OverhangEave * (p.RoofPitchRise / 12.0),
                RightEaveZ = p.EaveHeight - p.OverhangEave * (p.RoofPitchRise / 12.0),
                PeakX = 0, // No peak — continuous slope
                PeakZ = highWall,
                BottomChordZ = p.EaveHeight
            };
        }

        public int RenderFrontElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double highWall = GetHighWallHeight(p);
            double ovhDrop = p.OverhangEave * (p.RoofPitchRise / 12.0);

            // Left post extends to high wall
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(0, p.EaveHeight, offset),
                DrawingHelpers.Offset(0, highWall, offset),
                LayerManager.Layers.Posts);
            count++;

            // Roof slope: high wall (left) to low wall (right) with overhangs
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, highWall + ovhDrop, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight - ovhDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            // Eave extensions (horizontal at wall tops)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, highWall, offset),
                DrawingHelpers.Offset(0, highWall, offset),
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Fascia verticals
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, highWall, offset),
                DrawingHelpers.Offset(-p.OverhangEave, highWall + ovhDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight - ovhDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            return count;
        }

        public int RenderSideElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            // Side view shows the slope as the roof plane at a single height
            // For mono-slope, show both eave line and high wall line
            double highWall = GetHighWallHeight(p);

            // Low eave line
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // High wall line
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, highWall, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, highWall, offset),
                LayerManager.Layers.Roof);
            count++;

            // Gable fascia
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(-p.OverhangGable, highWall, offset),
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, highWall, offset),
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
            double highWall = GetHighWallHeight(p);

            // Bottom chord at low eave height
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // Top chord: high wall to low wall (single slope)
            DrawingHelpers.AddLine3D(tr, btr, 0, y, highWall,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // Vertical at high wall
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                0, y, highWall, LayerManager.Layers.Trusses);
            count++;

            // Web members at quarter and three-quarter points
            double q1x = p.BuildingWidth * 0.25;
            double q1z = highWall - (highWall - p.EaveHeight) * 0.25;
            DrawingHelpers.AddLine3D(tr, btr, q1x, y, p.EaveHeight,
                q1x, y, q1z, LayerManager.Layers.Trusses);
            count++;

            double q2x = p.BuildingWidth * 0.5;
            double q2z = highWall - (highWall - p.EaveHeight) * 0.5;
            DrawingHelpers.AddLine3D(tr, btr, q2x, y, p.EaveHeight,
                q2x, y, q2z, LayerManager.Layers.Trusses);
            count++;

            double q3x = p.BuildingWidth * 0.75;
            double q3z = highWall - (highWall - p.EaveHeight) * 0.75;
            DrawingHelpers.AddLine3D(tr, btr, q3x, y, p.EaveHeight,
                q3x, y, q3z, LayerManager.Layers.Trusses);
            count++;

            return count;
        }

        public int RenderPlanRoofOutline(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            // Mono-slope has no ridge line — just the rectangular outline
            // which is already drawn by PlanViewGenerator
            return 0;
        }

        public List<string> GetParameterNames() => new List<string>();
    }
}
