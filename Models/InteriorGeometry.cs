using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;

namespace PoleBarnGenerator.Models
{
    /// <summary>
    /// Computed interior geometry from parameters. Bridge between interior
    /// parameters and the InteriorGenerator drawing code.
    /// </summary>
    public class InteriorGeometry
    {
        public StallLayout StallLayout { get; set; }
        public LoftGeometryData LoftGeometry { get; set; }
        public List<PartitionGeometryData> PartitionGeometries { get; set; } = new();
        public List<WorkshopFeatureGeometry> WorkshopFeatures { get; set; } = new();

        /// <summary>
        /// Calculate all interior geometry from building geometry and interior parameters.
        /// </summary>
        public static InteriorGeometry Calculate(BarnGeometry mainGeo,
            HorseStallParameters stalls, LoftParameters loft,
            InteriorPartitionParameters partitions, WorkshopParameters workshop)
        {
            var result = new InteriorGeometry();
            var p = mainGeo.Params;

            // ── Horse Stalls ──
            if (stalls?.IsEnabled == true)
            {
                result.StallLayout = CalculateStallLayout(mainGeo, stalls);
            }

            // ── Loft ──
            if (loft?.IsEnabled == true)
            {
                result.LoftGeometry = CalculateLoftGeometry(mainGeo, loft);
            }

            // ── Partitions ──
            if (partitions?.IsEnabled == true)
            {
                result.PartitionGeometries = CalculatePartitionGeometries(mainGeo, partitions);
            }

            // ── Workshop ──
            if (workshop?.IsEnabled == true)
            {
                result.WorkshopFeatures = CalculateWorkshopGeometries(mainGeo, workshop);
            }

            return result;
        }

