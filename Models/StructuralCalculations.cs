using System;

namespace PoleBarnGenerator.Models.StructuralCalculations
{
    /// <summary>
    /// Load type affecting header sizing requirements.
    /// </summary>
    public enum LoadType
    {
        /// <summary>Header supports roof load only (typical for pole barns)</summary>
        Roof,
        /// <summary>Header supports floor/loft load above</summary>
        Floor,
        /// <summary>Header in non-load-bearing partition</summary>
        NonBearing
    }

    /// <summary>
    /// Represents a structural header member.
    /// </summary>
    public class HeaderSize
    {
        /// <summary>Number of plies (e.g., 2 for doubled header)</summary>
        public int Plies { get; set; } = 2;

        /// <summary>Nominal width in inches (e.g., 2 for a 2x)</summary>
        public double NominalWidth { get; set; }

        /// <summary>Nominal depth in inches (e.g., 10 for a 2x10)</summary>
        public double NominalDepth { get; set; }

        /// <summary>Actual width in inches</summary>
        public double ActualWidth { get; set; }

        /// <summary>Actual depth in inches</summary>
        public double ActualDepth { get; set; }

        /// <summary>Whether this is an LVL (laminated veneer lumber) beam</summary>
        public bool IsLVL { get; set; } = false;

        /// <summary>Species/grade description</summary>
        public string Material { get; set; } = "DF";
    }

    /// <summary>
    /// Calculates structural header sizes based on span width and load type.
    /// Uses standard residential/agricultural header sizing tables.
    /// </summary>
    public static class HeaderSizing
    {
        /// <summary>
        /// Calculate the required header size for a given span and load type.
        /// </summary>
        /// <param name="spanWidthFeet">Opening span width in feet</param>
        /// <param name="loadType">Type of load the header must support</param>
        /// <returns>Calculated HeaderSize</returns>
        public static HeaderSize CalculateHeaderSize(double spanWidthFeet, LoadType loadType)
        {
            if (spanWidthFeet <= 0)
                throw new ArgumentException("Span width must be positive.", nameof(spanWidthFeet));

            // Non-bearing headers are simpler
            if (loadType == LoadType.NonBearing)
            {
                if (spanWidthFeet <= 6)
                    return MakeSawn(2, 6);
                if (spanWidthFeet <= 10)
                    return MakeSawn(2, 8);
                return MakeSawn(2, 10);
            }

            // Roof or Floor bearing headers — standard sizing table
            // These follow conventional header sizing for single-story pole barns
            if (spanWidthFeet <= 4)
                return MakeSawn(2, 6);
            if (spanWidthFeet <= 6)
                return MakeSawn(2, 8);
            if (spanWidthFeet <= 8)
                return MakeSawn(2, 10);
            if (spanWidthFeet <= 10)
                return MakeSawn(2, 12);
            if (spanWidthFeet <= 12)
            {
                return loadType == LoadType.Floor
                    ? MakeLVL(3.5, 11.875)
                    : MakeSawn(3, 12); // triple 2x12
            }
            if (spanWidthFeet <= 14)
            {
                // Doubled 2x12 or single LVL
                return loadType == LoadType.Floor
                    ? MakeLVL(3.5, 11.875)
                    : MakeSawn(3, 12); // triple 2x12
            }

            // 16'+ spans require LVL beams
            if (spanWidthFeet <= 20)
                return MakeLVL(3.5, 11.875);
            if (spanWidthFeet <= 24)
                return MakeLVL(5.25, 11.875);

            // Very large spans — double LVL
            return MakeLVL(7.0, 11.875);
        }

        /// <summary>
        /// Returns a human-readable description of a header size.
        /// </summary>
        /// <param name="header">The header size to describe</param>
        /// <returns>Description like "2x10 DF" or "3.5x11.875 LVL"</returns>
        public static string GetHeaderDescription(HeaderSize header)
        {
            if (header == null)
                return "Unknown";

            if (header.IsLVL)
                return $"{header.ActualWidth}x{header.ActualDepth} LVL";

            string prefix = header.Plies > 1 ? $"({header.Plies}) " : "";
            return $"{prefix}2x{(int)header.NominalDepth} {header.Material}";
        }

        private static HeaderSize MakeSawn(int plies, int nominalDepth)
        {
            return new HeaderSize
            {
                Plies = plies,
                NominalWidth = 2,
                NominalDepth = nominalDepth,
                ActualWidth = 1.5 * plies,
                ActualDepth = GetActualDepth(nominalDepth),
                IsLVL = false,
                Material = "DF"  // Douglas Fir #2
            };
        }

        private static HeaderSize MakeLVL(double actualWidth, double actualDepth)
        {
            return new HeaderSize
            {
                Plies = 1,
                NominalWidth = actualWidth,
                NominalDepth = actualDepth,
                ActualWidth = actualWidth,
                ActualDepth = actualDepth,
                IsLVL = true,
                Material = "LVL"
            };
        }

        /// <summary>
        /// Converts nominal lumber depth to actual depth.
        /// </summary>
        private static double GetActualDepth(int nominalDepth)
        {
            switch (nominalDepth)
            {
                case 4: return 3.5;
                case 6: return 5.5;
                case 8: return 7.25;
                case 10: return 9.25;
                case 12: return 11.25;
                default: return nominalDepth - 0.75;
            }
        }
    }
}
