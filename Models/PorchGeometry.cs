using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Models
{
    /// <summary>
    /// Computes all geometry for a single porch structure.
    /// Leverages similar coordinate logic as LeanToGeometry.
    /// </summary>
    public class PorchGeometry
    {
        public PorchParameters Porch { get; }
        public BarnParameters MainParams { get; }
        public BarnGeometry MainGeo { get; }

        public FootprintCorners Corners { get; private set; }
        public List<(double X, double Y)> ColumnLocations { get; private set; }
        public List<(double X1, double Y1, double X2, double Y2)> RailingSegments { get; private set; }
        public double TieInHeight { get; private set; }
        public double OuterEaveHeight { get; private set; }
        public double EffectiveLength { get; private set; }

        public PorchGeometry(PorchParameters porch, BarnParameters mainParams, BarnGeometry mainGeo)
        {
            Porch = porch;
            MainParams = mainParams;
            MainGeo = mainGeo;
            Compute();
        }

        private void Compute()
        {
            TieInHeight = Porch.GetTieInHeight(MainParams.EaveHeight);
            OuterEaveHeight = Porch.GetOuterEaveHeight(MainParams.EaveHeight);
            EffectiveLength = Porch.GetEffectiveLength(MainParams);
            ComputeCorners();
            ComputeColumns();
            ComputeRailings();
        }

        private void ComputeCorners()
        {
            double start = Porch.GetEffectiveStart();
            double end = Porch.GetEffectiveEnd(MainParams);
            double d = Porch.Depth;
            double bw = MainParams.BuildingWidth;
            double bl = MainParams.BuildingLength;

            switch (Porch.AttachmentWall)
            {
                case WallSide.Front:
                    Corners = new FootprintCorners(
                        attachStart: (start, 0),
                        attachEnd: (end, 0),
                        outerEnd: (end, -d),
                        outerStart: (start, -d));
                    break;
                case WallSide.Back:
                    Corners = new FootprintCorners(
                        attachStart: (start, bl),
                        attachEnd: (end, bl),
                        outerEnd: (end, bl + d),
                        outerStart: (start, bl + d));
                    break;
                case WallSide.Left:
                    Corners = new FootprintCorners(
                        attachStart: (0, start),
                        attachEnd: (0, end),
                        outerEnd: (-d, end),
                        outerStart: (-d, start));
                    break;
                case WallSide.Right:
                    Corners = new FootprintCorners(
                        attachStart: (bw, start),
                        attachEnd: (bw, end),
                        outerEnd: (bw + d, end),
                        outerStart: (bw + d, start));
                    break;
            }
        }

        private void ComputeColumns()
        {
            ColumnLocations = new List<(double X, double Y)>();
            double start = Porch.GetEffectiveStart();
            double end = Porch.GetEffectiveEnd(MainParams);
            double spacing = Porch.ColumnSpacing;
            double d = Porch.Depth;
            double bw = MainParams.BuildingWidth;
            double bl = MainParams.BuildingLength;

            // Generate column positions along the outer edge
            int numColumns = Math.Max(2, (int)Math.Ceiling((end - start) / spacing) + 1);
            double actualSpacing = (end - start) / (numColumns - 1);

            for (int i = 0; i < numColumns; i++)
            {
                double pos = start + i * actualSpacing;
                switch (Porch.AttachmentWall)
                {
                    case WallSide.Front:
                        ColumnLocations.Add((pos, -d));
                        break;
                    case WallSide.Back:
                        ColumnLocations.Add((pos, bl + d));
                        break;
                    case WallSide.Left:
                        ColumnLocations.Add((-d, pos));
                        break;
                    case WallSide.Right:
                        ColumnLocations.Add((bw + d, pos));
                        break;
                }
            }
        }

        private void ComputeRailings()
        {
            RailingSegments = new List<(double, double, double, double)>();
            if (!Porch.HasRailing || ColumnLocations.Count < 2) return;

            // Outer edge railing (connects all columns)
            for (int i = 0; i < ColumnLocations.Count - 1; i++)
            {
                RailingSegments.Add((
                    ColumnLocations[i].X, ColumnLocations[i].Y,
                    ColumnLocations[i + 1].X, ColumnLocations[i + 1].Y));
            }

            // End railings (connect first/last column back to building)
            var first = ColumnLocations[0];
            var last = ColumnLocations[ColumnLocations.Count - 1];
            var c = Corners;

            RailingSegments.Add((first.X, first.Y, c.AttachStart.X, c.AttachStart.Y));
            RailingSegments.Add((last.X, last.Y, c.AttachEnd.X, c.AttachEnd.Y));
        }

        /// <summary>Gets 3D tie-in line coordinates</summary>
        public (double X1, double Y1, double Z1, double X2, double Y2, double Z2) GetTieInLine()
        {
            double start = Porch.GetEffectiveStart();
            double end = Porch.GetEffectiveEnd(MainParams);
            double z = TieInHeight;
            double bw = MainParams.BuildingWidth;
            double bl = MainParams.BuildingLength;

            switch (Porch.AttachmentWall)
            {
                case WallSide.Front: return (start, 0, z, end, 0, z);
                case WallSide.Back: return (start, bl, z, end, bl, z);
                case WallSide.Left: return (0, start, z, 0, end, z);
                case WallSide.Right: return (bw, start, z, bw, end, z);
                default: return (0, 0, z, 0, 0, z);
            }
        }

        /// <summary>Gets 3D outer eave line coordinates</summary>
        public (double X1, double Y1, double Z1, double X2, double Y2, double Z2) GetOuterEaveLine()
        {
            double z = OuterEaveHeight;
            var c = Corners;
            return (c.OuterStart.X, c.OuterStart.Y, z,
                    c.OuterEnd.X, c.OuterEnd.Y, z);
        }
    }
}