        private static StallLayout CalculateStallLayout(BarnGeometry geo, HorseStallParameters stalls)
        {
            var layout = new StallLayout();
            var p = geo.Params;

            double buildingCenterX = p.BuildingWidth / 2.0;
            double aisleHalf = stalls.AisleWidth / 2.0;
            double aisleLeft = buildingCenterX - aisleHalf;
            double aisleRight = buildingCenterX + aisleHalf;

            // Center aisle rectangle
            layout.CenterAisle = new RectangleData
            {
                X = aisleLeft,
                Y = 0,
                Width = stalls.AisleWidth,
                Height = p.BuildingLength
            };

            // Calculate stall positions
            int stallsPerSide = stalls.StallSide == StallSide.Both
                ? (stalls.NumberOfStalls + 1) / 2
                : stalls.NumberOfStalls;

            double stallWidth = stalls.StallWidth;
            double stallDepth = stalls.StallDepth;

            // Left side stalls
            if (stalls.StallSide == StallSide.Left || stalls.StallSide == StallSide.Both)
            {
                double stallStartX = aisleLeft - stallDepth;
                for (int i = 0; i < stallsPerSide; i++)
                {
                    double stallY = i * stallWidth;
                    if (stallY + stallWidth > p.BuildingLength) break;

                    layout.StallOutlines.Add(new RectangleData
                    {
                        X = stallStartX,
                        Y = stallY,
                        Width = stallDepth,
                        Height = stallWidth
                    });

                    // Gate at aisle-side of stall
                    layout.StallGates.Add(new Point2d(aisleLeft, stallY + stallWidth / 2.0));

                    // Fixture positions (inside stall, near front corners)
                    layout.WaterLocations.Add(new Point2d(stallStartX + 0.5, stallY + 0.5));
                    layout.FeedLocations.Add(new Point2d(stallStartX + 0.5, stallY + stallWidth - 0.5));
                }
            }

            // Right side stalls
            if (stalls.StallSide == StallSide.Right || stalls.StallSide == StallSide.Both)
            {
                int rightCount = stalls.StallSide == StallSide.Both
                    ? stalls.NumberOfStalls / 2
                    : stalls.NumberOfStalls;

                for (int i = 0; i < rightCount; i++)
                {
                    double stallY = i * stallWidth;
                    if (stallY + stallWidth > p.BuildingLength) break;

                    layout.StallOutlines.Add(new RectangleData
                    {
                        X = aisleRight,
                        Y = stallY,
                        Width = stallDepth,
                        Height = stallWidth
                    });

                    layout.StallGates.Add(new Point2d(aisleRight, stallY + stallWidth / 2.0));
                    layout.WaterLocations.Add(new Point2d(aisleRight + stallDepth - 0.5, stallY + 0.5));
                    layout.FeedLocations.Add(new Point2d(aisleRight + stallDepth - 0.5, stallY + stallWidth - 0.5));
                }
            }

            // Specialty rooms
            if (stalls.TackRoom.IsEnabled)
            {
                double tackX = stalls.TackRoom.Side == TackRoomSide.Left
                    ? aisleLeft - stallDepth
                    : aisleRight;
                double tackY = stalls.TackRoom.BayStart * geo.ActualBaySpacing;
                double tackLength = stalls.TackRoom.BaySpan * geo.ActualBaySpacing;

                layout.SpecialtyRooms.Add(new SpecialtyRoomData
                {
                    Name = "TACK ROOM",
                    Outline = new RectangleData { X = tackX, Y = tackY, Width = stallDepth, Height = tackLength }
                });
            }

            if (stalls.FeedRoom.IsEnabled)
            {
                double feedX = stalls.FeedRoom.Side == FeedRoomSide.Left
                    ? aisleLeft - stallDepth
                    : aisleRight;
                double feedY = stalls.FeedRoom.BayStart * geo.ActualBaySpacing;
                double feedLength = stalls.FeedRoom.BaySpan * geo.ActualBaySpacing;

                layout.SpecialtyRooms.Add(new SpecialtyRoomData
                {
                    Name = "FEED ROOM",
                    Outline = new RectangleData { X = feedX, Y = feedY, Width = stallDepth, Height = feedLength }
                });
            }

            if (stalls.WashBay.IsEnabled)
            {
                double washX = aisleRight; // wash bays typically on right
                double washY = stalls.WashBay.BayPosition * geo.ActualBaySpacing;
                double washLength = geo.ActualBaySpacing;

                layout.SpecialtyRooms.Add(new SpecialtyRoomData
                {
                    Name = "WASH BAY",
                    Outline = new RectangleData { X = washX, Y = washY, Width = stallDepth, Height = washLength },
                    HasDrain = stalls.WashBay.HasDrain
                });
            }

            return layout;
        }

        private static LoftGeometryData CalculateLoftGeometry(BarnGeometry geo, LoftParameters loft)
        {
            var data = new LoftGeometryData();
            var p = geo.Params;

            // Determine loft span from bay positions
            var bayPositions = geo.BayPositions;
            List<int> spanBays = loft.SpanBays.Count > 0
                ? loft.SpanBays
                : Enumerable.Range(0, geo.NumBays).ToList();

            if (spanBays.Count == 0) return data;

            int minBay = spanBays.Min();
            int maxBay = spanBays.Max();

            double startY = minBay < bayPositions.Count ? bayPositions[minBay] : 0;
            double endY = maxBay + 1 < bayPositions.Count ? bayPositions[maxBay + 1] : p.BuildingLength;

            double loftWidth = loft.Depth == LoftDepth.FullWidth
                ? p.BuildingWidth
                : p.BuildingWidth / 2.0;

            data.FloorOutline = new RectangleData
            {
                X = 0,
                Y = startY,
                Width = loftWidth,
                Height = endY - startY
            };

            data.FloorHeight = loft.FloorHeight;
            data.RailingHeight = loft.RailingHeight;

            // Calculate joist positions (perpendicular to length, across width)
            if (loft.HasFloorJoists)
            {
                double joistSpacingFt = loft.JoistSpacing / 12.0;
                for (double y = startY; y <= endY; y += joistSpacingFt)
                {
                    data.JoistLines.Add(new LineData
                    {
                        StartX = 0, StartY = y,
                        EndX = loftWidth, EndY = y
                    });
                }
            }

            // Access location
            double accessX = loft.AccessLocation.X != 0 ? loft.AccessLocation.X : 2.0;
            double accessY = loft.AccessLocation.Y != 0 ? loft.AccessLocation.Y : startY + 2.0;
            data.AccessPoint = new Point2d(accessX, accessY);
            data.AccessType = loft.AccessType;

            // Stair/ladder footprint
            switch (loft.AccessType)
            {
                case LoftAccess.Stairs:
                    data.AccessFootprint = new RectangleData { X = accessX, Y = accessY, Width = 3.0, Height = loft.FloorHeight }; // 3' wide, run = rise
                    break;
                case LoftAccess.ShipLadder:
                    data.AccessFootprint = new RectangleData { X = accessX, Y = accessY, Width = 2.0, Height = loft.FloorHeight * 0.6 };
                    break;
                default: // StraightLadder
                    data.AccessFootprint = new RectangleData { X = accessX, Y = accessY, Width = 1.5, Height = 1.0 };
                    break;
            }

            return data;
        }

