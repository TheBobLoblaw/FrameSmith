using System;
using System.Collections.Generic;
using System.Linq;

namespace PoleBarnGenerator.Models
{
    public enum FloorConnectionType
    {
        ContinuousPost,
        SplicedPost
    }

    /// <summary>
    /// Story-level configuration for multi-floor pole barn layouts.
    /// </summary>
    public class StoryParameters
    {
        private int _numberOfFloors = 1;
        private List<double> _floorHeights = new List<double>();
        private FloorConnectionType _floorConnection = FloorConnectionType.ContinuousPost;
        private string _floorBeamSize = "2-2x12 LVL";

        public int NumberOfFloors
        {
            get => _numberOfFloors;
            set => _numberOfFloors = Math.Max(1, value);
        }

        public List<double> FloorHeights
        {
            get => _floorHeights;
            set => _floorHeights = value ?? new List<double>();
        }

        public FloorConnectionType FloorConnection
        {
            get => _floorConnection;
            set => _floorConnection = value;
        }

        public string FloorBeamSize
        {
            get => _floorBeamSize;
            set => _floorBeamSize = string.IsNullOrWhiteSpace(value) ? "2-2x12 LVL" : value.Trim();
        }

        public List<double> ResolveHeights(double eaveHeight)
        {
            if (NumberOfFloors <= 1)
            {
                return new List<double> { eaveHeight };
            }

            if (FloorHeights != null && FloorHeights.Count == NumberOfFloors && FloorHeights.All(h => h > 0))
            {
                if (FloorHeights.Sum() <= eaveHeight + 0.01)
                {
                    return new List<double>(FloorHeights);
                }
            }

            double defaultHeight = eaveHeight / NumberOfFloors;
            return Enumerable.Repeat(defaultHeight, NumberOfFloors).ToList();
        }
    }
}
