using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Generators.TrussProfiles;

namespace PoleBarnGenerator.Models
{
    /// <summary>
    /// Computes all structural geometry (post positions, girt elevations,
    /// truss points, purlin positions) from a BarnParameters instance.
    /// This is the bridge between user input and the drawing generators.
    /// </summary>
    public class BarnGeometry
    {
        public BarnParameters Params { get; }

        // Computed collections
        public List<PostLocation> Posts { get; private set; }
        public List<GirtLine> Girts { get; private set; }
        public List<TrussProfile> Trusses { get; private set; }
        public List<PurlinLocation> Purlins { get; private set; }
        public List<LeanToGeometry> LeanToGeometries { get; private set; }
        public List<PorchGeometry> PorchGeometries { get; private set; }
        public InteriorGeometry InteriorGeometry { get; private set; }
        public List<double> BayPositions { get; private set; }
        public List<double> FloorLevels { get; private set; }
        public List<FloorFramingLine> FloorFraming { get; private set; }
        public List<WallPathSegment> WallSegments { get; private set; }
        public List<Point2d> FootprintOutline { get; private set; }
        public List<Point2d> RoofIntersectionPoints { get; private set; }
        public List<ExpansionJointDetail> ExpansionJoints { get; private set; }

        // Key dimensions
        public double RoofRise => Params.RoofRise;
        public double PeakHeight => TrussProfile?.CalculatePeakHeight(Params.EaveHeight, Params.BuildingWidth, Params.RoofPitchRise) ?? Params.PeakHeight;
        public double RidgeY => Params.BuildingWidth / 2.0;
        public int NumBays => Params.NumberOfBays;
        public double ActualBaySpacing => Params.ActualBaySpacing;

        public BarnGeometry(BarnParameters parameters)
        {
            Params = parameters;
            Compute();
        }

        private void Compute()
        {
            ComputeFloorLevels();
            ComputeFootprint();
            ComputeRoofIntersections();
            ComputeBayPositions();
            ComputeExpansionJoints();
            ComputePosts();
            ComputeFloorFraming();
            ComputeGirts();
            ComputeTrusses();
            ComputePurlins();
            ComputeLeanTos();
            ComputePorches();
            ComputeInterior();
        }

        private void ComputeInterior()
        {
            InteriorGeometry = InteriorGeometry.Calculate(this,
                Params.HorseStalls, Params.Loft, Params.Partitions, Params.Workshop,
                Params.Drainage);
        }

        private void ComputeFloorLevels()
        {
            FloorLevels = new List<double>();
            var storyHeights = Params.GetResolvedFloorHeights();
            double currentElevation = 0;

            for (int i = 0; i < storyHeights.Count - 1; i++)
            {
                currentElevation += storyHeights[i];
                FloorLevels.Add(currentElevation);
            }
        }

        private void ComputeFootprint()
        {
            var strategy = FootprintStrategyFactory.Get(Params.FootprintShape);
            FootprintOutline = strategy.BuildPolygon(Params);

            // Ensure clockwise order for stable concavity checks.
            if (GetSignedArea(FootprintOutline) > 0)
            {
                FootprintOutline.Reverse();
            }

            WallSegments = BuildLinearSegments(FootprintOutline);

            if (Params.CurvedWall.Enabled)
            {
                WallSegments = BuildCurvedWallSegments();
            }
        }

        private void ComputeRoofIntersections()
        {
            RoofIntersectionPoints = new List<Point2d>();
            if (FootprintOutline == null || FootprintOutline.Count < 4)
            {
                return;
            }

            for (int i = 0; i < FootprintOutline.Count; i++)
            {
                Point2d prev = FootprintOutline[(i - 1 + FootprintOutline.Count) % FootprintOutline.Count];
                Point2d curr = FootprintOutline[i];
                Point2d next = FootprintOutline[(i + 1) % FootprintOutline.Count];

                var v1 = curr - prev;
                var v2 = next - curr;
                double cross = v1.X * v2.Y - v1.Y * v2.X;

                // Clockwise polygon: positive cross indicates reflex/inside corner.
                if (cross > 0.0001)
                {
                    RoofIntersectionPoints.Add(curr);
                }
            }
        }

