using System;
using System.Windows;
using System.Windows.Controls;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Generators.TrussProfiles;

namespace PoleBarnGenerator.UI
{
    public partial class PoleBarnDialog : Window
    {
        public BarnParameters Parameters { get; private set; }

        public PoleBarnDialog()
        {
            InitializeComponent();
            Parameters = new BarnParameters();

            // Wire up live bay count update
            txtLength.TextChanged += OnDimensionChanged;
            txtBaySpacing.TextChanged += OnDimensionChanged;

            // Bind opening manager to parameters
            openingManager.BindParameters(Parameters);
            leanToManager.BindParameters(Parameters);
            leanToManager.BindParameters(Parameters);
        }

        private void OnDimensionChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtLength.Text, out double len) &&
                double.TryParse(txtBaySpacing.Text, out double spacing) &&
                spacing > 0)
            {
                int bays = Math.Max(1, (int)Math.Round(len / spacing));
                lblBayCount.Content = $"{bays} (actual: {len / bays:F1}' O.C.)";
            }
        }

        private BarnParameters ReadFromDialog()
        {
            var p = Parameters; // Use the same instance so openings are preserved

            p.BuildingWidth = ParseDouble(txtWidth.Text, 30);
            p.BuildingLength = ParseDouble(txtLength.Text, 40);
            p.EaveHeight = ParseDouble(txtEaveHeight.Text, 12);
            p.RoofPitchRise = ParseDouble(txtPitch.Text, 4);
            p.BaySpacing = ParseDouble(txtBaySpacing.Text, 10);

            // Structural
            p.PostSize = (cmbPostSize.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "6x6";
            p.GirtSpacing = ParseDouble(txtGirtSpacing.Text, 24);
            p.PurlinSpacing = ParseDouble(txtPurlinSpacing.Text, 24);
            p.OverhangEave = ParseDouble(txtOverhangEave.Text, 1);
            p.OverhangGable = ParseDouble(txtOverhangGable.Text, 1);

            p.TrussType = cmbTrussType.SelectedIndex switch
            {
                1 => TrussType.Scissor,
                2 => TrussType.MonoSlope,
                3 => TrussType.Gambrel,
                4 => TrussType.Monitor,
                5 => TrussType.Attic,
                _ => TrussType.Common
            };

            // Output options
            p.GeneratePlan = chkPlan.IsChecked == true;
            p.GenerateFront = chkFront.IsChecked == true;
            p.GenerateSide = chkSide.IsChecked == true;
            p.Generate3D = chk3D.IsChecked == true;
            p.AddDimensions = chkDims.IsChecked == true;

            // Openings are already synced via OpeningManagerControl
            return p;
        }

        private void OnGenerate(object sender, RoutedEventArgs e)
        {
            Parameters = ReadFromDialog();

            // Sync lean-to settings from UI
            leanToManager.SyncToParameters();

            var (isValid, error) = Parameters.Validate();
            if (!isValid)
            {
                MessageBox.Show(error, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Switch to Openings tab if it's an opening error
                if (error.Contains("Lean-to"))
                    tabMain.SelectedIndex = 3;
                else if (error.Contains("Door") || error.Contains("Window") || error.Contains("overlap"))
                    tabMain.SelectedIndex = 2;
                return;
            }

            // Show summary
            var geo = new BarnGeometry(Parameters);
            txtSummary.Text =
                $"Building:  {Parameters.BuildingWidth}' W × {Parameters.BuildingLength}' L\n" +
                $"Eave:      {Parameters.EaveHeight}'   Peak: {geo.PeakHeight:F1}'\n" +
                $"Pitch:     {Parameters.RoofPitchDisplay}  ({Parameters.RoofAngleDegrees:F1}°)\n" +
                $"Bays:      {geo.NumBays} @ {geo.ActualBaySpacing:F1}' O.C.\n" +
                $"Posts:     {geo.Posts.Count}\n" +
                $"Girt rows: {geo.Girts.Count}\n" +
                $"Purlins:   {geo.Purlins.Count}\n" +
                $"Post size: {Parameters.PostSize}\n" +
                $"Doors:     {Parameters.Doors.Count}\n" +
                $"Windows:   {Parameters.Windows.Count}\n" +
                $"Lean-Tos:  {Parameters.LeanTos.FindAll(lt => lt.Enabled).Count}";

            DialogResult = true;
            Close();
        }

        private void OnCancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnLoadPreset(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            foreach (var name in new[] { "24x30", "30x40", "40x60", "50x80", "60x100" })
            {
                var item = new MenuItem { Header = name };
                string preset = name;
                item.Click += (s, args) =>
                {
                    var p = BarnParameters.CreatePreset(preset);
                    ApplyToDialog(p);
                    StatusText.Text = $"Loaded preset: {preset}";
                };
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        private void ApplyToDialog(BarnParameters p)
        {
            txtWidth.Text = p.BuildingWidth.ToString();
            txtLength.Text = p.BuildingLength.ToString();
            txtEaveHeight.Text = p.EaveHeight.ToString();
            txtPitch.Text = p.RoofPitchRise.ToString();
            txtBaySpacing.Text = p.BaySpacing.ToString();
            txtGirtSpacing.Text = p.GirtSpacing.ToString();
            txtPurlinSpacing.Text = p.PurlinSpacing.ToString();
            txtOverhangEave.Text = p.OverhangEave.ToString();
            txtOverhangGable.Text = p.OverhangGable.ToString();

            // Update the Parameters instance and rebind opening manager
            Parameters = p;
            openingManager.BindParameters(Parameters);
            leanToManager.BindParameters(Parameters);
            leanToManager.BindParameters(Parameters);
        }

        private void OnTrussTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtTrussDescription == null) return;
            var trussType = cmbTrussType.SelectedIndex switch
            {
                1 => TrussType.Scissor,
                2 => TrussType.MonoSlope,
                3 => TrussType.Gambrel,
                4 => TrussType.Monitor,
                5 => TrussType.Attic,
                _ => TrussType.Common
            };
            var profile = TrussFactory.GetTrussProfile(trussType);
            txtTrussDescription.Text = profile.Description;
        }

        private static double ParseDouble(string text, double fallback)
        {
            return double.TryParse(text, out double val) ? val : fallback;
        }
    }
}
