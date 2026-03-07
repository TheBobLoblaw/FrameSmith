using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Utils;

namespace PoleBarnGenerator.UI
{
    /// <summary>
    /// View model wrapper for DoorOpening with display and validation properties.
    /// </summary>
    public class DoorViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public DoorOpening Model { get; }

        public DoorViewModel(DoorOpening model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public string TypeIcon => Model.Type switch
        {
            DoorType.Overhead => "⬆",
            DoorType.Sliding => "↔",
            DoorType.Walk => "🚶",
            DoorType.Dutch => "⬒",
            DoorType.Double => "⇔",
            _ => "?"
        };

        public string TypeName => Model.Type.ToString();
        public string WallName => Model.Wall.ToString();
        public string SizeDisplay => $"{Model.Width}'×{Model.Height}'";
        public string PositionDisplay => $"{Model.CenterOffset:F1}'";

        private string _validationStatus = "Valid";
        public string ValidationStatus
        {
            get => _validationStatus;
            set { _validationStatus = value; Notify(nameof(ValidationStatus)); Notify(nameof(StatusIcon)); }
        }

        public string StatusIcon => ValidationStatus switch
        {
            "Error" => "❌",
            "Warning" => "⚠️",
            _ => "✅"
        };

        private string _validationMessage = "";
        public string ValidationMessage
        {
            get => _validationMessage;
            set { _validationMessage = value; Notify(nameof(ValidationMessage)); }
        }

        public void Refresh()
        {
            Notify(nameof(TypeIcon));
            Notify(nameof(TypeName));
            Notify(nameof(WallName));
            Notify(nameof(SizeDisplay));
            Notify(nameof(PositionDisplay));
        }
    }

    /// <summary>
    /// View model wrapper for WindowOpening with display and validation properties.
    /// </summary>
    public class WindowViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void Notify(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public WindowOpening Model { get; }

        public WindowViewModel(WindowOpening model)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public string TypeIcon => Model.Type switch
        {
            WindowType.Fixed => "▣",
            WindowType.SingleHung => "⬆",
            WindowType.Sliding => "↔",
            WindowType.BarnSash => "⊞",
            WindowType.Awning => "⏏",
            WindowType.Casement => "⊟",
            _ => "?"
        };

        public string TypeName => Model.Type switch
        {
            WindowType.SingleHung => "Sgl Hung",
            WindowType.BarnSash => "Barn Sash",
            _ => Model.Type.ToString()
        };

        public string WallName => Model.Wall.ToString();
        public string SizeDisplay => $"{Model.Width}'×{Model.Height}'";
        public string SillDisplay => $"{Model.SillHeight:F1}'";

        private string _validationStatus = "Valid";
        public string ValidationStatus
        {
            get => _validationStatus;
            set { _validationStatus = value; Notify(nameof(ValidationStatus)); Notify(nameof(StatusIcon)); }
        }

        public string StatusIcon => ValidationStatus switch
        {
            "Error" => "❌",
            "Warning" => "⚠️",
            _ => "✅"
        };

        private string _validationMessage = "";
        public string ValidationMessage
        {
            get => _validationMessage;
            set { _validationMessage = value; Notify(nameof(ValidationMessage)); }
        }

        public void Refresh()
        {
            Notify(nameof(TypeIcon));
            Notify(nameof(TypeName));
            Notify(nameof(WallName));
            Notify(nameof(SizeDisplay));
            Notify(nameof(SillDisplay));
        }
    }

    // Value converters for DataGrid styling
    public class ValidationStatusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() ?? "Valid";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class DoorTypeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() ?? "";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public class WindowTypeIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value?.ToString() ?? "";
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    public partial class OpeningManagerControl : UserControl
    {
        private BarnParameters _parameters;
        private ObservableCollection<DoorViewModel> _doorVMs = new();
        private ObservableCollection<WindowViewModel> _windowVMs = new();
        private bool _suppressPropertyEvents = false;
        private DispatcherTimer _validationTimer;

        public OpeningManagerControl()
        {
            InitializeComponent();

            dgDoors.ItemsSource = _doorVMs;
            dgWindows.ItemsSource = _windowVMs;

            // Debounced validation timer (300ms)
            _validationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _validationTimer.Tick += (s, e) =>
            {
                _validationTimer.Stop();
                RunValidation();
            };
        }

        /// <summary>
        /// Binds this control to BarnParameters. Call from parent dialog.
        /// </summary>
        public void BindParameters(BarnParameters parameters)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

            _doorVMs.Clear();
            foreach (var door in _parameters.Doors)
                _doorVMs.Add(new DoorViewModel(door));

            _windowVMs.Clear();
            foreach (var window in _parameters.Windows)
                _windowVMs.Add(new WindowViewModel(window));

            ScheduleValidation();
        }