        // ───────────────────────────────────────────────
        // Bay positions along the length (Y axis in plan)
        // ───────────────────────────────────────────────
        private void ComputeBayPositions()
        {
            BayPositions = new List<double>();
            for (int i = 0; i <= NumBays; i++)
            {
                BayPositions.Add(i * ActualBaySpacing);
            }
        }

        // ───────────────────────────────────────────────
        // Expansion joints
        // ───────────────────────────────────────────────
        private void ComputeExpansionJoints()
        {
            ExpansionJoints = new List<ExpansionJointDetail>();

            var locations = new List<double>();
            if (Params.ExpansionJoint.Enabled && Params.ExpansionJoint.Locations.Count > 0)
            {
                locations = Params.ExpansionJoint.Locations
                    .Where(l => l > 0 && l < Params.BuildingLength)
                    .Distinct()
                    .OrderBy(l => l)
                    .ToList();
            }
            else if (Params.BuildingLength > 120.0)
            {
                locations = Params.GetSuggestedExpansionJointLocations();
            }

            foreach (double location in locations)
            {
                ExpansionJoints.Add(new ExpansionJointDetail
                {
                    Location = location,
                    GapWidth = Params.ExpansionJoint.GapWidth,
                    JointType = Params.ExpansionJoint.JointType,
                    IsAutoSuggested = !Params.ExpansionJoint.Enabled
                });
            }
        }

        // ───────────────────────────────────────────────
        // Post locations
        // ───────────────────────────────────────────────
        private void ComputePosts()
        {
            Posts = new List<PostLocation>();

            var planPostPoints = SampleWallPathPoints();
            var storyHeights = Params.GetResolvedFloorHeights();

            foreach (var point in planPostPoints)
            {
                if (Params.FloorConnection == FloorConnectionType.ContinuousPost)
                {
                    Posts.Add(new PostLocation
                    {
                        X = point.X,
                        Y = point.Y,
                        Height = Params.EaveHeight,
                        BaseElevation = 0,
                        TopElevation = Params.EaveHeight,
                        StoryIndex = -1,
                        IsPlanInstance = true,
                        IsContinuous = true,
                        PostWidth = Params.PostWidthInches / 12.0,
                        PostDepth = Params.PostDepthInches / 12.0,
                        Wall = ResolveWall(point)
                    });
                }
                else
                {
                    double baseElevation = 0;
                    for (int story = 0; story < storyHeights.Count; story++)
                    {
                        double topElevation = baseElevation + storyHeights[story];
                        Posts.Add(new PostLocation
                        {
                            X = point.X,
                            Y = point.Y,
                            Height = storyHeights[story],
                            BaseElevation = baseElevation,
                            TopElevation = topElevation,
                            StoryIndex = story,
                            IsSplice = story > 0,
                            IsPlanInstance = story == 0,
                            IsContinuous = false,
                            LoadPathFromAbove = story < storyHeights.Count - 1,
                            PostWidth = Params.PostWidthInches / 12.0,
                            PostDepth = Params.PostDepthInches / 12.0,
                            Wall = ResolveWall(point)
                        });

                        baseElevation = topElevation;
                    }
                }
            }

            // Preserve legacy center post behavior on endwalls for wide rectangular buildings.
            if (!Params.CurvedWall.Enabled && Params.FootprintShape == FootprintShape.Rectangle && Params.BuildingWidth > 24)
            {
                AddEndwallCenterPost(0);
                AddEndwallCenterPost(Params.BuildingLength);
            }
        }

        private void AddEndwallCenterPost(double y)
        {
            bool exists = Posts.Any(p => Math.Abs(p.X - Params.BuildingWidth / 2.0) < 0.01 && Math.Abs(p.Y - y) < 0.01 && p.IsPlanInstance);
            if (exists)
            {
                return;
            }

            Posts.Add(new PostLocation
            {
                X = Params.BuildingWidth / 2.0,
                Y = y,
                Height = Params.EaveHeight,
                BaseElevation = 0,
                TopElevation = Params.EaveHeight,
                StoryIndex = -1,
                IsPlanInstance = true,
                IsContinuous = Params.FloorConnection == FloorConnectionType.ContinuousPost,
                PostWidth = Params.PostWidthInches / 12.0,
                PostDepth = Params.PostDepthInches / 12.0,
                Wall = y == 0 ? WallSide.Front : WallSide.Back,
                IsEndwallCenter = true
            });
        }