        private static List<PartitionGeometryData> CalculatePartitionGeometries(
            BarnGeometry geo, InteriorPartitionParameters partitions)
        {
            var result = new List<PartitionGeometryData>();

            foreach (var partition in partitions.Partitions)
            {
                var pgd = new PartitionGeometryData
                {
                    StartPoint = partition.StartPoint,
                    EndPoint = partition.EndPoint,
                    Type = partition.Type,
                    Label = partition.Label,
                    HasDoor = partition.HasDoor,
                    IsLoadBearing = partition.IsLoadBearing
                };

                // Calculate door opening position (center of partition)
                if (partition.HasDoor && partition.Door != null)
                {
                    double midX = (partition.StartPoint.X + partition.EndPoint.X) / 2.0;
                    double midY = (partition.StartPoint.Y + partition.EndPoint.Y) / 2.0;
                    pgd.DoorPosition = new Point2d(midX, midY);
                    pgd.DoorWidth = partition.Door.Width;
                }

                result.Add(pgd);
            }

            return result;
        }

        private static List<WorkshopFeatureGeometry> CalculateWorkshopGeometries(
            BarnGeometry geo, WorkshopParameters workshop)
        {
            var result = new List<WorkshopFeatureGeometry>();
            var p = geo.Params;

            if (workshop.HasWorkbench)
            {
                var wb = workshop.WorkbenchLocation;
                double posX = wb.Position.X != 0 ? wb.Position.X : 1.0; // Default: against left wall
                double posY = wb.Position.Y != 0 ? wb.Position.Y : p.BuildingLength / 2.0 - wb.Length / 2.0;

                result.Add(new WorkshopFeatureGeometry
                {
                    FeatureType = WorkshopFeatureType.Workbench,
                    Label = "WORKBENCH",
                    Outline = new RectangleData { X = posX, Y = posY, Width = wb.Depth, Height = wb.Length },
                    HasVise = wb.HasVise,
                    VisePosition = new Point2d(posX + wb.Depth, posY + 1.0)
                });
            }

            if (workshop.HasCompressorArea)
            {
                double compX = workshop.CompressorLocation.X != 0 ? workshop.CompressorLocation.X : p.BuildingWidth - 4.0;
                double compY = workshop.CompressorLocation.Y != 0 ? workshop.CompressorLocation.Y : p.BuildingLength - 4.0;

                result.Add(new WorkshopFeatureGeometry
                {
                    FeatureType = WorkshopFeatureType.Compressor,
                    Label = "AIR COMP.",
                    Outline = new RectangleData { X = compX, Y = compY, Width = 3.0, Height = 3.0 }
                });
            }

            if (workshop.HasToolStorage)
            {
                result.Add(new WorkshopFeatureGeometry
                {
                    FeatureType = WorkshopFeatureType.ToolStorage,
                    Label = "TOOL STORAGE",
                    Outline = new RectangleData { X = 1.0, Y = 1.0, Width = 2.0, Height = 8.0 }
                });
            }

            // Power outlets
            foreach (var outlet in workshop.PowerOutlets)
            {
                result.Add(new WorkshopFeatureGeometry
                {
                    FeatureType = WorkshopFeatureType.PowerOutlet,
                    Label = outlet.Type switch
                    {
                        PowerType.Standard110 => "110V",
                        PowerType.Heavy220 => "220V",
                        PowerType.Welding => "WELD",
                        PowerType.CompressedAir => "AIR",
                        _ => "PWR"
                    },
                    Position = outlet.Position,
                    PowerOutletType = outlet.Type,
                    IsGFCI = outlet.IsGFCI
                });
            }

            // Floor drains
            if (workshop.HasFloorDrain)
            {
                var drainLocs = workshop.DrainLocations.Count > 0
                    ? workshop.DrainLocations
                    : new List<Point2d> { new Point2d(p.BuildingWidth / 2.0, p.BuildingLength / 2.0) };

                foreach (var drain in drainLocs)
                {
                    result.Add(new WorkshopFeatureGeometry
                    {
                        FeatureType = WorkshopFeatureType.FloorDrain,
                        Label = "FD",
                        Position = drain
                    });
                }
            }

            // Overhead crane
            if (workshop.HasOverheadCrane)
            {
                result.Add(new WorkshopFeatureGeometry
                {
                    FeatureType = WorkshopFeatureType.OverheadCrane,
                    Label = $"{workshop.CraneCapacity:F1}T CRANE",
                    Outline = new RectangleData
                    {
                        X = 2.0,
                        Y = 2.0,
                        Width = p.BuildingWidth - 4.0,
                        Height = p.BuildingLength - 4.0
                    }
                });
            }

            return result;
        }
    }