        /// <summary>
        /// Syncs view models back to BarnParameters model lists.
        /// </summary>
        private void SyncToModel()
        {
            if (_parameters == null) return;
            _parameters.Doors = _doorVMs.Select(vm => vm.Model).ToList();
            _parameters.Windows = _windowVMs.Select(vm => vm.Model).ToList();
        }

        private void ScheduleValidation()
        {
            _validationTimer.Stop();
            _validationTimer.Start();
        }

        private void RunValidation()
        {
            if (_parameters == null) return;

            SyncToModel();
            List<string> errors;
            try
            {
                var geometry = new BarnGeometry(_parameters);
                errors = OpeningValidator.ValidateOpenings(_parameters, geometry);
            }
            catch (Exception ex)
            {
                errors = new List<string> { $"Unable to validate openings with current geometry: {ex.Message}" };
            }

            // Reset all statuses
            foreach (var vm in _doorVMs) { vm.ValidationStatus = "Valid"; vm.ValidationMessage = ""; }
            foreach (var vm in _windowVMs) { vm.ValidationStatus = "Valid"; vm.ValidationMessage = ""; }

            // Map errors to specific openings
            foreach (var error in errors)
            {
                for (int i = 0; i < _doorVMs.Count; i++)
                {
                    if (error.Contains($"Door #{i + 1}"))
                    {
                        _doorVMs[i].ValidationStatus = "Error";
                        _doorVMs[i].ValidationMessage += (string.IsNullOrEmpty(_doorVMs[i].ValidationMessage) ? "" : "\n") + error;
                    }
                }
                for (int i = 0; i < _windowVMs.Count; i++)
                {
                    if (error.Contains($"Window #{i + 1}"))
                    {
                        _windowVMs[i].ValidationStatus = "Error";
                        _windowVMs[i].ValidationMessage += (string.IsNullOrEmpty(_windowVMs[i].ValidationMessage) ? "" : "\n") + error;
                    }
                }
            }

            // Also run per-opening basic validation from BarnParameters
            for (int i = 0; i < _doorVMs.Count; i++)
            {
                var d = _doorVMs[i].Model;
                if (d.Width <= 0 || d.Height <= 0)
                {
                    _doorVMs[i].ValidationStatus = "Error";
                    _doorVMs[i].ValidationMessage = "Width and height must be positive.";
                }
                else if (_parameters.EaveHeight > 0 && d.Height > _parameters.EaveHeight)
                {
                    _doorVMs[i].ValidationStatus = "Error";
                    _doorVMs[i].ValidationMessage = $"Door height ({d.Height}') exceeds eave height ({_parameters.EaveHeight}').";
                }
            }

            for (int i = 0; i < _windowVMs.Count; i++)
            {
                var w = _windowVMs[i].Model;
                if (w.Width <= 0 || w.Height <= 0)
                {
                    _windowVMs[i].ValidationStatus = "Error";
                    _windowVMs[i].ValidationMessage = "Width and height must be positive.";
                }
                else if (_parameters.EaveHeight > 0 && w.SillHeight + w.Height > _parameters.EaveHeight)
                {
                    _windowVMs[i].ValidationStatus = "Error";
                    _windowVMs[i].ValidationMessage = $"Window top ({w.SillHeight + w.Height}') exceeds eave height ({_parameters.EaveHeight}').";
                }
            }

            // Update detail panel validation display
            UpdateDetailValidation();
        }

        private void UpdateDetailValidation()
        {
            var selectedDoor = dgDoors.SelectedItem as DoorViewModel;
            if (selectedDoor != null)
            {
                if (!string.IsNullOrEmpty(selectedDoor.ValidationMessage))
                {
                    pnlDoorValidation.Visibility = Visibility.Visible;
                    txtDoorErrors.Text = selectedDoor.ValidationMessage;
                }
                else
                {
                    pnlDoorValidation.Visibility = Visibility.Collapsed;
                }
            }

            var selectedWindow = dgWindows.SelectedItem as WindowViewModel;
            if (selectedWindow != null)
            {
                if (!string.IsNullOrEmpty(selectedWindow.ValidationMessage))
                {
                    pnlWindowValidation.Visibility = Visibility.Visible;
                    txtWindowErrors.Text = selectedWindow.ValidationMessage;
                }
                else
                {
                    pnlWindowValidation.Visibility = Visibility.Collapsed;
                }
            }
        }

