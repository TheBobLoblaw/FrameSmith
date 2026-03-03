using System;
using System.Collections.Generic;
using System.Linq;
using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.Utils
{
    /// <summary>
    /// Validates opening placements for conflicts, clearances, and structural interference.
    /// </summary>
    public static class OpeningValidator
    {
        /// <summary>
        /// Minimum clearance from opening edge to wall corner in feet.
        /// </summary>
        private const double MinEdgeClearance = 0.5; // 6 inches

        /// <summary>
        /// Minimum clearance between adjacent openings in feet.
        /// </summary>
        private const double MinOpeningSpacing = 0.5; // 6 inches

        /// <summary>
        /// Validates all openings in the barn parameters for conflicts and issues.
        /// </summary>
        /// <param name="parameters">The barn parameters to validate</param>
        /// <returns>List of human-readable error messages (empty if no issues)</returns>
        public static List<string> ValidateOpenings(BarnParameters parameters)
        {
            var errors = new List<string>();
            const int MaxErrors = 20; // Cap error count for performance

            // Build a unified list of openings with their wall positions
            var allOpenings = new List<OpeningRect>();

            for (int i = 0; i < parameters.Doors.Count; i++)
            {
                var d = parameters.Doors[i];
                allOpenings.Add(new OpeningRect
                {
                    Label = $"Door #{i + 1} ({d.Type}, {d.Width}'x{d.Height}')",
                    Wall = d.Wall,
                    LeftEdge = d.CenterOffset - d.Width / 2.0,
                    RightEdge = d.CenterOffset + d.Width / 2.0,
                    BottomEdge = 0,
                    TopEdge = d.Height,
                    IsDoor = true,
                    DoorType = d.Type,
                    Width = d.Width
                });
            }

            for (int i = 0; i < parameters.Windows.Count; i++)
            {
                var w = parameters.Windows[i];
                allOpenings.Add(new OpeningRect
                {
                    Label = $"Window #{i + 1} ({w.Type}, {w.Width}'x{w.Height}')",
                    Wall = w.Wall,
                    LeftEdge = w.CenterOffset - w.Width / 2.0,
                    RightEdge = w.CenterOffset + w.Width / 2.0,
                    BottomEdge = w.SillHeight,
                    TopEdge = w.SillHeight + w.Height,
                    IsDoor = false
                });
            }

            // Group by wall
            var wallGroups = allOpenings.GroupBy(o => o.Wall);

            foreach (var group in wallGroups)
            {
                var wall = group.Key;
                double wallLength = (wall == WallSide.Front || wall == WallSide.Back)
                    ? parameters.BuildingWidth
                    : parameters.BuildingLength;

                var openings = group.OrderBy(o => o.LeftEdge).ToList();

                foreach (var opening in openings)
                {
                    // Check wall edge clearances
                    if (opening.LeftEdge < MinEdgeClearance)
                        errors.Add($"{opening.Label} on {wall} wall: left edge is only {opening.LeftEdge:F1}' from wall corner (min {MinEdgeClearance}').");

                    if (opening.RightEdge > wallLength - MinEdgeClearance)
                        errors.Add($"{opening.Label} on {wall} wall: right edge extends to {opening.RightEdge:F1}' (wall is {wallLength}', min clearance {MinEdgeClearance}').");

                    if (errors.Count >= MaxErrors) break;

                    // Check sliding door clearances — need wall space equal to door width for slide
                    if (opening.IsDoor && opening.DoorType == DoorType.Sliding)
                    {
                        double slideSpace = opening.Width;
                        // Check if there's room to slide right
                        bool canSlideRight = opening.RightEdge + slideSpace <= wallLength - MinEdgeClearance;
                        // Check if there's room to slide left
                        bool canSlideLeft = opening.LeftEdge - slideSpace >= MinEdgeClearance;

                        if (!canSlideRight && !canSlideLeft)
                            errors.Add($"{opening.Label} on {wall} wall: insufficient wall space for sliding clearance (needs {slideSpace}' clear on at least one side).");
                    }
                }

                // Check for overlaps between openings on the same wall
                for (int i = 0; i < openings.Count && errors.Count < MaxErrors; i++)
                {
                    for (int j = i + 1; j < openings.Count && errors.Count < MaxErrors; j++)
                    {
                        var a = openings[i];
                        var b = openings[j];

                        // Horizontal overlap check
                        bool hOverlap = a.RightEdge + MinOpeningSpacing > b.LeftEdge
                                     && a.LeftEdge < b.RightEdge + MinOpeningSpacing;

                        // Vertical overlap check (openings can be at different heights)
                        bool vOverlap = a.TopEdge > b.BottomEdge && a.BottomEdge < b.TopEdge;

                        if (hOverlap && vOverlap)
                            errors.Add($"{a.Label} and {b.Label} on {wall} wall overlap or are too close (min spacing {MinOpeningSpacing}').");
                    }
                }

                // Check post interference
                var postPositions = GetPostPositions(wall, parameters);
                double postHalfWidth = (parameters.PostWidthInches / 12.0) / 2.0;

                foreach (var opening in openings)
                {
                    foreach (double postCenter in postPositions)
                    {
                        double postLeft = postCenter - postHalfWidth;
                        double postRight = postCenter + postHalfWidth;

                        // Check if opening overlaps with post
                        if (opening.LeftEdge < postRight && opening.RightEdge > postLeft)
                            errors.Add($"{opening.Label} on {wall} wall conflicts with structural post at {postCenter:F1}'.");
                    }
                }
            }

            return errors;
        }

        /// <summary>
        /// Gets the center positions of structural posts along a wall in feet.
        /// </summary>
        private static List<double> GetPostPositions(WallSide wall, BarnParameters parameters)
        {
            var positions = new List<double>();

            if (wall == WallSide.Front || wall == WallSide.Back)
            {
                // Endwalls: posts at corners (0, width) and potentially intermediate
                // For endwalls, posts are at X=0 and X=BuildingWidth at minimum
                positions.Add(0);
                positions.Add(parameters.BuildingWidth);
                // Center post if wide enough (typically at ridge for endwalls)
                if (parameters.BuildingWidth > 20)
                    positions.Add(parameters.BuildingWidth / 2.0);
            }
            else
            {
                // Sidewalls: posts at each bay spacing
                double spacing = parameters.ActualBaySpacing;
                int bays = parameters.NumberOfBays;
                for (int i = 0; i <= bays; i++)
                {
                    positions.Add(i * spacing);
                }
            }

            return positions;
        }

        /// <summary>
        /// Internal representation of an opening's position on a wall.
        /// </summary>
        private class OpeningRect
        {
            public string Label { get; set; }
            public WallSide Wall { get; set; }
            public double LeftEdge { get; set; }
            public double RightEdge { get; set; }
            public double BottomEdge { get; set; }
            public double TopEdge { get; set; }
            public bool IsDoor { get; set; }
            public DoorType DoorType { get; set; }
            public double Width { get; set; }
        }
    }
}
