using System;
using System.Collections.Generic;

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
        public List<double> BayPositions { get; private set; }

        // Key dimensions
        public double RoofRise => Params.RoofRise;
        public double PeakHeight => Params.PeakHeight;
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
            ComputeBayPositions();
            ComputePosts();
            ComputeGirts();
            ComputeTrusses();
            ComputePurlins();
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
        // Post locations (grid of posts at bay lines × sidewall/endwall)
        // ───────────────────────────────────────────────
        private void ComputePosts()
        {
            Posts = new List<PostLocation>();
            double w = Params.BuildingWidth;

            foreach (double bayY in BayPositions)
            {
                // Left sidewall post
                Posts.Add(new PostLocation
                {
                    X = 0,
                    Y = bayY,
                    Height = Params.EaveHeight,
                    PostWidth = Params.PostWidthInches / 12.0,
                    PostDepth = Params.PostDepthInches / 12.0,
                    Wall = bayY == 0 || bayY == Params.BuildingLength ? WallSide.Front : WallSide.Left
                });

                // Right sidewall post
                Posts.Add(new PostLocation
                {
                    X = w,
                    Y = bayY,
                    Height = Params.EaveHeight,
                    PostWidth = Params.PostWidthInches / 12.0,
                    PostDepth = Params.PostDepthInches / 12.0,
                    Wall = bayY == 0 || bayY == Params.BuildingLength ? WallSide.Front : WallSide.Right
                });

                // Center post on endwalls (optional for wider buildings)
                if ((bayY == 0 || bayY == Params.BuildingLength) && w > 24)
                {
                    // Add intermediate endwall posts at ~half width
                    Posts.Add(new PostLocation
                    {
                        X = w / 2.0,
                        Y = bayY,
                        Height = Params.PeakHeight,
                        PostWidth = Params.PostWidthInches / 12.0,
                        PostDepth = Params.PostDepthInches / 12.0,
                        Wall = bayY == 0 ? WallSide.Front : WallSide.Back,
                        IsEndwallCenter = true
                    });
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
        private void ComputeTrusses()
        {
            Trusses = new List<TrussProfile>();
            double halfWidth = Params.BuildingWidth / 2.0;

            foreach (double bayY in BayPositions)
            {
                var truss = new TrussProfile
                {
                    BayPosition = bayY,
                    LeftEaveX = 0 - Params.OverhangEave,
                    RightEaveX = Params.BuildingWidth + Params.OverhangEave,
                    LeftEaveZ = Params.EaveHeight,
                    RightEaveZ = Params.EaveHeight,
                    PeakX = halfWidth,
                    PeakZ = Params.PeakHeight,
                    // Bottom chord (for common truss, same as eave height)
                    BottomChordZ = Params.EaveHeight
                };

                // Overhang: extend roof slope beyond eave
                double overhangDrop = Params.OverhangEave * (Params.RoofPitchRise / 12.0);
                truss.LeftEaveZ -= overhangDrop;
                truss.RightEaveZ -= overhangDrop;

                Trusses.Add(truss);
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

        /// <summary>Roof slope length (eave to ridge) for one side</summary>
        public double GetRoofSlopeLength()
        {
            double halfWidth = Params.BuildingWidth / 2.0;
            return Math.Sqrt(halfWidth * halfWidth + RoofRise * RoofRise);
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
    }

    public class PurlinLocation
    {
        public double X { get; set; }
        public double Z { get; set; }
        public bool IsLeftSlope { get; set; }
    }
}
