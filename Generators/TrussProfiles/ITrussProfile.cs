using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;
using System.Collections.Generic;

namespace PoleBarnGenerator.Generators.TrussProfiles
{
    /// <summary>
    /// Strategy interface for truss profile geometry and rendering.
    /// Each implementation defines a distinct roof type.
    /// </summary>
    public interface ITrussProfile
    {
        /// <summary>Display name (e.g., "Common Gable")</summary>
        string Name { get; }

        /// <summary>User-facing description for UI</summary>
        string Description { get; }

        /// <summary>
        /// Compute peak height for this truss type.
        /// </summary>
        double CalculatePeakHeight(double eaveHeight, double span, double pitchRise);

        /// <summary>
        /// Populate a TrussProfile struct with the geometry for this truss type.
        /// Called per bay line during BarnGeometry.ComputeTrusses.
        /// </summary>
        TrussProfile ComputeTruss(BarnParameters p, double bayPosition);

        /// <summary>
        /// Render the roof profile in front elevation view (2D).
        /// Returns entity count.
        /// </summary>
        int RenderFrontElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset);

        /// <summary>
        /// Render the roof profile in side elevation view (2D).
        /// Returns entity count.
        /// </summary>
        int RenderSideElevation(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset);

        /// <summary>
        /// Render one truss in 3D wireframe at the given bay position.
        /// Returns entity count.
        /// </summary>
        int Render3DTruss(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, TrussProfile truss);

        /// <summary>
        /// Render the plan-view roof outline (ridge line, break lines, etc.).
        /// Returns entity count.
        /// </summary>
        int RenderPlanRoofOutline(Transaction tr, BlockTableRecord btr,
            BarnGeometry geo, Vector3d offset);

        /// <summary>
        /// Names of additional parameters this truss type uses (for dynamic UI).
        /// </summary>
        List<string> GetParameterNames();
    }
}
