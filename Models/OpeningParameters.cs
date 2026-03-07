using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PoleBarnGenerator.Models
{
    public class OpeningParameters : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private List<DoorOpening> _doors = new List<DoorOpening>();
        public List<DoorOpening> Doors
        {
            get => _doors;
            set { _doors = value ?? new List<DoorOpening>(); OnPropertyChanged(); }
        }

        private List<WindowOpening> _windows = new List<WindowOpening>();
        public List<WindowOpening> Windows
        {
            get => _windows;
            set { _windows = value ?? new List<WindowOpening>(); OnPropertyChanged(); }
        }

        public (bool IsValid, string Error) Validate(BarnParameters barn)
        {
            foreach (var door in Doors)
            {
                if (door.Width <= 0) return (false, "Door width must be positive.");
                if (door.Height <= 0) return (false, "Door height must be positive.");
                if (door.Height > barn.EaveHeight) return (false, $"Door on {door.Wall} wall ({door.Height}') exceeds eave height ({barn.EaveHeight}').");
                if (door.CenterOffset < 0) return (false, "Door center offset must not be negative.");

                if (door.Type == DoorType.Dutch && (door.SplitHeight <= 0 || door.SplitHeight >= door.Height))
                    return (false, $"Dutch door split height must be between 0 and door height ({door.Height}').");
            }

            foreach (var window in Windows)
            {
                if (window.Width <= 0) return (false, "Window width must be positive.");
                if (window.Height <= 0) return (false, "Window height must be positive.");
                if (window.SillHeight < 0) return (false, "Window sill height must not be negative.");
                if (window.SillHeight + window.Height > barn.EaveHeight)
                    return (false, $"Window on {window.Wall} wall top ({window.SillHeight + window.Height}') exceeds eave height ({barn.EaveHeight}').");
                if (window.CenterOffset < 0) return (false, "Window center offset must not be negative.");
            }

            return (true, null);
        }
    }
}
