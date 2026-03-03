using System;
using System.Collections.Generic;
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
        public List<double> BayPositions { get; private set; }

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
            ComputeBayPositions();
            ComputePosts();
            ComputeGirts();
            ComputeTrusses();
            ComputePurlins();
            ComputeLeanTos();
            ComputePorches();
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
                    Wall = bayY == 0 ? WallSide.Front : bayY == Params.BuildingLength ? WallSide.Back : WallSide.Left
                });

                // Right sidewall post
                Posts.Add(new PostLocation
                {
                    X = w,
                    Y = bayY,
                    Height = Params.EaveHeight,
                    PostWidth = Params.PostWidthInches / 12.0,
                    PostDepth = Params.PostDepthInches / 12.0,
                    Wall = bayY == 0 ? WallSide.Front : bayY == Params.BuildingLength ? WallSide.Back : WallSide.Right
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
}
