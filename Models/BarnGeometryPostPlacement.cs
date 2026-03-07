namespace PoleBarnGenerator.Models
{
    /// <summary>
    /// Shared post-placement decisions used by BarnGeometry.
    /// </summary>
    public static class BarnGeometryPostPlacement
    {
        public const double EndwallCenterPostThresholdFeet = 24.0;

        public static bool ShouldAddEndwallCenterPosts(BarnParameters parameters)
        {
            if (parameters == null)
            {
                return false;
            }

            return ShouldAddEndwallCenterPosts(
                parameters.CurvedWall.Enabled,
                parameters.FootprintShape,
                parameters.BuildingWidth);
        }

        public static bool ShouldAddEndwallCenterPosts(bool curvedWallsEnabled, FootprintShape footprintShape, double buildingWidth)
        {
            return !curvedWallsEnabled
                && footprintShape == FootprintShape.Rectangle
                && buildingWidth > EndwallCenterPostThresholdFeet;
        }
    }
}
