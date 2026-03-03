using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Models
{
    /// <summary>
    /// Computes all geometry for a single lean-to structure:
    /// posts, girts, purlins, roof tie-in, and footprint coordinates.
    /// Works in the main building's coordinate system.
    /// </summary>
    public class LeanToGeometry
    {
        public LeanToParameters LeanTo { get; }
        public BarnParameters MainParams { get; }
        public BarnGeometry MainGeo { get; }

        // Computed collections
        public List<PostLocation> Posts { get; private set; }
        public List<GirtLine> Girts { get; private set; }
        public List<LeanToPurlin> Purlins { get; private set; }

        // Key points in main building coordinate system
        /// <summary>Corners of the lean-to footprint [0]=attach-start, [1]=attach-end, [2]=outer-end, [3]=outer-start</summary>
        public FootprintCorners Corners { get; private set; }

        /// <summary>Height where lean-to roof meets main building wall</summary>
        public double TieInHeight => LeanTo.TieInHeight;

        /// <summary>Effective length along attachment wall</summary>
        public double EffectiveLength { get; private set; }

        /// <summary>Bay positions along the lean-to length (from attachment wall perspective)</summary>
        public List<double> BayPositions { get; private set; }

        public LeanToGeometry(LeanToParameters leanTo, BarnParameters mainParams, BarnGeometry mainGeo)
        {
            LeanTo = leanTo;
            MainParams = mainParams;
            MainGeo = mainGeo;
            Compute();
        }

        private void Compute()
        {
            EffectiveLength = LeanTo.GetEffectiveLength(MainParams);
            ComputeCorners();
            ComputeBayPositions();
            ComputePosts();
            ComputeGirts();
            ComputePurlins();
        }

        /// <summary>
        /// Computes the 4 corners of the lean-to footprint in main building coordinates.
        /// </summary>
        private void ComputeCorners()
        {
            double start = LeanTo.GetEffectiveStart();
            double end = LeanTo.GetEffectiveEnd(MainParams);
            double w = LeanTo.Width;
            double bw = MainParams.BuildingWidth;
            double bl = MainParams.BuildingLength;

            // Corners: AttachStart, AttachEnd, OuterEnd, OuterStart
            // "Along" axis runs along the attachment wall; "Out" axis is perpendicular outward
            switch (LeanTo.AttachmentWall)
            {
                case WallSide.Left:
                    // Attachment wall at X=0, lean-to extends in -X direction
                    Corners = new FootprintCorners(
                        attachStart: (0, start),
                        attachEnd: (0, end),
                        outerEnd: (-w, end),
                        outerStart: (-w, start));
                    break;

                case WallSide.Right:
                    // Attachment wall at X=BuildingWidth, extends in +X direction
                    Corners = new FootprintCorners(
                        attachStart: (bw, start),
                        attachEnd: (bw, end),
                        outerEnd: (bw + w, end),
                        outerStart: (bw + w, start));
                    break;

                case WallSide.Front:
                    // Attachment wall at Y=0, extends in -Y direction
                    Corners = new FootprintCorners(
                        attachStart: (start, 0),
                        attachEnd: (end, 0),
                        outerEnd: (end, -w),
                        outerStart: (start, -w));
                    break;

                case WallSide.Back:
                    // Attachment wall at Y=BuildingLength, extends in +Y direction
                    Corners = new FootprintCorners(
                        attachStart: (start, bl),
                        attachEnd: (end, bl),
                        outerEnd: (end, bl + w),
                        outerStart: (start, bl + w));
                    break;
            }
        }

        /// <summary>
        /// Compute bay positions along the lean-to length.
        /// Aligns with main building bay spacing where possible.
        /// </summary>
        private void ComputeBayPositions()
        {
            BayPositions = new List<double>();
            double start = LeanTo.GetEffectiveStart();
            double end = LeanTo.GetEffectiveEnd(MainParams);

            // Use main building bay spacing for consistency
            double spacing = MainParams.ActualBaySpacing;

            // Start with the start position
            BayPositions.Add(start);

            // Add main building bay positions that fall within our range
            foreach (double bayPos in MainGeo.BayPositions)
            {
                if (bayPos > start + 0.1 && bayPos < end - 0.1)
                {
                    BayPositions.Add(bayPos);
                }
            }

            // End position
            if (Math.Abs(BayPositions[BayPositions.Count - 1] - end) > 0.1)
            {
                BayPositions.Add(end);
            }
        }

        /// <summary>
        /// Compute post locations for the lean-to.
        /// Posts along the outer edge at each bay position.
        /// Shared posts on the attachment wall are the main building's posts.
        /// </summary>
        private void ComputePosts()
        {
            Posts = new List<PostLocation>();
            double w = LeanTo.Width;
            double bw = MainParams.BuildingWidth;
            double bl = MainParams.BuildingLength;
            double pw = MainParams.PostWidthInches / 12.0;
            double pd = MainParams.PostDepthInches / 12.0;

            foreach (double bayPos in BayPositions)
            {
                double x, y;
                switch (LeanTo.AttachmentWall)
                {
                    case WallSide.Left:
                        x = -w; y = bayPos; break;
                    case WallSide.Right:
                        x = bw + w; y = bayPos; break;
                    case WallSide.Front:
                        x = bayPos; y = -w; break;
                    case WallSide.Back:
                        x = bayPos; y = bl + w; break;
                    default:
                        x = 0; y = 0; break;
                }

                Posts.Add(new PostLocation
                {
                    X = x,
                    Y = y,
                    Height = LeanTo.EaveHeight,
                    PostWidth = pw,
                    PostDepth = pd,
                    Wall = LeanTo.AttachmentWall,
                    IsEndwallCenter = false
                });
            }
        }

        /// <summary>
        /// Compute girt elevations for the lean-to walls.
        /// </summary>
        private void ComputeGirts()
        {
            Girts = new List<GirtLine>();
            double girtSpacingFt = MainParams.GirtSpacing / 12.0;
            double elev = girtSpacingFt;

            while (elev < LeanTo.EaveHeight)
            {
                Girts.Add(new GirtLine { Elevation = elev });
                elev += girtSpacingFt;
            }

            if (Girts.Count == 0 || Math.Abs(Girts[Girts.Count - 1].Elevation - LeanTo.EaveHeight) > 0.1)
            {
                Girts.Add(new GirtLine { Elevation = LeanTo.EaveHeight });
            }
        }

        /// <summary>
        /// Compute purlin locations along the lean-to roof slope.
        /// </summary>
        private void ComputePurlins()
        {
            Purlins = new List<LeanToPurlin>();
            double purlinSpacingFt = MainParams.PurlinSpacing / 12.0;
            double slopeLength = GetRoofSlopeLength();
            int numPurlins = (int)Math.Floor(slopeLength / purlinSpacingFt);

            for (int i = 0; i <= numPurlins; i++)
            {
                double distAlongSlope = i * purlinSpacingFt;
                double fraction = distAlongSlope / slopeLength;

                // Purlin position: fraction 0 = outer eave, fraction 1 = tie-in at wall
                Purlins.Add(new LeanToPurlin
                {
                    FractionFromEave = fraction,
                    DistanceFromOuterEdge = fraction * LeanTo.Width,
                    Height = LeanTo.EaveHeight + fraction * (TieInHeight - LeanTo.EaveHeight)
                });
            }
        }

        /// <summary>Roof slope length from outer eave to tie-in point.</summary>
        public double GetRoofSlopeLength()
        {
            double rise = TieInHeight - LeanTo.EaveHeight;
            return Math.Sqrt(LeanTo.Width * LeanTo.Width + rise * rise);
        }

        /// <summary>
        /// Gets the 3D coordinates for the roof tie-in line on the main building wall.
        /// Returns (startPoint, endPoint) along the attachment wall at TieInHeight.
        /// </summary>
        public (double X1, double Y1, double Z1, double X2, double Y2, double Z2) GetTieInLine()
        {
            double start = LeanTo.GetEffectiveStart();
            double end = LeanTo.GetEffectiveEnd(MainParams);
            double z = TieInHeight;
            double bw = MainParams.BuildingWidth;
            double bl = MainParams.BuildingLength;

            switch (LeanTo.AttachmentWall)
            {
                case WallSide.Left:
                    return (0, start, z, 0, end, z);
                case WallSide.Right:
                    return (bw, start, z, bw, end, z);
                case WallSide.Front:
                    return (start, 0, z, end, 0, z);
                case WallSide.Back:
                    return (start, bl, z, end, bl, z);
                default:
                    return (0, 0, z, 0, 0, z);
            }
        }

        /// <summary>
        /// Gets the 3D coordinates for the outer eave line.
        /// </summary>
        public (double X1, double Y1, double Z1, double X2, double Y2, double Z2) GetOuterEaveLine()
        {
            double z = LeanTo.EaveHeight;
            var c = Corners;
            return (c.OuterStart.X, c.OuterStart.Y, z,
                    c.OuterEnd.X, c.OuterEnd.Y, z);
        }
    }

    /// <summary>
    /// Footprint corners for a lean-to in main building coordinates.
    /// </summary>
    public class FootprintCorners
    {
        public (double X, double Y) AttachStart { get; }
        public (double X, double Y) AttachEnd { get; }
        public (double X, double Y) OuterEnd { get; }
        public (double X, double Y) OuterStart { get; }

        public FootprintCorners(
            (double X, double Y) attachStart,
            (double X, double Y) attachEnd,
            (double X, double Y) outerEnd,
            (double X, double Y) outerStart)
        {
            AttachStart = attachStart;
            AttachEnd = attachEnd;
            OuterEnd = outerEnd;
            OuterStart = outerStart;
        }
    }

    /// <summary>
    /// Purlin location on a lean-to roof slope.
    /// </summary>
    public class LeanToPurlin
    {
        public double FractionFromEave { get; set; }
        public double DistanceFromOuterEdge { get; set; }
        public double Height { get; set; }
    }
}