        private void ComputeFloorFraming()
        {
            FloorFraming = new List<FloorFramingLine>();

            if (FloorLevels.Count == 0)
            {
                return;
            }

            foreach (double elevation in FloorLevels)
            {
                foreach (var segment in WallSegments)
                {
                    if (segment.IsArc)
                    {
                        FloorFraming.Add(new FloorFramingLine
                        {
                            IsArc = true,
                            Elevation = elevation,
                            BeamSize = Params.FloorBeamSize,
                            Start = segment.Start,
                            End = segment.End,
                            ArcCenter = segment.ArcCenter,
                            ArcRadius = segment.ArcRadius,
                            StartAngle = segment.StartAngle,
                            EndAngle = segment.EndAngle
                        });
                    }
                    else
                    {
                        FloorFraming.Add(new FloorFramingLine
                        {
                            IsArc = false,
                            Elevation = elevation,
                            BeamSize = Params.FloorBeamSize,
                            Start = segment.Start,
                            End = segment.End
                        });
                    }
                }
            }
        }

        // ───────────────────────────────────────────────
        // Girt elevations (horizontal members on walls)
        // ───────────────────────────────────────────────
        private void ComputeGirts()
        {
            Girts = new List<GirtLine>();
            double girtSpacingFt = Params.GirtSpacing / 12.0;
            double currentElev = girtSpacingFt; // first girt above ground

            while (currentElev < Params.EaveHeight)
            {
                Girts.Add(new GirtLine { Elevation = currentElev });
                currentElev += girtSpacingFt;
            }

            // Top girt at eave
            if (Girts.Count == 0 || Math.Abs(Girts[Girts.Count - 1].Elevation - Params.EaveHeight) > 0.1)
            {
                Girts.Add(new GirtLine { Elevation = Params.EaveHeight });
            }
        }

        // ───────────────────────────────────────────────
        // Truss profiles at each bay line
        // ───────────────────────────────────────────────
        /// <summary>The active truss profile strategy for this geometry.</summary>
        public ITrussProfile TrussProfile { get; private set; }

        private void ComputeTrusses()
        {
            Trusses = new List<TrussProfile>();
            TrussProfile = TrussFactory.GetTrussProfile(Params.TrussType);

            foreach (double bayY in BayPositions)
            {
                Trusses.Add(TrussProfile.ComputeTruss(Params, bayY));
            }
        }

        // ───────────────────────────────────────────────
        // Purlins along the roof slope
        // ───────────────────────────────────────────────
        private void ComputePurlins()
        {
            Purlins = new List<PurlinLocation>();
            double purlinSpacingFt = Params.PurlinSpacing / 12.0;
            double slopeLength = GetRoofSlopeLength();
            double numPurlins = Math.Floor(slopeLength / purlinSpacingFt);

            // Left slope purlins
            for (int i = 0; i <= numPurlins; i++)
            {
                double distAlongSlope = i * purlinSpacingFt;
                double fraction = distAlongSlope / slopeLength;

                Purlins.Add(new PurlinLocation
                {
                    X = fraction * (Params.BuildingWidth / 2.0),
                    Z = Params.EaveHeight + fraction * RoofRise,
                    IsLeftSlope = true
                });
            }

            // Right slope purlins (mirror)
            for (int i = 0; i <= numPurlins; i++)
            {
                double distAlongSlope = i * purlinSpacingFt;
                double fraction = distAlongSlope / slopeLength;

                Purlins.Add(new PurlinLocation
                {
                    X = Params.BuildingWidth - fraction * (Params.BuildingWidth / 2.0),
                    Z = Params.EaveHeight + fraction * RoofRise,
                    IsLeftSlope = false
                });
            }
        }


        private void ComputeLeanTos()
        {
            LeanToGeometries = new List<LeanToGeometry>();
            foreach (var lt in Params.LeanTos)
            {
                if (lt.Enabled)
                    LeanToGeometries.Add(new LeanToGeometry(lt, Params, this));
            }
        }

