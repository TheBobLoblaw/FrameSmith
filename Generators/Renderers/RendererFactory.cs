using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.Generators.Renderers
{
    /// <summary>
    /// Factory for selecting the appropriate renderer based on door/window type.
    /// </summary>
    public static class RendererFactory
    {
        private static readonly IOpeningRenderer OverheadDoorRenderer = new OverheadDoorRenderer();
        private static readonly IOpeningRenderer WalkDoorRenderer = new WalkDoorRenderer();
        private static readonly IOpeningRenderer SlidingDoorRenderer = new SlidingDoorRenderer();
        private static readonly IOpeningRenderer DutchDoorRenderer = new DutchDoorRenderer();
        private static readonly IWindowRenderer SingleHungWindowRenderer = new SingleHungWindowRenderer();

        public static IOpeningRenderer GetDoorRenderer(DoorType type)
        {
            switch (type)
            {
                case DoorType.Overhead: return OverheadDoorRenderer;
                case DoorType.Walk: return WalkDoorRenderer;
                case DoorType.Sliding: return SlidingDoorRenderer;
                case DoorType.Dutch: return DutchDoorRenderer;
                case DoorType.Double: return WalkDoorRenderer; // Double uses walk with two leaves
                default: return WalkDoorRenderer;
            }
        }

        public static IWindowRenderer GetWindowRenderer(WindowType type)
        {
            switch (type)
            {
                case WindowType.SingleHung: return SingleHungWindowRenderer;
                // All other types fall back to single-hung for now
                default: return SingleHungWindowRenderer;
            }
        }
    }
}
