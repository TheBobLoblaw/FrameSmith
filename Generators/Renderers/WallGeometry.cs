using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.Generators.Renderers
{
    /// <summary>
    /// Helper that resolves wall-relative coordinates to world coordinates.
    /// Converts "along-wall" and "perpendicular" positions into X/Y for plan view.
    /// </summary>
    public class WallGeometry
    {
        public WallSide Wall { get; }
        public double WallLength { get; }
        public double BuildingWidth { get; }
        public double BuildingLength { get; }
        public double EaveHeight { get; }
        public double WallThickness { get; } = 0.5;

        public WallGeometry(BarnParameters p, WallSide wall)
        {
            Wall = wall;
            BuildingWidth = p.BuildingWidth;
            BuildingLength = p.BuildingLength;
            EaveHeight = p.EaveHeight;
            WallLength = (wall == WallSide.Front || wall == WallSide.Back)
                ? p.BuildingWidth
                : p.BuildingLength;
        }

        /// <summary>
        /// Converts along-wall + perpendicular offset to 2D plan point.
        /// perpendicular > 0 = outward from wall.
        /// </summary>
        public Point2d ToPlan(double alongWall, double perpendicular)
        {
            switch (Wall)
            {
                case WallSide.Front: return new Point2d(alongWall, -perpendicular);
                case WallSide.Back:  return new Point2d(alongWall, BuildingLength + perpendicular);
                case WallSide.Left:  return new Point2d(-perpendicular, alongWall);
                case WallSide.Right: return new Point2d(BuildingWidth + perpendicular, alongWall);
                default:             return new Point2d(alongWall, -perpendicular);
            }
        }

        /// <summary>Inward direction from this wall (unit vector in plan).</summary>
        public Vector2d InwardDirection()
        {
            switch (Wall)
            {
                case WallSide.Front: return new Vector2d(0, 1);
                case WallSide.Back:  return new Vector2d(0, -1);
                case WallSide.Left:  return new Vector2d(1, 0);
                case WallSide.Right: return new Vector2d(-1, 0);
                default:             return new Vector2d(0, 1);
            }
        }

        /// <summary>Outward direction from this wall.</summary>
        public Vector2d OutwardDirection()
        {
            var inv = InwardDirection();
            return new Vector2d(-inv.X, -inv.Y);
        }

        /// <summary>Along-wall direction (left-to-right facing the wall from outside).</summary>
        public Vector2d AlongWallDirection()
        {
            switch (Wall)
            {
                case WallSide.Front: return new Vector2d(1, 0);
                case WallSide.Back:  return new Vector2d(1, 0);
                case WallSide.Left:  return new Vector2d(0, 1);
                case WallSide.Right: return new Vector2d(0, 1);
                default:             return new Vector2d(1, 0);
            }
        }

        /// <summary>Whether this wall runs horizontally in plan (Front/Back).</summary>
        public bool IsHorizontalInPlan => Wall == WallSide.Front || Wall == WallSide.Back;
    }
}
