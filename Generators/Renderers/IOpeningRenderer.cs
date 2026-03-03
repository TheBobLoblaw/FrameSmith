using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.Generators.Renderers
{
    /// <summary>
    /// Strategy interface for rendering door openings in plan and elevation views.
    /// Each door type (Overhead, Walk, Sliding, Dutch, Double) has its own implementation.
    /// </summary>
    public interface IOpeningRenderer
    {
        /// <summary>
        /// Render the door in plan (top-down) view with proper architectural symbols.
        /// </summary>
        /// <returns>Number of entities created.</returns>
        int RenderPlan(Transaction tr, BlockTableRecord btr,
                       DoorOpening door, WallGeometry wall, Vector3d offset);

        /// <summary>
        /// Render the door in elevation (front/side) view with frame details,
        /// panels, and structural header.
        /// </summary>
        /// <returns>Number of entities created.</returns>
        int RenderElevation(Transaction tr, BlockTableRecord btr,
                            DoorOpening door, double wallHeight, Vector3d offset);
    }

    /// <summary>
    /// Strategy interface for rendering window openings.
    /// </summary>
    public interface IWindowRenderer
    {
        /// <summary>Render window in plan view (wall-line break with frame indication).</summary>
        int RenderPlan(Transaction tr, BlockTableRecord btr,
                       WindowOpening window, WallGeometry wall, Vector3d offset);

        /// <summary>Render window in elevation view with sash, frame, and sill details.</summary>
        int RenderElevation(Transaction tr, BlockTableRecord btr,
                            WindowOpening window, double wallHeight, Vector3d offset);
    }
}