        private void ComputePorches()
        {
            PorchGeometries = new List<PorchGeometry>();
            foreach (var porch in Params.AllPorches)
            {
                if (porch.IsEnabled)
                    PorchGeometries.Add(new PorchGeometry(porch, Params, this));
            }
        }

        /// <summary>Roof slope length (eave to ridge) for one side</summary>
        public double GetRoofSlopeLength()
        {
            double halfWidth = Params.BuildingWidth / 2.0;
            return Math.Sqrt(halfWidth * halfWidth + RoofRise * RoofRise);
        }

        private List<Point2d> SampleWallPathPoints()
        {
            var points = new List<Point2d>();
            double spacing = Math.Max(0.5, Params.BaySpacing);

            foreach (var segment in WallSegments)
            {
                if (segment.IsArc)
                {
                    double sweep = segment.EndAngle - segment.StartAngle;
                    int divisions = Math.Max(1, (int)Math.Floor(Math.Abs(sweep) * segment.ArcRadius / spacing));
                    for (int i = 0; i <= divisions; i++)
                    {
                        double t = divisions == 0 ? 0 : (double)i / divisions;
                        double angle = segment.StartAngle + sweep * t;
                        var point = new Point2d(
                            segment.ArcCenter.X + segment.ArcRadius * Math.Cos(angle),
                            segment.ArcCenter.Y + segment.ArcRadius * Math.Sin(angle));
                        AddUniquePoint(points, point);
                    }
                }
                else
                {
                    double len = segment.Start.GetDistanceTo(segment.End);
                    int divisions = Math.Max(1, (int)Math.Floor(len / spacing));
                    for (int i = 0; i <= divisions; i++)
                    {
                        double t = divisions == 0 ? 0 : (double)i / divisions;
                        var point = new Point2d(
                            segment.Start.X + (segment.End.X - segment.Start.X) * t,
                            segment.Start.Y + (segment.End.Y - segment.Start.Y) * t);
                        AddUniquePoint(points, point);
                    }
                }
            }

            return points;
        }

        private static void AddUniquePoint(List<Point2d> points, Point2d point)
        {
            foreach (var existing in points)
            {
                if (existing.GetDistanceTo(point) < 0.05)
                {
                    return;
                }
            }

            points.Add(point);
        }

        private WallSide ResolveWall(Point2d pt)
        {
            if (Math.Abs(pt.Y) < 0.1)
                return WallSide.Front;
            if (Math.Abs(pt.Y - Params.BuildingLength) < 0.1)
                return WallSide.Back;
            if (Math.Abs(pt.X) < 0.1)
                return WallSide.Left;
            return WallSide.Right;
        }

        private List<WallPathSegment> BuildLinearSegments(List<Point2d> polygon)
        {
            var segments = new List<WallPathSegment>();
            if (polygon == null || polygon.Count < 3)
            {
                return segments;
            }

            for (int i = 0; i < polygon.Count; i++)
            {
                Point2d start = polygon[i];
                Point2d end = polygon[(i + 1) % polygon.Count];
                segments.Add(new WallPathSegment { Start = start, End = end, IsArc = false });
            }

            return segments;
        }

