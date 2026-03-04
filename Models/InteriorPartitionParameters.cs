using System.Collections.Generic;
using Autodesk.AutoCAD.Geometry;

namespace PoleBarnGenerator.Models
{
    public class InteriorPartitionParameters
    {
        public bool IsEnabled { get; set; } = false;
        public List<InteriorPartition> Partitions { get; set; } = new();

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            if (!IsEnabled) return (true, null);

            foreach (var partition in Partitions)
            {
                if (partition.Height <= 0 || partition.Height > barn.EaveHeight)
                    return (false, $"Partition '{partition.Name}' height ({partition.Height:F1}') must be between 0 and eave height ({barn.EaveHeight:F1}').");

                // Validate points are within building footprint
                if (partition.StartPoint.X < 0 || partition.StartPoint.X > barn.BuildingWidth ||
                    partition.StartPoint.Y < 0 || partition.StartPoint.Y > barn.BuildingLength)
                    return (false, $"Partition '{partition.Name}' start point is outside building footprint.");

                if (partition.EndPoint.X < 0 || partition.EndPoint.X > barn.BuildingWidth ||
                    partition.EndPoint.Y < 0 || partition.EndPoint.Y > barn.BuildingLength)
                    return (false, $"Partition '{partition.Name}' end point is outside building footprint.");
            }

            return (true, null);
        }
    }

    public class InteriorPartition
    {
        public string Name { get; set; } = "Partition";
        public Point2d StartPoint { get; set; }
        public Point2d EndPoint { get; set; }
        public double Height { get; set; } = 8.0;
        public PartitionType Type { get; set; } = PartitionType.FrameWall;
        public bool HasDoor { get; set; } = false;
        public DoorOpening Door { get; set; }
        public string Label { get; set; } = "";
        public bool IsLoadBearing { get; set; } = false;

        /// <summary>Length of the partition in feet</summary>
        public double Length => StartPoint.GetDistanceTo(EndPoint);
    }

    public enum PartitionType
    {
        FrameWall,
        ChainLink,
        PipePanel,
        BoardFence,
        ConcreteBlock
    }
}
