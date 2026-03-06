using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates interior features in plan view: horse stalls, loft, partitions, workshop.
    /// Interior features are plan-view only — they don't appear in elevations.
    /// </summary>
    public static class InteriorGenerator
    {
        // ════════════════════════════════════════════════════
        // Horse Stalls
        // ════════════════════════════════════════════════════

        public static int GenerateHorseStalls(Transaction tr, BlockTableRecord btr,
            BarnGeometry mainGeo, StallLayout stallGeo, Vector3d offset)
        {
            if (stallGeo == null) return 0;
            int count = 0;

            // ── Center aisle ──
            var aisle = stallGeo.CenterAisle;
            if (aisle != null)
            {
                // Aisle outline (dashed)
                var aisleRect = DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(aisle.X, aisle.Y, offset),
                    aisle.Width, aisle.Height,
                    LayerManager.Layers.Stalls);
                try { aisleRect.Linetype = "DASHED"; } catch { }
                count++;

                // Aisle centerline
                double centerX = aisle.X + aisle.Width / 2.0;
                var centerLine = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(centerX, aisle.Y, offset),
                    DrawingHelpers.Offset(centerX, aisle.Y + aisle.Height, offset),
                    LayerManager.Layers.Stalls);
                try { centerLine.Linetype = "CENTER"; } catch { }
                count++;

                // Aisle width dimension
                DrawingHelpers.AddAlignedDim(tr, btr,
                    DrawingHelpers.Offset(aisle.X, aisle.Y - 1.5, offset),
                    DrawingHelpers.Offset(aisle.X + aisle.Width, aisle.Y - 1.5, offset),
                    DrawingHelpers.Offset(aisle.X + aisle.Width / 2.0, aisle.Y - 2.5, offset),
                    LayerManager.Layers.Dims);
                count++;
            }

            // ── Stall outlines ──
            foreach (var stall in stallGeo.StallOutlines)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(stall.X, stall.Y, offset),
                    stall.Width, stall.Height,
                    LayerManager.Layers.Stalls);
                count++;

                // Stall dimension (width)
                DrawingHelpers.AddAlignedDim(tr, btr,
                    DrawingHelpers.Offset(stall.X, stall.Y, offset),
                    DrawingHelpers.Offset(stall.X, stall.Y + stall.Height, offset),
                    DrawingHelpers.Offset(stall.X - 1.0, stall.Y + stall.Height / 2.0, offset),
                    LayerManager.Layers.Dims);
                count++;
            }

            // ── Gate symbols (small arc showing swing direction) ──
            foreach (var gate in stallGeo.StallGates)
            {
                // Simple gate symbol: short line with arc
                double gateWidth = 4.0; // 4' gate
                DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(gate.X, gate.Y - gateWidth / 2.0, offset),
                    DrawingHelpers.Offset(gate.X, gate.Y + gateWidth / 2.0, offset),
                    LayerManager.Layers.Stalls);
                count++;

                // Gate swing arc (quarter circle)
                Arc arc = new Arc(
                    DrawingHelpers.Offset(gate.X, gate.Y - gateWidth / 2.0, offset),
                    gateWidth,
                    0, Math.PI / 2.0);
                LayerManager.SetLayer(arc, LayerManager.Layers.Stalls);
                btr.AppendEntity(arc);
                tr.AddNewlyCreatedDBObject(arc, true);
                count++;
            }

            // ── Water fixture symbols (small circle with W) ──
            foreach (var water in stallGeo.WaterLocations)
            {
                Circle waterSymbol = new Circle(
                    DrawingHelpers.Offset(water.X, water.Y, offset),
                    Vector3d.ZAxis, 0.3);
                LayerManager.SetLayer(waterSymbol, LayerManager.Layers.Stalls);
                btr.AppendEntity(waterSymbol);
                tr.AddNewlyCreatedDBObject(waterSymbol, true);
                count++;

                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(water.X, water.Y - 0.5, offset),
                    "W", 0.25, LayerManager.Layers.Stalls);
                count++;
            }

            // ── Feed fixture symbols (small square with F) ──
            foreach (var feed in stallGeo.FeedLocations)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(feed.X - 0.25, feed.Y - 0.25, offset),
                    0.5, 0.5, LayerManager.Layers.Stalls);
                count++;

                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(feed.X, feed.Y - 0.5, offset),
                    "F", 0.25, LayerManager.Layers.Stalls);
                count++;
            }

            // ── Specialty rooms ──
            foreach (var room in stallGeo.SpecialtyRooms)
            {
                var outline = room.Outline;
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(outline.X, outline.Y, offset),
                    outline.Width, outline.Height,
                    LayerManager.Layers.Stalls);
                count++;

                // Room label
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(outline.X + outline.Width / 2.0,
                        outline.Y + outline.Height / 2.0, offset),
                    room.Name, 0.75, LayerManager.Layers.Anno);
                count++;

                // Floor drain symbol for wash bay
                if (room.HasDrain)
                {
                    double drainX = outline.X + outline.Width / 2.0;
                    double drainY = outline.Y + outline.Height / 2.0 - 1.0;
                    AddFloorDrainSymbol(tr, btr, drainX, drainY, offset);
                    count++;
                }
            }

            return count;
        }

        // ════════════════════════════════════════════════════
        // Loft / Mezzanine
        // ════════════════════════════════════════════════════

        public static int GenerateLoft(Transaction tr, BlockTableRecord btr,
            BarnGeometry mainGeo, LoftGeometryData loftGeo, Vector3d offset)
        {
            if (loftGeo?.FloorOutline == null) return 0;
            int count = 0;

            var floor = loftGeo.FloorOutline;

            // ── Loft floor outline (thick) ──
            var loftOutline = DrawingHelpers.AddRectangle(tr, btr,
                DrawingHelpers.Offset2d(floor.X, floor.Y, offset),
                floor.Width, floor.Height,
                LayerManager.Layers.Loft);
            count++;

            // ── Diagonal hatch pattern to indicate loft area ──
            // Draw diagonal lines across the loft area
            double spacing = 2.0; // 2' diagonal line spacing
            for (double d = 0; d < floor.Width + floor.Height; d += spacing)
            {
                double x1 = floor.X + Math.Min(d, floor.Width);
                double y1 = floor.Y + Math.Max(0, d - floor.Width);
                double x2 = floor.X + Math.Max(0, d - floor.Height);
                double y2 = floor.Y + Math.Min(d, floor.Height);

                var hatchLine = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(x1, y1, offset),
                    DrawingHelpers.Offset(x2, y2, offset),
                    LayerManager.Layers.Loft);
                count++;
            }

            // ── Floor joists ──
            foreach (var joist in loftGeo.JoistLines)
            {
                var joistLine = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(joist.StartX, joist.StartY, offset),
                    DrawingHelpers.Offset(joist.EndX, joist.EndY, offset),
                    LayerManager.Layers.Loft);
                try { joistLine.Linetype = "DASHED"; } catch { }
                count++;
            }

            // ── Access stairs/ladder ──
            var access = loftGeo.AccessFootprint;
            if (access != null)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(access.X, access.Y, offset),
                    access.Width, access.Height,
                    LayerManager.Layers.Loft);
                count++;

                string accessLabel = loftGeo.AccessType switch
                {
                    LoftAccess.Stairs => "STAIRS UP",
                    LoftAccess.ShipLadder => "SHIP LADDER",
                    _ => "LADDER"
                };

                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(access.X + access.Width / 2.0,
                        access.Y + access.Height / 2.0, offset),
                    accessLabel, 0.5, LayerManager.Layers.Anno);
                count++;

                // Direction arrow for stairs
                if (loftGeo.AccessType == LoftAccess.Stairs)
                {
                    DrawingHelpers.AddLine(tr, btr,
                        DrawingHelpers.Offset(access.X + access.Width / 2.0, access.Y, offset),
                        DrawingHelpers.Offset(access.X + access.Width / 2.0, access.Y + access.Height, offset),
                        LayerManager.Layers.Loft);
                    count++;

                    // Arrow head
                    double ax = access.X + access.Width / 2.0;
                    double ay = access.Y + access.Height;
                    DrawingHelpers.AddLine(tr, btr,
                        DrawingHelpers.Offset(ax, ay, offset),
                        DrawingHelpers.Offset(ax - 0.3, ay - 0.5, offset),
                        LayerManager.Layers.Loft);
                    DrawingHelpers.AddLine(tr, btr,
                        DrawingHelpers.Offset(ax, ay, offset),
                        DrawingHelpers.Offset(ax + 0.3, ay - 0.5, offset),
                        LayerManager.Layers.Loft);
                    count += 2;
                }
            }

            // ── Guardrail lines at open edges ──
            // Front edge (open side facing aisle)
            var railLine = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(floor.X, floor.Y, offset),
                DrawingHelpers.Offset(floor.X + floor.Width, floor.Y, offset),
                LayerManager.Layers.Loft);
            count++;

            // ── Loft label ──
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(floor.X + floor.Width / 2.0,
                    floor.Y + floor.Height / 2.0 + 1.0, offset),
                $"LOFT ({loftGeo.FloorHeight:F0}' AFF)", 0.75, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        // ════════════════════════════════════════════════════
        // Interior Partitions
        // ════════════════════════════════════════════════════

        public static int GeneratePartitions(Transaction tr, BlockTableRecord btr,
            BarnGeometry mainGeo, List<PartitionGeometryData> partitions, Vector3d offset)
        {
            if (partitions == null || partitions.Count == 0) return 0;
            int count = 0;

            foreach (var partition in partitions)
            {
                // Line style varies by partition type
                string linetype = partition.Type switch
                {
                    PartitionType.ChainLink => "DASHED",
                    PartitionType.PipePanel => "DASHED",
                    PartitionType.BoardFence => "DASHED",
                    _ => "ByLayer" // Solid for FrameWall and ConcreteBlock
                };

                // Draw partition line
                var line = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(partition.StartPoint.X, partition.StartPoint.Y, offset),
                    DrawingHelpers.Offset(partition.EndPoint.X, partition.EndPoint.Y, offset),
                    LayerManager.Layers.Partitions);
                try { if (linetype != "ByLayer") line.Linetype = linetype; } catch { }
                count++;

                // Double line for frame walls and concrete block (show wall thickness)
                if (partition.Type == PartitionType.FrameWall || partition.Type == PartitionType.ConcreteBlock)
                {
                    double thickness = partition.Type == PartitionType.ConcreteBlock ? 8.0 / 12.0 : 4.5 / 12.0;
                    double dx = partition.EndPoint.X - partition.StartPoint.X;
                    double dy = partition.EndPoint.Y - partition.StartPoint.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 0)
                    {
                        double nx = -dy / len * thickness / 2.0;
                        double ny = dx / len * thickness / 2.0;

                        DrawingHelpers.AddLine(tr, btr,
                            DrawingHelpers.Offset(partition.StartPoint.X + nx, partition.StartPoint.Y + ny, offset),
                            DrawingHelpers.Offset(partition.EndPoint.X + nx, partition.EndPoint.Y + ny, offset),
                            LayerManager.Layers.Partitions);
                        DrawingHelpers.AddLine(tr, btr,
                            DrawingHelpers.Offset(partition.StartPoint.X - nx, partition.StartPoint.Y - ny, offset),
                            DrawingHelpers.Offset(partition.EndPoint.X - nx, partition.EndPoint.Y - ny, offset),
                            LayerManager.Layers.Partitions);
                        count += 2;
                    }
                }

                // Door opening (gap in partition line)
                if (partition.HasDoor && partition.DoorWidth > 0)
                {
                    // Draw door swing arc
                    Arc doorArc = new Arc(
                        DrawingHelpers.Offset(partition.DoorPosition.X, partition.DoorPosition.Y, offset),
                        partition.DoorWidth,
                        0, Math.PI / 2.0);
                    LayerManager.SetLayer(doorArc, LayerManager.Layers.Doors);
                    btr.AppendEntity(doorArc);
                    tr.AddNewlyCreatedDBObject(doorArc, true);
                    count++;
                }

                // Label
                if (!string.IsNullOrEmpty(partition.Label))
                {
                    double midX = (partition.StartPoint.X + partition.EndPoint.X) / 2.0;
                    double midY = (partition.StartPoint.Y + partition.EndPoint.Y) / 2.0;

                    DrawingHelpers.AddText(tr, btr,
                        DrawingHelpers.Offset(midX, midY + 0.5, offset),
                        partition.Label, 0.5, LayerManager.Layers.Anno);
                    count++;
                }

                // Material callout for load-bearing partitions
                if (partition.IsLoadBearing)
                {
                    double midX = (partition.StartPoint.X + partition.EndPoint.X) / 2.0;
                    double midY = (partition.StartPoint.Y + partition.EndPoint.Y) / 2.0;

                    string material = partition.Type switch
                    {
                        PartitionType.FrameWall => "2x4 FRAMED (LB)",
                        PartitionType.ConcreteBlock => "8\" CMU (LB)",
                        _ => "(LOAD BEARING)"
                    };

                    DrawingHelpers.AddText(tr, btr,
                        DrawingHelpers.Offset(midX, midY - 0.5, offset),
                        material, 0.35, LayerManager.Layers.Anno);
                    count++;
                }
            }

            return count;
        }

        // ════════════════════════════════════════════════════
        // Workshop Features
        // ════════════════════════════════════════════════════

        public static int GenerateWorkshopFeatures(Transaction tr, BlockTableRecord btr,
            BarnGeometry mainGeo, List<WorkshopFeatureGeometry> features, Vector3d offset)
        {
            if (features == null || features.Count == 0) return 0;
            int count = 0;

            foreach (var feature in features)
            {
                switch (feature.FeatureType)
                {
                    case WorkshopFeatureType.Workbench:
                        count += DrawWorkbench(tr, btr, feature, offset);
                        break;

                    case WorkshopFeatureType.Compressor:
                        count += DrawEquipment(tr, btr, feature, offset);
                        break;

                    case WorkshopFeatureType.ToolStorage:
                        count += DrawEquipment(tr, btr, feature, offset);
                        break;

                    case WorkshopFeatureType.PowerOutlet:
                        count += DrawPowerOutlet(tr, btr, feature, offset);
                        break;

                    case WorkshopFeatureType.FloorDrain:
                        AddFloorDrainSymbol(tr, btr, feature.Position.X, feature.Position.Y, offset);
                        count++;
                        break;

                    case WorkshopFeatureType.OverheadCrane:
                        count += DrawOverheadCrane(tr, btr, feature, offset);
                        break;
                }
            }

            return count;
        }

        public static int GenerateDairyLayout(Transaction tr, BlockTableRecord btr,
            BarnGeometry mainGeo, DairyLayoutData layout, Vector3d offset)
        {
            if (layout == null) return 0;
            int count = 0;

            if (layout.FeedAlley != null)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(layout.FeedAlley.X, layout.FeedAlley.Y, offset),
                    layout.FeedAlley.Width, layout.FeedAlley.Height, LayerManager.Layers.Dairy);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(layout.FeedAlley.X + layout.FeedAlley.Width / 2.0, layout.FeedAlley.Y + layout.FeedAlley.Height / 2.0, offset),
                    "FEED ALLEY / BUNK LINE", 0.45, LayerManager.Layers.Dairy);
                count += 2;
            }

            if (layout.ManureAlley != null)
            {
                var manure = DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(layout.ManureAlley.X, layout.ManureAlley.Y, offset),
                    layout.ManureAlley.Width, layout.ManureAlley.Height, LayerManager.Layers.Dairy);
                try { manure.Linetype = "DASHED"; } catch { }
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(layout.ManureAlley.X + layout.ManureAlley.Width / 2.0, layout.ManureAlley.Y + layout.ManureAlley.Height / 2.0, offset),
                    "MANURE ALLEY / GUTTER", 0.4, LayerManager.Layers.Dairy);
                count += 2;
            }

            if (layout.MilkingParlor != null)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(layout.MilkingParlor.X, layout.MilkingParlor.Y, offset),
                    layout.MilkingParlor.Width, layout.MilkingParlor.Height, LayerManager.Layers.Dairy);
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(layout.MilkingParlor.X + layout.MilkingParlor.Width / 2.0, layout.MilkingParlor.Y + layout.MilkingParlor.Height / 2.0, offset),
                    $"{layout.ParlorType.ToString().ToUpper()} PARLOR", 0.5, LayerManager.Layers.Anno);
                count += 2;
            }

            foreach (var stall in layout.Freestalls)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(stall.X, stall.Y, offset),
                    stall.Width, stall.Height, LayerManager.Layers.Dairy);
                count++;
            }

            foreach (var path in layout.TrafficPaths)
            {
                var flow = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(path.StartX, path.StartY, offset),
                    DrawingHelpers.Offset(path.EndX, path.EndY, offset),
                    LayerManager.Layers.Dairy);
                try { flow.Linetype = "CENTER"; } catch { }
                count++;
            }

            return count;
        }

        public static int GenerateEquipmentStorageLayout(Transaction tr, BlockTableRecord btr,
            EquipmentStorageLayoutData layout, Vector3d offset)
        {
            if (layout == null) return 0;
            int count = 0;

            if (layout.ClearanceZone != null)
            {
                var zone = DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(layout.ClearanceZone.X, layout.ClearanceZone.Y, offset),
                    layout.ClearanceZone.Width, layout.ClearanceZone.Height, LayerManager.Layers.Equip);
                try { zone.Linetype = "DASHED"; } catch { }
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(layout.ClearanceZone.X + layout.ClearanceZone.Width / 2.0, layout.ClearanceZone.Y + layout.ClearanceZone.Height / 2.0, offset),
                    "EQUIPMENT CLEARANCE ZONE", 0.4, LayerManager.Layers.Equip);
                count += 2;
            }

            if (layout.CraneRailLeft != null)
            {
                var left = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(layout.CraneRailLeft.StartX, layout.CraneRailLeft.StartY, offset),
                    DrawingHelpers.Offset(layout.CraneRailLeft.EndX, layout.CraneRailLeft.EndY, offset),
                    LayerManager.Layers.Crane);
                try { left.Linetype = "DASHED"; } catch { }
                count++;
            }

            if (layout.CraneRailRight != null)
            {
                var right = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(layout.CraneRailRight.StartX, layout.CraneRailRight.StartY, offset),
                    DrawingHelpers.Offset(layout.CraneRailRight.EndX, layout.CraneRailRight.EndY, offset),
                    LayerManager.Layers.Crane);
                try { right.Linetype = "DASHED"; } catch { }
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(layout.CraneRailRight.StartX - 2.0, 1.0, offset),
                    $"CRANE RAIL {layout.CraneCapacityTons:F1}T @ {layout.CraneRailHeight:F1}'", 0.4, LayerManager.Layers.Crane);
                count += 2;
            }

            if (!string.IsNullOrWhiteSpace(layout.SlabSpec))
            {
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(2.0, 2.0, offset),
                    $"SLAB: {layout.SlabSpec}", 0.35, LayerManager.Layers.Equip);
                count++;
            }

            if (layout.IsClearSpan)
            {
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(2.0, 3.0, offset),
                    "CLEAR-SPAN MODE (NO INTERIOR POSTS)", 0.35, LayerManager.Layers.Equip);
                count++;
            }

            return count;
        }

        public static int GenerateDrainageLayout(Transaction tr, BlockTableRecord btr,
            DrainageLayoutData layout, Vector3d offset)
        {
            if (layout == null) return 0;
            int count = 0;

            foreach (var drain in layout.DrainLocations)
            {
                AddFloorDrainSymbol(tr, btr, drain.X, drain.Y, offset);
                count++;
            }

            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(1.5, 1.0, offset),
                $"FLOOR SLOPE {layout.FloorSlopePercent:F1}% TO DRAINS", 0.35, LayerManager.Layers.Drain);
            count++;

            if (layout.FrenchDrainEnabled)
            {
                var french = DrawingHelpers.AddLine(tr, btr,
                    DrawingHelpers.Offset(0.5, 0.5, offset),
                    DrawingHelpers.Offset(0.5, 10.0, offset),
                    LayerManager.Layers.Drain);
                try { french.Linetype = "DASHED"; } catch { }
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(1.2, 9.5, offset),
                    "FRENCH DRAIN", 0.3, LayerManager.Layers.Drain);
                count += 2;
            }

            return count;
        }

        public static int GenerateGrainStorageLayout(Transaction tr, BlockTableRecord btr,
            GrainStorageLayoutData layout, Vector3d offset)
        {
            if (layout == null) return 0;
            int count = 0;

            foreach (var pad in layout.BinPads)
            {
                Circle bin = new Circle(
                    DrawingHelpers.Offset(pad.Center.X, pad.Center.Y, offset),
                    Vector3d.ZAxis, pad.Diameter / 2.0);
                LayerManager.SetLayer(bin, LayerManager.Layers.Grain);
                btr.AppendEntity(bin);
                tr.AddNewlyCreatedDBObject(bin, true);
                count++;
            }

            if (layout.AerationFloorEnabled)
            {
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(2.0, 1.5, offset),
                    "AERATION FLOOR", 0.35, LayerManager.Layers.Grain);
                count++;
            }

            if (layout.FlatStorageEnabled)
            {
                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(2.0, 2.2, offset),
                    "FLAT STORAGE ZONE", 0.35, LayerManager.Layers.Grain);
                count++;
            }

            return count;
        }

        public static int GenerateMachineryLayout(Transaction tr, BlockTableRecord btr,
            MachineryLayoutData layout, Vector3d offset)
        {
            if (layout == null) return 0;
            int count = 0;

            foreach (var bay in layout.ClearSpanBays)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(bay.X, bay.Y, offset),
                    bay.Width, bay.Height, LayerManager.Layers.Equip);
                count++;
            }

            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(2.0, 3.0, offset),
                $"MACHINERY EAVE TARGET {layout.PreferredEaveHeight:F1}'", 0.35, LayerManager.Layers.Equip);
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(2.0, 3.7, offset),
                $"RECOMMENDED EQUIPMENT DOOR HEIGHT {layout.RecommendedDoorHeight:F1}'", 0.35, LayerManager.Layers.Equip);
            count += 2;

            return count;
        }

        // ── Private helper methods ──

        private static int DrawWorkbench(Transaction tr, BlockTableRecord btr,
            WorkshopFeatureGeometry feature, Vector3d offset)
        {
            int count = 0;
            var o = feature.Outline;

            // Workbench outline (filled rectangle)
            DrawingHelpers.AddFilledRect(tr, btr,
                DrawingHelpers.Offset2d(o.X + o.Width / 2.0, o.Y + o.Height / 2.0, offset),
                o.Width, o.Height,
                LayerManager.Layers.Workshop);
            count++;

            // Outline
            DrawingHelpers.AddRectangle(tr, btr,
                DrawingHelpers.Offset2d(o.X, o.Y, offset),
                o.Width, o.Height,
                LayerManager.Layers.Workshop);
            count++;

            // Label
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(o.X + o.Width / 2.0, o.Y + o.Height / 2.0, offset),
                feature.Label, 0.5, LayerManager.Layers.Anno);
            count++;

            // Vise symbol (small rectangle at edge)
            if (feature.HasVise)
            {
                DrawingHelpers.AddRectangle(tr, btr,
                    DrawingHelpers.Offset2d(feature.VisePosition.X - 0.25, feature.VisePosition.Y - 0.25, offset),
                    0.5, 0.5,
                    LayerManager.Layers.Workshop);
                count++;

                DrawingHelpers.AddText(tr, btr,
                    DrawingHelpers.Offset(feature.VisePosition.X, feature.VisePosition.Y - 0.5, offset),
                    "VISE", 0.25, LayerManager.Layers.Anno);
                count++;
            }

            return count;
        }

        private static int DrawEquipment(Transaction tr, BlockTableRecord btr,
            WorkshopFeatureGeometry feature, Vector3d offset)
        {
            int count = 0;
            var o = feature.Outline;

            DrawingHelpers.AddRectangle(tr, btr,
                DrawingHelpers.Offset2d(o.X, o.Y, offset),
                o.Width, o.Height,
                LayerManager.Layers.Workshop);
            count++;

            // Cross-hatch (X) inside for equipment
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(o.X, o.Y, offset),
                DrawingHelpers.Offset(o.X + o.Width, o.Y + o.Height, offset),
                LayerManager.Layers.Workshop);
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(o.X + o.Width, o.Y, offset),
                DrawingHelpers.Offset(o.X, o.Y + o.Height, offset),
                LayerManager.Layers.Workshop);
            count += 2;

            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(o.X + o.Width / 2.0, o.Y - 0.5, offset),
                feature.Label, 0.35, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        private static int DrawPowerOutlet(Transaction tr, BlockTableRecord btr,
            WorkshopFeatureGeometry feature, Vector3d offset)
        {
            int count = 0;
            double x = feature.Position.X;
            double y = feature.Position.Y;

            // 110V = circle, 220V = square, Welding = diamond, Air = triangle
            switch (feature.PowerOutletType)
            {
                case PowerType.Standard110:
                    Circle c = new Circle(
                        DrawingHelpers.Offset(x, y, offset),
                        Vector3d.ZAxis, 0.3);
                    LayerManager.SetLayer(c, LayerManager.Layers.Workshop);
                    btr.AppendEntity(c);
                    tr.AddNewlyCreatedDBObject(c, true);
                    count++;
                    break;

                case PowerType.Heavy220:
                    DrawingHelpers.AddRectangle(tr, btr,
                        DrawingHelpers.Offset2d(x - 0.3, y - 0.3, offset),
                        0.6, 0.6, LayerManager.Layers.Workshop);
                    count++;
                    break;

                case PowerType.Welding:
                    // Diamond shape
                    var diamond = new List<Point2d>
                    {
                        DrawingHelpers.Offset2d(x, y + 0.4, offset),
                        DrawingHelpers.Offset2d(x + 0.4, y, offset),
                        DrawingHelpers.Offset2d(x, y - 0.4, offset),
                        DrawingHelpers.Offset2d(x - 0.4, y, offset)
                    };
                    DrawingHelpers.AddPolyline(tr, btr, diamond, LayerManager.Layers.Workshop, closed: true);
                    count++;
                    break;

                case PowerType.CompressedAir:
                    // Triangle
                    var tri = new List<Point2d>
                    {
                        DrawingHelpers.Offset2d(x, y + 0.4, offset),
                        DrawingHelpers.Offset2d(x + 0.35, y - 0.2, offset),
                        DrawingHelpers.Offset2d(x - 0.35, y - 0.2, offset)
                    };
                    DrawingHelpers.AddPolyline(tr, btr, tri, LayerManager.Layers.Workshop, closed: true);
                    count++;
                    break;
            }

            // Label
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(x, y - 0.6, offset),
                feature.Label + (feature.IsGFCI ? " GFCI" : ""),
                0.25, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        private static int DrawOverheadCrane(Transaction tr, BlockTableRecord btr,
            WorkshopFeatureGeometry feature, Vector3d offset)
        {
            int count = 0;
            var o = feature.Outline;

            // Crane rails (dashed lines along building length)
            var leftRail = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(o.X, o.Y, offset),
                DrawingHelpers.Offset(o.X, o.Y + o.Height, offset),
                LayerManager.Layers.Workshop);
            try { leftRail.Linetype = "DASHED"; } catch { }
            count++;

            var rightRail = DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(o.X + o.Width, o.Y, offset),
                DrawingHelpers.Offset(o.X + o.Width, o.Y + o.Height, offset),
                LayerManager.Layers.Workshop);
            try { rightRail.Linetype = "DASHED"; } catch { }
            count++;

            // Bridge beam (center, perpendicular)
            double bridgeY = o.Y + o.Height / 2.0;
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(o.X, bridgeY, offset),
                DrawingHelpers.Offset(o.X + o.Width, bridgeY, offset),
                LayerManager.Layers.Workshop);
            count++;

            // Hoist symbol (circle at center)
            Circle hoist = new Circle(
                DrawingHelpers.Offset(o.X + o.Width / 2.0, bridgeY, offset),
                Vector3d.ZAxis, 0.5);
            LayerManager.SetLayer(hoist, LayerManager.Layers.Workshop);
            btr.AppendEntity(hoist);
            tr.AddNewlyCreatedDBObject(hoist, true);
            count++;

            // Label
            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(o.X + o.Width / 2.0, o.Y - 1.0, offset),
                feature.Label, 0.5, LayerManager.Layers.Anno);
            count++;

            return count;
        }

        private static void AddFloorDrainSymbol(Transaction tr, BlockTableRecord btr,
            double x, double y, Vector3d offset)
        {
            // Floor drain: circle with cross
            Circle drain = new Circle(
                DrawingHelpers.Offset(x, y, offset),
                Vector3d.ZAxis, 0.4);
            LayerManager.SetLayer(drain, LayerManager.Layers.Workshop);
            btr.AppendEntity(drain);
            tr.AddNewlyCreatedDBObject(drain, true);

            // Cross inside
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(x - 0.3, y, offset),
                DrawingHelpers.Offset(x + 0.3, y, offset),
                LayerManager.Layers.Workshop);
            DrawingHelpers.AddLine(tr, btr,
                DrawingHelpers.Offset(x, y - 0.3, offset),
                DrawingHelpers.Offset(x, y + 0.3, offset),
                LayerManager.Layers.Workshop);

            DrawingHelpers.AddText(tr, btr,
                DrawingHelpers.Offset(x, y - 0.7, offset),
                "FD", 0.25, LayerManager.Layers.Anno);
        }
    }
}
