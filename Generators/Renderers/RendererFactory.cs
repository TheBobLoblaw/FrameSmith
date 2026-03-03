using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.Generators.Renderers
{
    /// <summary>
    /// Factory for selecting the appropriate renderer based on door/window type.
    /// </summary>
    public static class RendererFactory
    {
        public static IOpeningRenderer GetDoorRenderer(DoorType type)
        {
            switch (type)
            {
                case DoorType.Overhead: return new OverheadDoorRenderer();
                case DoorType.Walk:     return new WalkDoorRenderer();
                case DoorType.Sliding:  return new SlidingDoorRenderer();
                case DoorType.Dutch:    return new DutchDoorRenderer();
                case DoorType.Double:   return new WalkDoorRenderer(); // Double uses walk with two leaves
                default:                return new WalkDoorRenderer();
            }
        }

        public static IWindowRenderer GetWindowRenderer(WindowType type)
        {
            switch (type)
            {
                case WindowType.SingleHung: return new SingleHungWindowRenderer();
                // All other types fall back to single-hung for now
                default: return new SingleHungWindowRenderer();
            }
        }
    }
}
