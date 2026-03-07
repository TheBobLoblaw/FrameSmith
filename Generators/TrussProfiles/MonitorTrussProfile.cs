using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators.TrussProfiles
{
    /// <summary>
    /// Monitor roof — raised center section above the main roof line.
    /// Creates clerestory windows for natural light and ventilation.
    /// Ideal for horse barns and livestock buildings.
    /// Monitor width defaults to 1/3 of building width, centered.
    /// </summary>
    public class MonitorTrussProfile : ITrussProfile
    {
        public string Name => "Monitor (Raised Center)";
        public string Description => "Raised center section with clerestory windows for natural light and ventilation. Ideal for horse barns, arenas, and livestock buildings.";

        private const double DefaultMonitorWidthRatio = 0.333;  // 1/3 of building width
        private const double DefaultMonitorRise = 4.0;          // feet above main roof at center

        public double CalculatePeakHeight(double eaveHeight, double span, double pitchRise)
        {
            double mainPeak = eaveHeight + (span / 2.0) * (pitchRise / 12.0);
            return mainPeak + DefaultMonitorRise;
        }

        private void GetMonitorGeometry(BarnParameters p,
            out double monitorLeft, out double monitorRight,
            out double mainRoofZAtMonitor, out double monitorTopZ)
        {
            double halfW = p.BuildingWidth / 2.0;
            double monitorWidth = p.BuildingWidth * DefaultMonitorWidthRatio;
            monitorLeft = halfW - monitorWidth / 2.0;
            monitorRight = halfW + monitorWidth / 2.0;

            // Height of main roof slope at the monitor wall lines
            double slopeRatio = monitorLeft / halfW; // fraction along slope
            mainRoofZAtMonitor = p.EaveHeight + slopeRatio * p.RoofRise;

            monitorTopZ = mainRoofZAtMonitor + DefaultMonitorRise;
        }

        public TrussProfile ComputeTruss(BarnParameters p, double bayPosition)
        {
            GetMonitorGeometry(p, out double monitorLeft, out double monitorRight,
                out double mainRoofZ, out double monitorTopZ);
            double halfWidth = p.BuildingWidth / 2.0;
            double overhangDrop = p.OverhangEave * (p.RoofPitchRise / 12.0);

            return new TrussProfile
            {
                BayPosition = bayPosition,
                LeftEaveX = -p.OverhangEave,
                RightEaveX = p.BuildingWidth + p.OverhangEave,
                LeftEaveZ = p.EaveHeight - overhangDrop,
                RightEaveZ = p.EaveHeight - overhangDrop,
                PeakX = halfWidth,
                PeakZ = monitorTopZ,
                BottomChordZ = p.EaveHeight,
                MonitorLeftX = monitorLeft,
                MonitorRightX = monitorRight,
                MonitorBaseZ = mainRoofZ,
                MonitorTopZ = monitorTopZ
            };
        }

        public int RenderFrontElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            double halfW = p.BuildingWidth / 2.0;
            double overhangDrop = p.OverhangEave * (p.RoofPitchRise / 12.0);
            GetMonitorGeometry(p, out double mLeft, out double mRight,
                out double mainRoofZ, out double monitorTopZ);

            // Left main slope: eave to monitor wall
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                DrawingHelpers.Offset(mLeft, mainRoofZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Left monitor wall (vertical)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(mLeft, mainRoofZ, offset),
                DrawingHelpers.Offset(mLeft, monitorTopZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Monitor roof: left wall to center peak
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(mLeft, monitorTopZ, offset),
                DrawingHelpers.Offset(halfW, monitorTopZ + 1.0, offset), // slight peak on monitor
                LayerManager.Layers.Roof);
            count++;

            // Monitor roof: center to right wall
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(halfW, monitorTopZ + 1.0, offset),
                DrawingHelpers.Offset(mRight, monitorTopZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Right monitor wall (vertical)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(mRight, monitorTopZ, offset),
                DrawingHelpers.Offset(mRight, mainRoofZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Right main slope: monitor wall to eave
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(mRight, mainRoofZ, offset),
                DrawingHelpers.Offset(p.BuildingWidth + p.OverhangEave, p.EaveHeight - overhangDrop, offset),
                LayerManager.Layers.Roof);
            count++;

            // Clerestory windows (simple rectangles on monitor walls)
            double windowHeight = DefaultMonitorRise * 0.6;
            double windowBottom = mainRoofZ + (DefaultMonitorRise - windowHeight) / 2.0;

            // Left clerestory
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(mLeft, windowBottom, offset),
                DrawingHelpers.Offset(mLeft, windowBottom + windowHeight, offset),
                LayerManager.Layers.Windows);
            count++;

            // Right clerestory
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(mRight, windowBottom, offset),
                DrawingHelpers.Offset(mRight, windowBottom + windowHeight, offset),
                LayerManager.Layers.Windows);
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
            int count = 0;
            var p = geo.Params;
            GetMonitorGeometry(p, out _, out _,
                out double mainRoofZ, out double monitorTopZ);

            // Main eave line
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, p.EaveHeight, offset),
                LayerManager.Layers.Roof);
            count++;

            // Main roof line (at monitor wall height)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, mainRoofZ, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, mainRoofZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Monitor top
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, monitorTopZ, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, monitorTopZ, offset),
                LayerManager.Layers.Roof);
            count++;

            // Gable fascia (full height including monitor)
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(-p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(-p.OverhangGable, monitorTopZ, offset),
                LayerManager.Layers.Roof);
            count++;

            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, p.EaveHeight, offset),
                DrawingHelpers.Offset(p.BuildingLength + p.OverhangGable, monitorTopZ, offset),
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
            GetMonitorGeometry(p, out double mLeft, out double mRight,
                out double mainRoofZ, out double monitorTopZ);

            // Bottom chord
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // Left main slope
            DrawingHelpers.AddLine3D(tr, btr, 0, y, p.EaveHeight,
                mLeft, y, mainRoofZ, LayerManager.Layers.Trusses);
            count++;

            // Left monitor wall
            DrawingHelpers.AddLine3D(tr, btr, mLeft, y, mainRoofZ,
                mLeft, y, monitorTopZ, LayerManager.Layers.Trusses);
            count++;

            // Monitor roof left to center
            DrawingHelpers.AddLine3D(tr, btr, mLeft, y, monitorTopZ,
                halfW, y, monitorTopZ + 1.0, LayerManager.Layers.Trusses);
            count++;

            // Monitor roof center to right
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, monitorTopZ + 1.0,
                mRight, y, monitorTopZ, LayerManager.Layers.Trusses);
            count++;

            // Right monitor wall
            DrawingHelpers.AddLine3D(tr, btr, mRight, y, monitorTopZ,
                mRight, y, mainRoofZ, LayerManager.Layers.Trusses);
            count++;

            // Right main slope
            DrawingHelpers.AddLine3D(tr, btr, mRight, y, mainRoofZ,
                p.BuildingWidth, y, p.EaveHeight, LayerManager.Layers.Trusses);
            count++;

            // King post
            DrawingHelpers.AddLine3D(tr, btr, halfW, y, p.EaveHeight,
                halfW, y, monitorTopZ + 1.0, LayerManager.Layers.Trusses);
            count++;

            // Monitor wall verticals from bottom chord
            DrawingHelpers.AddLine3D(tr, btr, mLeft, y, p.EaveHeight,
                mLeft, y, mainRoofZ, LayerManager.Layers.Trusses);
            count++;
            DrawingHelpers.AddLine3D(tr, btr, mRight, y, p.EaveHeight,
                mRight, y, mainRoofZ, LayerManager.Layers.Trusses);
            count++;

            return count;
        }

        public int RenderPlanRoofOutline(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            GetMonitorGeometry(p, out double mLeft, out double mRight, out _, out _);

            Database db = btr.Database;
            try { db.LoadLineTypeFile("DASHED", "acad.lin"); }
            catch (Autodesk.AutoCAD.Runtime.Exception ex)
            {
                WarningCollector.ReportCurrent("Failed to load DASHED linetype for monitor truss profile", ex);
            }

            // Monitor outline in plan view (dashed rectangle)
            Line l1 = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(mLeft, -p.OverhangGable, offset),
                DrawingHelpers.Offset(mLeft, p.BuildingLength + p.OverhangGable, offset),
                LayerManager.Layers.Roof);
            l1.Linetype = "DASHED";
            count++;

            Line l2 = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(mRight, -p.OverhangGable, offset),
                DrawingHelpers.Offset(mRight, p.BuildingLength + p.OverhangGable, offset),
                LayerManager.Layers.Roof);
            l2.Linetype = "DASHED";
            count++;

            return count;
        }

        public List<string> GetParameterNames() => new List<string>
        {
            "MonitorWidthRatio",
            "MonitorRise"
        };
    }
}
