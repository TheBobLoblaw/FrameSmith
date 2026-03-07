using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Generators.TrussProfiles;

namespace PoleBarnGenerator.UI
{
    public partial class PoleBarnDialog : Window
    {
        public BarnParameters Parameters { get; private set; }
        private BarnGeometry _cachedGeometry;
        private string _cachedGeometryKey;

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
            exteriorManager.BindParameters(Parameters);
            interiorManager.BindParameters(Parameters);
        }

        private void OnDimensionChanged(object sender, TextChangedEventArgs e)
        {
            if (double.TryParse(txtLength.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double len) &&
                double.TryParse(txtBaySpacing.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double spacing) &&
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

            // Advanced - multi-story
            p.NumberOfFloors = (int)ParseDouble(txtNumFloors.Text, 1);
            p.FloorHeights = ParseDoubleList(txtFloorHeights.Text);
            p.FloorConnection = cmbFloorConnection.SelectedIndex == 1
                ? FloorConnectionType.SplicedPost
                : FloorConnectionType.ContinuousPost;
            p.FloorBeamSize = txtFloorBeamSize.Text?.Trim();

            // Advanced - curved walls
            p.CurvedWall.Enabled = chkCurvedWalls.IsChecked == true;
            p.CurvedWall.Radius = ParseDouble(txtCurveRadius.Text, 120);
            p.CurvedWall.ArcAngleDegrees = ParseDouble(txtCurveAngle.Text, 45);
            p.CurvedWall.Mode = cmbCurveMode.SelectedIndex == 1
                ? CurvedWallMode.ChordDriven
                : CurvedWallMode.ArcLengthDriven;

            // Advanced - complex footprint
            p.FootprintShape = cmbFootprintShape.SelectedIndex switch
            {
                1 => FootprintShape.LShape,
                2 => FootprintShape.TShape,
                3 => FootprintShape.UShape,
                4 => FootprintShape.CustomPolygon,
                _ => FootprintShape.Rectangle
            };
            p.FootprintInsetWidth = ParseDouble(txtFootprintInsetWidth.Text, 10);
            p.FootprintInsetDepth = ParseDouble(txtFootprintInsetDepth.Text, 10);
            p.FootprintVertices = ParseVertices(txtFootprintVertices.Text);

            // Advanced - expansion joints
            p.ExpansionJoint.Enabled = chkExpansionJoints.IsChecked == true;
            p.ExpansionJoint.Locations = ParseDoubleList(txtJointLocations.Text);
            p.ExpansionJoint.GapWidth = ParseDouble(txtJointGap.Text, 0.5);
            p.ExpansionJoint.JointType = cmbJointType.SelectedIndex switch
            {
                1 => ExpansionJointType.DoublePost,
                2 => ExpansionJointType.IsolationGap,
                _ => ExpansionJointType.SlipPlate
            };

            // Industry specialization - dairy
            p.DairyBarn.IsEnabled = chkDairyEnabled.IsChecked == true;
            p.DairyBarn.ParlorType = cmbParlorType.SelectedIndex switch
            {
                1 => MilkingParlorType.Parallel,
                2 => MilkingParlorType.Rotary,
                _ => MilkingParlorType.Herringbone
            };
            p.DairyBarn.HerdSize = (int)ParseDouble(txtHerdSize.Text, 120);

            // Industry specialization - equipment storage
            p.EquipmentStorage.IsEnabled = chkEquipStorageEnabled.IsChecked == true;
            p.EquipmentStorage.RequireClearSpan = chkClearSpan.IsChecked == true;
            p.EquipmentStorage.CraneRail.IsEnabled = chkCraneRail.IsChecked == true;
            p.EquipmentStorage.CraneRail.RailHeight = ParseDouble(txtCraneRailHeight.Text, 16);
            p.EquipmentStorage.CraneRail.CapacityTons = ParseDouble(txtCraneCapacity.Text, 5);
            p.EquipmentStorage.LargeDoor.IsEnabled = chkLargeDoor.IsChecked == true;
            p.EquipmentStorage.LargeDoor.DoorType = cmbLargeDoorType.SelectedIndex == 1
                ? LargeDoorType.Hydraulic
                : LargeDoorType.Bifold;
            p.EquipmentStorage.LargeDoor.Width = ParseDouble(txtLargeDoorWidth.Text, 30);
            p.EquipmentStorage.LargeDoor.Height = ParseDouble(txtLargeDoorHeight.Text, 16);

            // Industry specialization - agricultural
            p.GrainStorage.IsEnabled = chkGrainStorage.IsChecked == true;
            p.GrainStorage.BinPadCount = (int)ParseDouble(txtBinPadCount.Text, 2);
            p.GrainStorage.BinPadDiameter = ParseDouble(txtBinPadDiameter.Text, 24);
            p.MachineryBuilding.IsEnabled = chkMachineryBuilding.IsChecked == true;
            p.MachineryBuilding.ClearSpanBayWidth = ParseDouble(txtMachineryBayWidth.Text, 40);
            p.MachineryBuilding.PreferredEaveHeight = ParseDouble(txtMachineryEaveHeight.Text, 20);

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

            // Sync lean-to and exterior settings from UI
            leanToManager.SyncToParameters();
            exteriorManager.SyncToParameters();
            interiorManager.SyncToParameters();

            var (isValid, error) = Parameters.Validate();
            if (!isValid)
            {
                MessageBox.Show(error, "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                // Switch to Openings tab if it's an opening error
                if (error.Contains("Porch"))
                    tabMain.SelectedIndex = 4;
                else if (error.Contains("Lean-to"))
                    tabMain.SelectedIndex = 3;
                else if (error.Contains("Door") || error.Contains("Window") || error.Contains("overlap"))
                    tabMain.SelectedIndex = 2;
                return;
            }

            // Show summary
            var geo = GetOrCreateGeometry(Parameters);
            txtSummary.Text =
                $"Building:  {Parameters.BuildingWidth}' W x {Parameters.BuildingLength}' L\n" +
                $"Eave:      {Parameters.EaveHeight}'   Peak: {geo.PeakHeight:F1}'\n" +
                $"Pitch:     {Parameters.RoofPitchDisplay}  ({Parameters.RoofAngleDegrees:F1} deg)\n" +
                $"Bays:      {geo.NumBays} @ {geo.ActualBaySpacing:F1}' O.C.\n" +
                $"Stories:   {Parameters.NumberOfFloors} ({(Parameters.FloorHeights.Count > 0 ? string.Join(", ", Parameters.FloorHeights.Select(h => $"{h:F1}'")) : "auto")})\n" +
                $"Posts:     {geo.Posts.Count}\n" +
                $"Girt rows: {geo.Girts.Count}\n" +
                $"Purlins:   {geo.Purlins.Count}\n" +
                $"Post size: {Parameters.PostSize}\n" +
                $"Doors:     {Parameters.Doors.Count}\n" +
                $"Windows:   {Parameters.Windows.Count}\n" +
                $"Lean-Tos:  {Parameters.LeanTos.FindAll(lt => lt.Enabled).Count}\n" +
                $"Porches:   {System.Array.FindAll(Parameters.AllPorches, p => p.IsEnabled).Length}\n" +
                $"Footprint: {Parameters.FootprintShape}\n" +
                $"Curved:    {(Parameters.CurvedWall.Enabled ? $"R={Parameters.CurvedWall.Radius:F1}', A={Parameters.CurvedWall.ArcAngleDegrees:F1} deg" : "No")}\n" +
                $"Joints:    {(geo.ExpansionJoints.Count > 0 ? geo.ExpansionJoints.Count.ToString() : "None")}\n" +
                $"Dairy:     {(Parameters.DairyBarn.IsEnabled ? $"{Parameters.DairyBarn.ParlorType}, Herd {Parameters.DairyBarn.HerdSize}" : "No")}\n" +
                $"Equipment: {(Parameters.EquipmentStorage.IsEnabled ? "Yes" : "No")}\n" +
                $"Ag Spec:   {(Parameters.GrainStorage.IsEnabled || Parameters.MachineryBuilding.IsEnabled ? "Yes" : "No")}\n" +
                $"Wainscot:  {(Parameters.Wainscot.IsEnabled ? "Yes" : "No")}\n" +
                $"Cupolas:   {(Parameters.Cupola.IsEnabled ? Parameters.Cupola.Count.ToString() : "No")}\n" +
                $"Gutters:   {(Parameters.Gutters.IsEnabled ? "Yes" : "No")}";

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

            txtNumFloors.Text = p.NumberOfFloors.ToString(CultureInfo.InvariantCulture);
            txtFloorHeights.Text = p.FloorHeights?.Count > 0
                ? string.Join(", ", p.FloorHeights.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)))
                : string.Empty;
            cmbFloorConnection.SelectedIndex = p.FloorConnection == FloorConnectionType.SplicedPost ? 1 : 0;
            txtFloorBeamSize.Text = p.FloorBeamSize;

            chkCurvedWalls.IsChecked = p.CurvedWall.Enabled;
            txtCurveRadius.Text = p.CurvedWall.Radius.ToString("0.##", CultureInfo.InvariantCulture);
            txtCurveAngle.Text = p.CurvedWall.ArcAngleDegrees.ToString("0.##", CultureInfo.InvariantCulture);
            cmbCurveMode.SelectedIndex = p.CurvedWall.Mode == CurvedWallMode.ChordDriven ? 1 : 0;

            cmbFootprintShape.SelectedIndex = p.FootprintShape switch
            {
                FootprintShape.LShape => 1,
                FootprintShape.TShape => 2,
                FootprintShape.UShape => 3,
                FootprintShape.CustomPolygon => 4,
                _ => 0
            };
            txtFootprintInsetWidth.Text = p.FootprintInsetWidth.ToString("0.##", CultureInfo.InvariantCulture);
            txtFootprintInsetDepth.Text = p.FootprintInsetDepth.ToString("0.##", CultureInfo.InvariantCulture);
            txtFootprintVertices.Text = p.FootprintVertices?.Count > 0
                ? string.Join(", ", p.FootprintVertices.Select(v => $"{v.X:0.##}:{v.Y:0.##}"))
                : string.Empty;

            chkExpansionJoints.IsChecked = p.ExpansionJoint.Enabled;
            txtJointLocations.Text = p.ExpansionJoint.Locations?.Count > 0
                ? string.Join(", ", p.ExpansionJoint.Locations.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)))
                : string.Empty;
            txtJointGap.Text = p.ExpansionJoint.GapWidth.ToString("0.##", CultureInfo.InvariantCulture);
            cmbJointType.SelectedIndex = p.ExpansionJoint.JointType switch
            {
                ExpansionJointType.DoublePost => 1,
                ExpansionJointType.IsolationGap => 2,
                _ => 0
            };

            chkDairyEnabled.IsChecked = p.DairyBarn.IsEnabled;
            cmbParlorType.SelectedIndex = p.DairyBarn.ParlorType switch
            {
                MilkingParlorType.Parallel => 1,
                MilkingParlorType.Rotary => 2,
                _ => 0
            };
            txtHerdSize.Text = p.DairyBarn.HerdSize.ToString(CultureInfo.InvariantCulture);

            chkEquipStorageEnabled.IsChecked = p.EquipmentStorage.IsEnabled;
            chkClearSpan.IsChecked = p.EquipmentStorage.RequireClearSpan;
            chkCraneRail.IsChecked = p.EquipmentStorage.CraneRail.IsEnabled;
            txtCraneRailHeight.Text = p.EquipmentStorage.CraneRail.RailHeight.ToString("0.##", CultureInfo.InvariantCulture);
            txtCraneCapacity.Text = p.EquipmentStorage.CraneRail.CapacityTons.ToString("0.##", CultureInfo.InvariantCulture);
            chkLargeDoor.IsChecked = p.EquipmentStorage.LargeDoor.IsEnabled;
            cmbLargeDoorType.SelectedIndex = p.EquipmentStorage.LargeDoor.DoorType == LargeDoorType.Hydraulic ? 1 : 0;
            txtLargeDoorWidth.Text = p.EquipmentStorage.LargeDoor.Width.ToString("0.##", CultureInfo.InvariantCulture);
            txtLargeDoorHeight.Text = p.EquipmentStorage.LargeDoor.Height.ToString("0.##", CultureInfo.InvariantCulture);

            chkGrainStorage.IsChecked = p.GrainStorage.IsEnabled;
            txtBinPadCount.Text = p.GrainStorage.BinPadCount.ToString(CultureInfo.InvariantCulture);
            txtBinPadDiameter.Text = p.GrainStorage.BinPadDiameter.ToString("0.##", CultureInfo.InvariantCulture);
            chkMachineryBuilding.IsChecked = p.MachineryBuilding.IsEnabled;
            txtMachineryBayWidth.Text = p.MachineryBuilding.ClearSpanBayWidth.ToString("0.##", CultureInfo.InvariantCulture);
            txtMachineryEaveHeight.Text = p.MachineryBuilding.PreferredEaveHeight.ToString("0.##", CultureInfo.InvariantCulture);

            // Update the Parameters instance and rebind opening manager
            Parameters = p;
            openingManager.BindParameters(Parameters);
            leanToManager.BindParameters(Parameters);
            exteriorManager.BindParameters(Parameters);
            interiorManager.BindParameters(Parameters);
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
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val)
                ? val
                : fallback;
        }

        private static List<double> ParseDoubleList(string csv)
        {
            var values = new List<double>();
            if (string.IsNullOrWhiteSpace(csv))
            {
                return values;
            }

            foreach (var token in csv.Split(','))
            {
                if (double.TryParse(token.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    values.Add(value);
                }
            }

            return values;
        }

        private static List<FootprintVertex> ParseVertices(string csv)
        {
            var vertices = new List<FootprintVertex>();
            if (string.IsNullOrWhiteSpace(csv))
            {
                return vertices;
            }

            foreach (var entry in csv.Split(','))
            {
                var parts = entry.Trim().Split(':');
                if (parts.Length != 2)
                {
                    continue;
                }

                if (double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                    double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                {
                    vertices.Add(new FootprintVertex { X = x, Y = y });
                }
            }

            return vertices;
        }

        private void OnSuggestJoints(object sender, RoutedEventArgs e)
        {
            var len = ParseDouble(txtLength.Text, 40);
            var temp = new BarnParameters { BuildingLength = len };
            var suggested = temp.GetSuggestedExpansionJointLocations();
            txtJointLocations.Text = suggested.Count > 0
                ? string.Join(", ", suggested.Select(v => v.ToString("0.##", CultureInfo.InvariantCulture)))
                : string.Empty;
            chkExpansionJoints.IsChecked = suggested.Count > 0;
        }

        protected override void OnClosed(EventArgs e)
        {
            txtLength.TextChanged -= OnDimensionChanged;
            txtBaySpacing.TextChanged -= OnDimensionChanged;
            cmbTrussType.SelectionChanged -= OnTrussTypeChanged;
            base.OnClosed(e);
        }

        private BarnGeometry GetOrCreateGeometry(BarnParameters parameters)
        {
            string key = BuildGeometryCacheKey(parameters);
            if (_cachedGeometry != null && string.Equals(_cachedGeometryKey, key, StringComparison.Ordinal))
            {
                return _cachedGeometry;
            }

            _cachedGeometry = new BarnGeometry(parameters);
            _cachedGeometryKey = key;
            return _cachedGeometry;
        }

        private static string BuildGeometryCacheKey(BarnParameters p)
        {
            var sb = new StringBuilder();
            sb.Append("W=").Append(p.BuildingWidth.ToString("R", CultureInfo.InvariantCulture))
                .Append("|L=").Append(p.BuildingLength.ToString("R", CultureInfo.InvariantCulture))
                .Append("|E=").Append(p.EaveHeight.ToString("R", CultureInfo.InvariantCulture))
                .Append("|Pitch=").Append(p.RoofPitchRise.ToString("R", CultureInfo.InvariantCulture))
                .Append("|Bay=").Append(p.BaySpacing.ToString("R", CultureInfo.InvariantCulture))
                .Append("|Truss=").Append((int)p.TrussType)
                .Append("|Floors=").Append(p.NumberOfFloors)
                .Append("|Footprint=").Append((int)p.FootprintShape)
                .Append("|InsetW=").Append(p.FootprintInsetWidth.ToString("R", CultureInfo.InvariantCulture))
                .Append("|InsetD=").Append(p.FootprintInsetDepth.ToString("R", CultureInfo.InvariantCulture))
                .Append("|Curved=").Append(p.CurvedWall.Enabled ? 1 : 0)
                .Append("|CurveR=").Append(p.CurvedWall.Radius.ToString("R", CultureInfo.InvariantCulture))
                .Append("|CurveA=").Append(p.CurvedWall.ArcAngleDegrees.ToString("R", CultureInfo.InvariantCulture))
                .Append("|CurveM=").Append((int)p.CurvedWall.Mode)
                .Append("|JointEnabled=").Append(p.ExpansionJoint.Enabled ? 1 : 0)
                .Append("|JointGap=").Append(p.ExpansionJoint.GapWidth.ToString("R", CultureInfo.InvariantCulture))
                .Append("|JointType=").Append((int)p.ExpansionJoint.JointType);

            foreach (var h in p.GetResolvedFloorHeights())
            {
                sb.Append("|FH=").Append(h.ToString("R", CultureInfo.InvariantCulture));
            }

            foreach (var v in p.FootprintVertices ?? Enumerable.Empty<FootprintVertex>())
            {
                sb.Append("|V=").Append(v.X.ToString("R", CultureInfo.InvariantCulture))
                    .Append(":").Append(v.Y.ToString("R", CultureInfo.InvariantCulture));
            }

            foreach (var loc in p.ExpansionJoint.Locations ?? Enumerable.Empty<double>())
            {
                sb.Append("|J=").Append(loc.ToString("R", CultureInfo.InvariantCulture));
            }

            return sb.ToString();
        }
    }
}