        // ─── Door Management ───

        private void OnAddDoor(object sender, RoutedEventArgs e)
        {
            var door = new DoorOpening
            {
                Type = DoorType.Walk, Wall = WallSide.Front,
                Width = 3, Height = 7, CenterOffset = 15
            };
            var vm = new DoorViewModel(door);
            _doorVMs.Add(vm);
            SyncToModel();
            dgDoors.SelectedItem = vm;
            ScheduleValidation();
        }

        private void OnDeleteDoor(object sender, RoutedEventArgs e)
        {
            if (dgDoors.SelectedItem is DoorViewModel vm)
            {
                _doorVMs.Remove(vm);
                SyncToModel();
                ShowNoSelection();
                ScheduleValidation();
            }
        }

        private void OnDuplicateDoor(object sender, RoutedEventArgs e)
        {
            if (dgDoors.SelectedItem is DoorViewModel vm)
            {
                var clone = OpeningPresets.CloneDoor(vm.Model);
                clone.CenterOffset += clone.Width + 1; // offset to avoid exact overlap
                var newVm = new DoorViewModel(clone);
                _doorVMs.Add(newVm);
                SyncToModel();
                dgDoors.SelectedItem = newVm;
                ScheduleValidation();
            }
        }

        private void OnDoorSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = dgDoors.SelectedItem as DoorViewModel;
            btnDeleteDoor.IsEnabled = vm != null;
            btnDupDoor.IsEnabled = vm != null;