        private List<WallPathSegment> BuildCurvedWallSegments()
        {
            double width = Params.BuildingWidth;
            double angleRad = Math.PI * Params.CurvedWall.ArcAngleDegrees / 180.0;
            double centerRadius = Params.CurvedWall.Radius;

            // Chord mode keeps outer chord equal to the user-specified building length.
            if (Params.CurvedWall.Mode == CurvedWallMode.ChordDriven)
            {
                double desiredChord = Math.Max(1.0, Params.BuildingLength);
                double ratio = desiredChord / (2.0 * centerRadius);
                ratio = Math.Max(-1.0, Math.Min(1.0, ratio));
                angleRad = 2.0 * Math.Asin(ratio);
            }

            double theta0 = Math.PI / 2.0 - angleRad / 2.0;
            double theta1 = Math.PI / 2.0 + angleRad / 2.0;
            Point2d center = new Point2d(width / 2.0, -centerRadius);
            double innerRadius = centerRadius - width / 2.0;
            double outerRadius = centerRadius + width / 2.0;

            var outerStart = new Point2d(center.X + outerRadius * Math.Cos(theta0), center.Y + outerRadius * Math.Sin(theta0));
            var outerEnd = new Point2d(center.X + outerRadius * Math.Cos(theta1), center.Y + outerRadius * Math.Sin(theta1));
            var innerStart = new Point2d(center.X + innerRadius * Math.Cos(theta0), center.Y + innerRadius * Math.Sin(theta0));
            var innerEnd = new Point2d(center.X + innerRadius * Math.Cos(theta1), center.Y + innerRadius * Math.Sin(theta1));

            FootprintOutline = new List<Point2d> { outerStart, outerEnd, innerEnd, innerStart };

            return new List<WallPathSegment>
            {
                new WallPathSegment
                {
                    IsArc = true,
                    Start = outerStart,
                    End = outerEnd,
                    ArcCenter = center,
                    ArcRadius = outerRadius,
                    StartAngle = theta0,
                    EndAngle = theta1
                },
                new WallPathSegment { IsArc = false, Start = outerEnd, End = innerEnd },
                new WallPathSegment
                {
                    IsArc = true,
                    Start = innerEnd,
                    End = innerStart,
                    ArcCenter = center,
                    ArcRadius = innerRadius,
                    StartAngle = theta1,
                    EndAngle = theta0
                },
                new WallPathSegment { IsArc = false, Start = innerStart, End = outerStart }
            };
        }

        private static double GetSignedArea(List<Point2d> polygon)
        {
            double area = 0;
            for (int i = 0; i < polygon.Count; i++)
            {
                Point2d a = polygon[i];
                Point2d b = polygon[(i + 1) % polygon.Count];
                area += (a.X * b.Y) - (b.X * a.Y);
            }

            return area * 0.5;
        }
    }

    // ───────────────────────────────────────────────
    // Geometry Data Structures
    // ───────────────────────────────────────────────