    // ── Supporting geometry data structures ──

    public class RectangleData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }

    public class LineData
    {
        public double StartX { get; set; }
        public double StartY { get; set; }
        public double EndX { get; set; }
        public double EndY { get; set; }
    }

    public class StallLayout
    {
        public List<RectangleData> StallOutlines { get; set; } = new();
        public List<Point2d> StallGates { get; set; } = new();
        public RectangleData CenterAisle { get; set; }
        public List<Point2d> WaterLocations { get; set; } = new();
        public List<Point2d> FeedLocations { get; set; } = new();
        public List<SpecialtyRoomData> SpecialtyRooms { get; set; } = new();
    }

    public class SpecialtyRoomData
    {
        public string Name { get; set; }
        public RectangleData Outline { get; set; }
        public bool HasDrain { get; set; } = false;
    }

    public class LoftGeometryData
    {
        public RectangleData FloorOutline { get; set; }
        public double FloorHeight { get; set; }
        public double RailingHeight { get; set; }
        public List<LineData> JoistLines { get; set; } = new();
        public Point2d AccessPoint { get; set; }
        public LoftAccess AccessType { get; set; }
        public RectangleData AccessFootprint { get; set; }
    }

    public class PartitionGeometryData
    {
        public Point2d StartPoint { get; set; }
        public Point2d EndPoint { get; set; }
        public PartitionType Type { get; set; }
        public string Label { get; set; }
        public bool HasDoor { get; set; }
        public Point2d DoorPosition { get; set; }
        public double DoorWidth { get; set; } = 3.0;
        public bool IsLoadBearing { get; set; }
    }

    public enum WorkshopFeatureType
    {
        Workbench,
        Compressor,
        ToolStorage,
        PowerOutlet,
        FloorDrain,
        OverheadCrane
    }

    public class WorkshopFeatureGeometry
    {
        public WorkshopFeatureType FeatureType { get; set; }
        public string Label { get; set; }
        public RectangleData Outline { get; set; }
        public Point2d Position { get; set; }
        public bool HasVise { get; set; }
        public Point2d VisePosition { get; set; }
        public PowerType PowerOutletType { get; set; }
        public bool IsGFCI { get; set; }
    }
}
