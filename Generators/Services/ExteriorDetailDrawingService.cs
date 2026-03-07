using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;

namespace PoleBarnGenerator.Generators.Services
{
    /// <summary>
    /// Draws exterior-detail add-ons such as wainscot, gutters, ventilation, and equipment notes.
    /// </summary>
    public static class ExteriorDetailDrawingService
    {
        public static int AddPlanExteriorDetails(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            var p = geo.Params;
            int count = 0;

            try
            {
                count += ExteriorDetailGenerator.AddCupolaPlan(tr, btr, geo, p.Cupola, offset);
                count += ExteriorDetailGenerator.AddGutterPlan(tr, btr, geo, p.Gutters, offset);
            }
            catch (Exception)
            {
                // Skip failed detail render.
            }

            return count;
        }

        public static int AddFrontElevationExteriorDetails(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            var p = geo.Params;
            int count = 0;

            try
            {
                count += ExteriorDetailGenerator.AddWainscotFrontElevation(tr, btr, geo, p.Wainscot, offset);
                count += ExteriorDetailGenerator.AddCupolaFrontElevation(tr, btr, geo, p.Cupola, offset);
                count += ExteriorDetailGenerator.AddGutterFrontElevation(tr, btr, geo, p.Gutters, offset);
                count += AddFrontVentilationOpenings(tr, btr, geo, offset);
                count += AddFrontEquipmentDetails(tr, btr, geo, offset);
            }
            catch (Exception)
            {
                // Skip failed detail render.
            }

            return count;
        }

        public static int AddSideElevationExteriorDetails(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            var p = geo.Params;
            int count = 0;

            try
            {
                count += ExteriorDetailGenerator.AddWainscotSideElevation(tr, btr, geo, p.Wainscot, offset);
                count += AddSideVentilationOpenings(tr, btr, geo, offset);
                count += AddSideEquipmentDetails(tr, btr, geo, offset);
            }
            catch (Exception)
            {
                // Skip failed detail render.
            }

            return count;
        }

        private static int AddFrontVentilationOpenings(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            var v = p.Ventilation;
            if (v?.IsEnabled != true && p.DairyBarn?.IsEnabled != true)
            {
                return 0;
            }

            if (v?.RidgeVentEnabled == true)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth * 0.25, geo.PeakHeight - 0.2, offset),
                    DrawingHelpers.Offset(p.BuildingWidth * 0.75, geo.PeakHeight - 0.2, offset),
                    LayerManager.Layers.Vent);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth * 0.5, geo.PeakHeight + 0.4, offset),
                    "RIDGE VENT", 0.35, LayerManager.Layers.Vent);
                count += 2;
            }

            int louvers = Math.Max(2, v?.WallLouverCount ?? 2);
            double spacing = p.BuildingWidth / (louvers + 1);
            for (int i = 1; i <= louvers; i++)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(i * spacing - 0.6, p.EaveHeight * 0.55, offset),
                    1.2, 0.9, LayerManager.Layers.Vent);
                count++;
            }

            int cupolas = Math.Max(0, v?.CupolaCount ?? 0);
            if (cupolas > 0)
            {
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(1.0, p.EaveHeight + 1.2, offset),
                    $"CUPOLAS: {cupolas} @ {v.CupolaAirflowCfmEach:F0} CFM", 0.35, LayerManager.Layers.Vent);
                count++;
            }

            return count;
        }

        private static int AddFrontEquipmentDetails(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            var equip = p.EquipmentStorage;
            if (equip?.IsEnabled != true)
            {
                return 0;
            }

            if (equip.CraneRail.IsEnabled)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, equip.CraneRail.RailHeight, offset),
                    DrawingHelpers.Offset(p.BuildingWidth, equip.CraneRail.RailHeight, offset),
                    LayerManager.Layers.Crane);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(p.BuildingWidth * 0.5, equip.CraneRail.RailHeight + 0.4, offset),
                    $"CRANE RAIL {equip.CraneRail.CapacityTons:F1}T", 0.35, LayerManager.Layers.Crane);
                count += 2;
            }

            if (equip.LargeDoor.IsEnabled &&
                (equip.LargeDoor.Wall == WallSide.Front || equip.LargeDoor.Wall == WallSide.Back))
            {
                double x0 = Math.Max(0, (p.BuildingWidth - equip.LargeDoor.Width) / 2.0);
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(x0, 0, offset),
                    equip.LargeDoor.Width, equip.LargeDoor.Height, LayerManager.Layers.Equip);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(x0 + equip.LargeDoor.Width / 2.0, equip.LargeDoor.Height + 0.5, offset),
                    $"{equip.LargeDoor.DoorType} DOOR {equip.LargeDoor.Width:F0}'", 0.35, LayerManager.Layers.Equip);
                count += 2;
            }

            return count;
        }

        private static int AddSideVentilationOpenings(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            var v = p.Ventilation;
            if (v?.IsEnabled != true && p.DairyBarn?.IsEnabled != true)
            {
                return 0;
            }

            if (v?.RidgeVentEnabled == true)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(p.BuildingLength * 0.25, p.EaveHeight + 0.25, offset),
                    DrawingHelpers.Offset(p.BuildingLength * 0.75, p.EaveHeight + 0.25, offset),
                    LayerManager.Layers.Vent);
                count++;
            }

            int louverCount = Math.Max(2, v?.WallLouverCount ?? 2);
            double spacing = p.BuildingLength / (louverCount + 1);
            for (int i = 1; i <= louverCount; i++)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(i * spacing - 0.5, p.EaveHeight * 0.55, offset),
                    1.0, 0.8, LayerManager.Layers.Vent);
                count++;
            }

            return count;
        }

        private static int AddSideEquipmentDetails(Transaction tr, BlockTableRecord btr, BarnGeometry geo, Vector3d offset)
        {
            int count = 0;
            var p = geo.Params;
            var equip = p.EquipmentStorage;
            if (equip?.IsEnabled != true)
            {
                return 0;
            }

            if (equip.CraneRail.IsEnabled)
            {
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0, equip.CraneRail.RailHeight, offset),
                    DrawingHelpers.Offset(p.BuildingLength, equip.CraneRail.RailHeight, offset),
                    LayerManager.Layers.Crane);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(p.BuildingLength * 0.5, equip.CraneRail.RailHeight + 0.4, offset),
                    $"CRANE RAIL {equip.CraneRail.CapacityTons:F1}T", 0.35, LayerManager.Layers.Crane);
                count += 2;
            }

            if (equip.LargeDoor.IsEnabled &&
                (equip.LargeDoor.Wall == WallSide.Left || equip.LargeDoor.Wall == WallSide.Right))
            {
                double x0 = Math.Max(0, (p.BuildingLength - equip.LargeDoor.Width) / 2.0);
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(x0, 0, offset),
                    equip.LargeDoor.Width, equip.LargeDoor.Height, LayerManager.Layers.Equip);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(x0 + equip.LargeDoor.Width / 2.0, equip.LargeDoor.Height + 0.5, offset),
                    $"{equip.LargeDoor.DoorType} DOOR {equip.LargeDoor.Width:F0}'", 0.35, LayerManager.Layers.Equip);
                count += 2;
            }

            return count;
        }
    }
}
