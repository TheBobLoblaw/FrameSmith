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
        /// <param name="geometry">Computed geometry containing the authoritative post locations</param>
        /// <returns>List of human-readable error messages (empty if no issues)</returns>
        public static List<string> ValidateOpenings(BarnParameters parameters, BarnGeometry geometry)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (geometry == null) throw new ArgumentNullException(nameof(geometry));

            var postsByWall = new Dictionary<WallSide, List<double>>
            {
                [WallSide.Front] = GetPostPositions(WallSide.Front, parameters, geometry),
                [WallSide.Back] = GetPostPositions(WallSide.Back, parameters, geometry),
                [WallSide.Left] = GetPostPositions(WallSide.Left, parameters, geometry),
                [WallSide.Right] = GetPostPositions(WallSide.Right, parameters, geometry)
            };

            return ValidateOpenings(parameters, postsByWall);
        }

        /// <summary>
        /// Validates openings using precomputed post positions per wall.
        /// </summary>
        public static List<string> ValidateOpenings(BarnParameters parameters, IReadOnlyDictionary<WallSide, List<double>> postPositionsByWall)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            if (postPositionsByWall == null) throw new ArgumentNullException(nameof(postPositionsByWall));

            var errors = new List<string>();
            const int MaxErrors = 20;

            var allOpenings = BuildOpeningRects(parameters);
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
                    if (opening.LeftEdge < MinEdgeClearance)
                        errors.Add($"{opening.Label} on {wall} wall: left edge is only {opening.LeftEdge:F1}' from wall corner (min {MinEdgeClearance}').");

                    if (opening.RightEdge > wallLength - MinEdgeClearance)
                        errors.Add($"{opening.Label} on {wall} wall: right edge extends to {opening.RightEdge:F1}' (wall is {wallLength}', min clearance {MinEdgeClearance}').");

                    if (errors.Count >= MaxErrors) break;

                    if (opening.IsDoor && opening.DoorType == DoorType.Sliding)
                    {
                        double slideSpace = opening.Width;
                        bool canSlideRight = opening.RightEdge + slideSpace <= wallLength - MinEdgeClearance;
                        bool canSlideLeft = opening.LeftEdge - slideSpace >= MinEdgeClearance;

                        if (!canSlideRight && !canSlideLeft)
                            errors.Add($"{opening.Label} on {wall} wall: insufficient wall space for sliding clearance (needs {slideSpace}' clear on at least one side).");
                    }
                }

                for (int i = 0; i < openings.Count && errors.Count < MaxErrors; i++)
                {
                    for (int j = i + 1; j < openings.Count && errors.Count < MaxErrors; j++)
                    {
                        var a = openings[i];
                        var b = openings[j];
                        bool hOverlap = a.RightEdge + MinOpeningSpacing > b.LeftEdge
                                     && a.LeftEdge < b.RightEdge + MinOpeningSpacing;
                        bool vOverlap = a.TopEdge > b.BottomEdge && a.BottomEdge < b.TopEdge;

                        if (hOverlap && vOverlap)
                            errors.Add($"{a.Label} and {b.Label} on {wall} wall overlap or are too close (min spacing {MinOpeningSpacing}').");
                    }
                }

                postPositionsByWall.TryGetValue(wall, out var postPositions);
                postPositions ??= new List<double>();
                double postHalfWidth = (parameters.PostWidthInches / 12.0) / 2.0;

                foreach (var opening in openings)
                {
                    foreach (double postCenter in postPositions)
                    {
                        double postLeft = postCenter - postHalfWidth;
                        double postRight = postCenter + postHalfWidth;
                        if (opening.LeftEdge < postRight && opening.RightEdge > postLeft)
                            errors.Add($"{opening.Label} on {wall} wall conflicts with structural post at {postCenter:F1}'.");
                    }
                }
            }

            return errors;
        }

        private static List<OpeningRect> BuildOpeningRects(BarnParameters parameters)
        {
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

            return allOpenings;
        }

        /// <summary>
        /// Gets the center positions of structural posts along a wall in feet.
        /// Uses BarnGeometry.Posts as the source of truth.
        /// </summary>
        private static List<double> GetPostPositions(WallSide wall, BarnParameters parameters, BarnGeometry geometry)
        {
            var positions = geometry.Posts
                .Where(p => p.IsPlanInstance && p.Wall == wall)
                .Select(p => wall == WallSide.Front || wall == WallSide.Back ? p.X : p.Y)
                .OrderBy(v => v)
                .Distinct()
                .ToList();

            // Defensive fallback for incomplete geometry cases.
            if (positions.Count > 0) return positions;

            if (wall == WallSide.Front || wall == WallSide.Back)
            {
                // Endwalls: posts at corners and optional center post.
                positions.Add(0);
                positions.Add(parameters.BuildingWidth);
                if (parameters.BuildingWidth > BarnGeometryPostPlacement.EndwallCenterPostThresholdFeet)
                    positions.Add(parameters.BuildingWidth / 2.0);
            }
            else
            {
                double spacing = parameters.ActualBaySpacing;
                int bays = parameters.NumberOfBays;
                for (int i = 0; i <= bays; i++)
                    positions.Add(i * spacing);
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