            if (vm != null)
            {
                // Deselect windows
                dgWindows.SelectedItem = null;
                ShowDoorDetail(vm);
            }
            else if (dgWindows.SelectedItem == null)
            {
                ShowNoSelection();
            }
        }

        private void ShowDoorDetail(DoorViewModel vm)
        {
            _suppressPropertyEvents = true;

            txtNoSelection.Visibility = Visibility.Collapsed;
            pnlWindowDetail.Visibility = Visibility.Collapsed;
            pnlDoorDetail.Visibility = Visibility.Visible;

            // Set type combo
            cmbDoorType.SelectedIndex = vm.Model.Type switch
            {
                DoorType.Overhead => 0,
                DoorType.Sliding => 1,
                DoorType.Walk => 2,
                DoorType.Dutch => 3,
                DoorType.Double => 4,
                _ => 0
            };

            // Set wall combo
            cmbDoorWall.SelectedIndex = (int)vm.Model.Wall;

            txtDoorWidth.Text = vm.Model.Width.ToString();
            txtDoorHeight.Text = vm.Model.Height.ToString();
            txtDoorPosition.Text = vm.Model.CenterOffset.ToString();

            // Track type
            cmbTrackType.SelectedIndex = vm.Model.TrackType switch
            {
                TrackType.StandardLift => 0,
                TrackType.HighLift => 1,
                TrackType.VerticalLift => 2,
                _ => 0
            };

            // Swing options
            cmbSwingDir.SelectedIndex = vm.Model.SwingDirection == SwingDirection.Out ? 0 : 1;
            cmbHanding.SelectedIndex = vm.Model.HandingDirection == HandingDirection.Left ? 0 : 1;

            // Dutch
            txtSplitHeight.Text = vm.Model.SplitHeight.ToString();

            chkHasLite.IsChecked = vm.Model.HasLite;

            // Type-specific panel visibility
            UpdateDoorTypeOptions(vm.Model.Type);

            // Structural info
            txtDoorHeader.Text = $"Header: {vm.Model.HeaderSize}";
            txtDoorRough.Text = $"Rough Opening: {vm.Model.Width + 0.25}'W × {vm.Model.Height + 0.125}'H";

            // Validation
            if (!string.IsNullOrEmpty(vm.ValidationMessage))
            {
                pnlDoorValidation.Visibility = Visibility.Visible;
                txtDoorErrors.Text = vm.ValidationMessage;
            }
            else
            {
                pnlDoorValidation.Visibility = Visibility.Collapsed;
            }

            _suppressPropertyEvents = false;
        }

        private void UpdateDoorTypeOptions(DoorType type)
        {
            pnlOverheadOptions.Visibility = type == DoorType.Overhead ? Visibility.Visible : Visibility.Collapsed;
            pnlSwingOptions.Visibility = (type == DoorType.Walk || type == DoorType.Dutch || type == DoorType.Double)
                ? Visibility.Visible : Visibility.Collapsed;
            pnlDutchOptions.Visibility = type == DoorType.Dutch ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnDoorPropertyChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressPropertyEvents) return;
            if (dgDoors.SelectedItem is not DoorViewModel vm) return;

            var door = vm.Model;

            // Read type
            door.Type = cmbDoorType.SelectedIndex switch
            {
                0 => DoorType.Overhead,
                1 => DoorType.Sliding,
                2 => DoorType.Walk,
                3 => DoorType.Dutch,
                4 => DoorType.Double,
                _ => door.Type
            };

            // Read wall
            door.Wall = (WallSide)(cmbDoorWall.SelectedIndex >= 0 ? cmbDoorWall.SelectedIndex : 0);

            if (double.TryParse(txtDoorWidth.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w)) door.Width = w;
            if (double.TryParse(txtDoorHeight.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double h)) door.Height = h;
            if (double.TryParse(txtDoorPosition.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double p)) door.CenterOffset = p;

            door.TrackType = cmbTrackType.SelectedIndex switch
            {
                0 => TrackType.StandardLift,
                1 => TrackType.HighLift,
                2 => TrackType.VerticalLift,
                _ => door.TrackType
            };

            door.SwingDirection = cmbSwingDir.SelectedIndex == 0 ? SwingDirection.Out : SwingDirection.In;
            door.HandingDirection = cmbHanding.SelectedIndex == 0 ? HandingDirection.Left : HandingDirection.Right;

            if (double.TryParse(txtSplitHeight.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double sh)) door.SplitHeight = sh;
            door.HasLite = chkHasLite.IsChecked == true;

            UpdateDoorTypeOptions(door.Type);

            // Update structural info
            txtDoorHeader.Text = $"Header: {door.HeaderSize}";
            txtDoorRough.Text = $"Rough Opening: {door.Width + 0.25}'W × {door.Height + 0.125}'H";

            vm.Refresh();
            ScheduleValidation();
        }

        // Overload for TextChanged events
        private void OnDoorPropertyChanged(object sender, TextChangedEventArgs e)
        {
            OnDoorPropertyChanged(sender, (RoutedEventArgs)e);
        }

        // Overload for SelectionChanged events
        private void OnDoorPropertyChanged(object sender, SelectionChangedEventArgs e)
        {
            OnDoorPropertyChanged(sender, (RoutedEventArgs)e);
        }

        // ─── Window Management ───

        private void OnAddWindow(object sender, RoutedEventArgs e)
        {
            var window = new WindowOpening
            {
                Type = WindowType.SingleHung, Wall = WallSide.Left,
                Width = 3, Height = 4, SillHeight = 4, CenterOffset = 10
            };
            var vm = new WindowViewModel(window);
            _windowVMs.Add(vm);
            SyncToModel();
            dgWindows.SelectedItem = vm;
            ScheduleValidation();
        }

        private void OnDeleteWindow(object sender, RoutedEventArgs e)
        {
            if (dgWindows.SelectedItem is WindowViewModel vm)
            {
                _windowVMs.Remove(vm);
                SyncToModel();
                ShowNoSelection();
                ScheduleValidation();
            }
        }

        private void OnDuplicateWindow(object sender, RoutedEventArgs e)
        {
            if (dgWindows.SelectedItem is WindowViewModel vm)
            {
                var clone = OpeningPresets.CloneWindow(vm.Model);
                clone.CenterOffset += clone.Width + 1;
                var newVm = new WindowViewModel(clone);
                _windowVMs.Add(newVm);
                SyncToModel();
                dgWindows.SelectedItem = newVm;
                ScheduleValidation();
            }
        }

        private void OnWindowSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = dgWindows.SelectedItem as WindowViewModel;
            btnDeleteWindow.IsEnabled = vm != null;
            btnDupWindow.IsEnabled = vm != null;

            if (vm != null)
            {
                dgDoors.SelectedItem = null;
                ShowWindowDetail(vm);
            }
            else if (dgDoors.SelectedItem == null)
            {
                ShowNoSelection();
            }
        }

        private void ShowWindowDetail(WindowViewModel vm)
        {
            _suppressPropertyEvents = true;

            txtNoSelection.Visibility = Visibility.Collapsed;
            pnlDoorDetail.Visibility = Visibility.Collapsed;
            pnlWindowDetail.Visibility = Visibility.Visible;

            cmbWindowType.SelectedIndex = vm.Model.Type switch
            {
                WindowType.Fixed => 0,
                WindowType.SingleHung => 1,
                WindowType.Sliding => 2,
                WindowType.BarnSash => 3,
                WindowType.Awning => 4,
                WindowType.Casement => 5,
                _ => 0
            };

            cmbWindowWall.SelectedIndex = (int)vm.Model.Wall;

            txtWindowWidth.Text = vm.Model.Width.ToString();
            txtWindowHeight.Text = vm.Model.Height.ToString();
            txtWindowSill.Text = vm.Model.SillHeight.ToString();
            txtWindowPosition.Text = vm.Model.CenterOffset.ToString();

            chkWindowGrid.IsChecked = vm.Model.HasGrid;
            pnlGridPattern.Visibility = vm.Model.HasGrid ? Visibility.Visible : Visibility.Collapsed;

            cmbGridPattern.SelectedIndex = vm.Model.GridPattern switch
            {
                GridPattern.Colonial => 0,
                GridPattern.Prairie => 1,
                GridPattern.Custom => 2,
                _ => 0
            };

            if (!string.IsNullOrEmpty(vm.ValidationMessage))
            {
                pnlWindowValidation.Visibility = Visibility.Visible;
                txtWindowErrors.Text = vm.ValidationMessage;
            }
            else
            {
                pnlWindowValidation.Visibility = Visibility.Collapsed;
            }

            _suppressPropertyEvents = false;
        }

        private void OnWindowPropertyChanged(object sender, RoutedEventArgs e)
        {
            if (_suppressPropertyEvents) return;
            if (dgWindows.SelectedItem is not WindowViewModel vm) return;

            var win = vm.Model;

            win.Type = cmbWindowType.SelectedIndex switch
            {
                0 => WindowType.Fixed,
                1 => WindowType.SingleHung,
                2 => WindowType.Sliding,
                3 => WindowType.BarnSash,
                4 => WindowType.Awning,
                5 => WindowType.Casement,
                _ => win.Type
            };

            win.Wall = (WallSide)(cmbWindowWall.SelectedIndex >= 0 ? cmbWindowWall.SelectedIndex : 0);

            if (double.TryParse(txtWindowWidth.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double w)) win.Width = w;
            if (double.TryParse(txtWindowHeight.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double h)) win.Height = h;
            if (double.TryParse(txtWindowSill.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double s)) win.SillHeight = s;
            if (double.TryParse(txtWindowPosition.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double p)) win.CenterOffset = p;

            win.HasGrid = chkWindowGrid.IsChecked == true;
            pnlGridPattern.Visibility = win.HasGrid ? Visibility.Visible : Visibility.Collapsed;

            win.GridPattern = cmbGridPattern.SelectedIndex switch
            {
                0 => GridPattern.Colonial,
                1 => GridPattern.Prairie,
                2 => GridPattern.Custom,
                _ => win.GridPattern
            };

            vm.Refresh();
            ScheduleValidation();
        }

        private void OnWindowPropertyChanged(object sender, TextChangedEventArgs e)
        {
            OnWindowPropertyChanged(sender, (RoutedEventArgs)e);
        }

        private void OnWindowPropertyChanged(object sender, SelectionChangedEventArgs e)
        {
            OnWindowPropertyChanged(sender, (RoutedEventArgs)e);
        }

        // ─── Preset Handling ───

        private void OnDoorPresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key &&
                OpeningPresets.DoorPresets.TryGetValue(key, out var preset))
            {
                var door = OpeningPresets.CloneDoor(preset.Door);
                var vm = new DoorViewModel(door);
                _doorVMs.Add(vm);
                SyncToModel();
                dgDoors.SelectedItem = vm;
                ScheduleValidation();
            }
        }

        private void OnWindowPresetClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key &&
                OpeningPresets.WindowPresets.TryGetValue(key, out var preset))
            {
                var window = OpeningPresets.CloneWindow(preset.Window);
                var vm = new WindowViewModel(window);
                _windowVMs.Add(vm);
                SyncToModel();
                dgWindows.SelectedItem = vm;
                ScheduleValidation();
            }
        }

        // ─── UI Helpers ───

        private void ShowNoSelection()
        {
            pnlDoorDetail.Visibility = Visibility.Collapsed;
            pnlWindowDetail.Visibility = Visibility.Collapsed;
            pnlDoorValidation.Visibility = Visibility.Collapsed;
            pnlWindowValidation.Visibility = Visibility.Collapsed;
            txtNoSelection.Visibility = Visibility.Visible;
        }
    }
}
