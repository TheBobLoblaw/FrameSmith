using System;
using PoleBarnGenerator.Models.Analysis;

namespace PoleBarnGenerator.Models.Materials
{
    /// <summary>
    /// Master engine that orchestrates all material takeoff calculations.
    /// Call RunFullTakeoff after structural analysis completes.
    /// </summary>
    public static class MaterialTakeoffEngine
    {
        public static MaterialTakeoff RunFullTakeoff(BarnGeometry geometry,
            StructuralDesignResult structural)
        {
            var takeoff = new MaterialTakeoff
            {
                ProjectDescription = $"{geometry.Params.BuildingWidth:F0}'×{geometry.Params.BuildingLength:F0}' " +
                    $"Pole Barn, {geometry.Params.EaveHeight:F0}' eave, {geometry.Params.RoofPitchDisplay} pitch",
                CalculationDate = DateTime.UtcNow
            };

            // Phase 1: Lumber
            takeoff.Lumber = LumberTakeoffCalculator.Calculate(geometry, structural);

            // Phase 2: Hardware (depends on lumber quantities)
            takeoff.Hardware = HardwareTakeoffCalculator.Calculate(geometry, structural, takeoff.Lumber);

            // Phase 3: Roofing
            takeoff.Roofing = RoofingTakeoffCalculator.Calculate(geometry);

            // Phase 4: Siding
            takeoff.Siding = SidingTakeoffCalculator.Calculate(geometry);

            // Phase 5: Foundation
            takeoff.Foundation = FoundationTakeoffCalculator.Calculate(geometry, structural);

            return takeoff;
        }
    }
}
