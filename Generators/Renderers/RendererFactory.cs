using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.Generators.Renderers
{
    /// <summary>
    /// Factory for selecting the appropriate renderer based on door/window type.
    /// </summary>
    public static class RendererFactory
    {
        private static readonly IOpeningRenderer OverheadDoor = new OverheadDoorRenderer();
        private static readonly IOpeningRenderer WalkDoor = new WalkDoorRenderer();
        private static readonly IOpeningRenderer SlidingDoor = new SlidingDoorRenderer();
        private static readonly IOpeningRenderer DutchDoor = new DutchDoorRenderer();

        private static readonly IWindowRenderer FixedWindow = new FixedWindowRenderer();
        private static readonly IWindowRenderer SingleHungWindow = new SingleHungWindowRenderer();
        private static readonly IWindowRenderer DoubleHungWindow = new DoubleHungWindowRenderer();
        private static readonly IWindowRenderer SlidingWindow = new SlidingWindowRenderer();
        private static readonly IWindowRenderer AwningWindow = new AwningWindowRenderer();

        public static IOpeningRenderer GetDoorRenderer(DoorType type)
        {
            switch (type)
            {
                case DoorType.Overhead: return OverheadDoor;
                case DoorType.Walk: return WalkDoor;
                case DoorType.Sliding: return SlidingDoor;
                case DoorType.Dutch: return DutchDoor;
                case DoorType.Double: return WalkDoor; // Double uses walk with two leaves
                default: return WalkDoor;
            }
        }

        public static IWindowRenderer GetWindowRenderer(WindowType type)
        {
            switch (type)
            {
                case WindowType.Fixed: return FixedWindow;
                case WindowType.SingleHung: return SingleHungWindow;
                case WindowType.DoubleHung: return DoubleHungWindow;
                case WindowType.Sliding: return SlidingWindow;
                case WindowType.BarnSash: return SlidingWindow;
                case WindowType.Awning: return AwningWindow;
                case WindowType.Casement:
                default:
                    return SingleHungWindow;
            }
        }
    }
}