    public class PostLocation
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Height { get; set; }
        public double BaseElevation { get; set; }
        public double TopElevation { get; set; }
        public int StoryIndex { get; set; }
        public bool IsSplice { get; set; }
        public bool IsContinuous { get; set; }
        public bool LoadPathFromAbove { get; set; }
        public bool IsPlanInstance { get; set; } = true;
        public double PostWidth { get; set; }  // in feet
        public double PostDepth { get; set; }  // in feet
        public WallSide Wall { get; set; }
        public bool IsEndwallCenter { get; set; }
    }

    public class GirtLine
    {
        public double Elevation { get; set; } // feet above ground
    }

    public class TrussProfile
    {
        public double BayPosition { get; set; }   // Y position along building length
        public double LeftEaveX { get; set; }
        public double RightEaveX { get; set; }
        public double LeftEaveZ { get; set; }
        public double RightEaveZ { get; set; }
        public double PeakX { get; set; }
        public double PeakZ { get; set; }
        public double BottomChordZ { get; set; }

        // ── Gambrel-specific ──
        public double GambrelBreakX { get; set; }
        public double GambrelBreakZ { get; set; }

        // ── Scissor-specific ──
        public double ScissorBottomPeakZ { get; set; }

        // ── Monitor-specific ──
        public double MonitorLeftX { get; set; }
        public double MonitorRightX { get; set; }
        public double MonitorBaseZ { get; set; }
        public double MonitorTopZ { get; set; }
    }

    public class PurlinLocation
    {
        public double X { get; set; }
        public double Z { get; set; }
        public bool IsLeftSlope { get; set; }
    }

    public class FloorFramingLine
    {
        public bool IsArc { get; set; }
        public Point2d Start { get; set; }
        public Point2d End { get; set; }
        public Point2d ArcCenter { get; set; }
        public double ArcRadius { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }
        public double Elevation { get; set; }
        public string BeamSize { get; set; }
    }

    public class WallPathSegment
    {
        public bool IsArc { get; set; }
        public Point2d Start { get; set; }
        public Point2d End { get; set; }
        public Point2d ArcCenter { get; set; }
        public double ArcRadius { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }
    }

    public class ExpansionJointDetail
    {
        public double Location { get; set; }
        public double GapWidth { get; set; }
        public ExpansionJointType JointType { get; set; }
        public bool IsAutoSuggested { get; set; }
    }

    internal interface IFootprintStrategy
    {
        List<Point2d> BuildPolygon(BarnParameters p);
    }

    internal static class FootprintStrategyFactory
    {
        public static IFootprintStrategy Get(FootprintShape shape)
        {
            return shape switch
            {
                FootprintShape.LShape => new LShapeFootprintStrategy(),
                FootprintShape.TShape => new TShapeFootprintStrategy(),
                FootprintShape.UShape => new UShapeFootprintStrategy(),
                FootprintShape.CustomPolygon => new CustomPolygonFootprintStrategy(),
                _ => new RectangleFootprintStrategy()
            };
        }
    }

    internal sealed class RectangleFootprintStrategy : IFootprintStrategy
    {
        public List<Point2d> BuildPolygon(BarnParameters p)
        {
            return new List<Point2d>
            {
                new Point2d(0, 0),
                new Point2d(p.BuildingWidth, 0),
                new Point2d(p.BuildingWidth, p.BuildingLength),
                new Point2d(0, p.BuildingLength)
            };
        }
    }

    internal sealed class LShapeFootprintStrategy : IFootprintStrategy
    {
        public List<Point2d> BuildPolygon(BarnParameters p)
        {
            double iw = Math.Min(p.FootprintInsetWidth, p.BuildingWidth - 1.0);
            double id = Math.Min(p.FootprintInsetDepth, p.BuildingLength - 1.0);
            return new List<Point2d>
            {
                new Point2d(0, 0),
                new Point2d(p.BuildingWidth, 0),
                new Point2d(p.BuildingWidth, p.BuildingLength - id),
                new Point2d(iw, p.BuildingLength - id),
                new Point2d(iw, p.BuildingLength),
                new Point2d(0, p.BuildingLength)
            };
        }
    }

    internal sealed class TShapeFootprintStrategy : IFootprintStrategy
    {
        public List<Point2d> BuildPolygon(BarnParameters p)
        {
            double stemWidth = Math.Max(2.0, p.BuildingWidth - 2.0 * p.FootprintInsetWidth);
            double stemLeft = (p.BuildingWidth - stemWidth) / 2.0;
            double stemRight = stemLeft + stemWidth;
            double stemDepth = Math.Min(p.FootprintInsetDepth, p.BuildingLength - 1.0);

            return new List<Point2d>
            {
                new Point2d(0, 0),
                new Point2d(p.BuildingWidth, 0),
                new Point2d(p.BuildingWidth, stemDepth),
                new Point2d(stemRight, stemDepth),
                new Point2d(stemRight, p.BuildingLength),
                new Point2d(stemLeft, p.BuildingLength),
                new Point2d(stemLeft, stemDepth),
                new Point2d(0, stemDepth)
            };
        }
    }

    internal sealed class UShapeFootprintStrategy : IFootprintStrategy
    {
        public List<Point2d> BuildPolygon(BarnParameters p)
        {
            double legInset = Math.Min(p.FootprintInsetWidth, p.BuildingWidth / 2.0 - 1.0);
            double courtyardDepth = Math.Min(p.FootprintInsetDepth, p.BuildingLength - 1.0);

            return new List<Point2d>
            {
                new Point2d(0, 0),
                new Point2d(p.BuildingWidth, 0),
                new Point2d(p.BuildingWidth, p.BuildingLength),
                new Point2d(p.BuildingWidth - legInset, p.BuildingLength),
                new Point2d(p.BuildingWidth - legInset, courtyardDepth),
                new Point2d(legInset, courtyardDepth),
                new Point2d(legInset, p.BuildingLength),
                new Point2d(0, p.BuildingLength)
            };
        }
    }

    internal sealed class CustomPolygonFootprintStrategy : IFootprintStrategy
    {
        public List<Point2d> BuildPolygon(BarnParameters p)
        {
            if (p.FootprintVertices != null && p.FootprintVertices.Count >= 3)
            {
                return p.FootprintVertices.Select(v => new Point2d(v.X, v.Y)).ToList();
            }

            return new RectangleFootprintStrategy().BuildPolygon(p);
        }
    }
}
