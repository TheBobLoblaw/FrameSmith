using System;
using System.Windows;
using System.Windows.Controls;
using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.UI
{
    public partial class ExteriorControl : UserControl
    {
        private BarnParameters _params;

        public ExteriorControl()
        {
            InitializeComponent();
        }

        public void BindParameters(BarnParameters parameters)
        {
            _params = parameters;

            // Sync UI from parameters
            chkFrontPorch.IsChecked = _params.FrontPorch.IsEnabled;
            txtFrontDepth.Text = _params.FrontPorch.Depth.ToString();
            txtFrontPitch.Text = _params.FrontPorch.RoofPitch.ToString();
            txtFrontColSpacing.Text = _params.FrontPorch.ColumnSpacing.ToString();
            cmbFrontColType.SelectedIndex = (int)_params.FrontPorch.ColumnType;
            chkFrontRailing.IsChecked = _params.FrontPorch.HasRailing;

            chkBackPorch.IsChecked = _params.BackPorch.IsEnabled;
            txtBackDepth.Text = _params.BackPorch.Depth.ToString();
            txtBackPitch.Text = _params.BackPorch.RoofPitch.ToString();
            cmbBackColType.SelectedIndex = (int)_params.BackPorch.ColumnType;
            chkBackRailing.IsChecked = _params.BackPorch.HasRailing;

            chkLeftPorch.IsChecked = _params.LeftPorch.IsEnabled;
            txtLeftDepth.Text = _params.LeftPorch.Depth.ToString();
            txtLeftPitch.Text = _params.LeftPorch.RoofPitch.ToString();
            chkLeftRailing.IsChecked = _params.LeftPorch.HasRailing;

            chkRightPorch.IsChecked = _params.RightPorch.IsEnabled;
            txtRightDepth.Text = _params.RightPorch.Depth.ToString();
            txtRightPitch.Text = _params.RightPorch.RoofPitch.ToString();
            chkRightRailing.IsChecked = _params.RightPorch.HasRailing;

            chkWainscot.IsChecked = _params.Wainscot.IsEnabled;
            txtWainscotHeight.Text = _params.Wainscot.Height.ToString();
            cmbWainscotMaterial.SelectedIndex = (int)_params.Wainscot.Material;
            chkWainFront.IsChecked = _params.Wainscot.Walls[0];
            chkWainBack.IsChecked = _params.Wainscot.Walls[1];
            chkWainLeft.IsChecked = _params.Wainscot.Walls[2];
            chkWainRight.IsChecked = _params.Wainscot.Walls[3];

            chkCupola.IsChecked = _params.Cupola.IsEnabled;
            txtCupolaSize.Text = _params.Cupola.Size.ToString();
            txtCupolaCount.Text = _params.Cupola.Count.ToString();
            chkCupolaVented.IsChecked = _params.Cupola.IsVented;
            chkCupolaWindows.IsChecked = _params.Cupola.HasWindows;

            chkGutters.IsChecked = _params.Gutters.IsEnabled;
            cmbGutterStyle.SelectedIndex = (int)_params.Gutters.Style;
            chkGutterFront.IsChecked = _params.Gutters.Eaves[0];
            chkGutterBack.IsChecked = _params.Gutters.Eaves[1];
            chkGutterLeft.IsChecked = _params.Gutters.Eaves[2];
            chkGutterRight.IsChecked = _params.Gutters.Eaves[3];
        }

        /// <summary>Sync UI values back to BarnParameters before generation</summary>
        public void SyncToParameters()
        {
            if (_params == null) return;

            // Front Porch
            _params.FrontPorch.IsEnabled = chkFrontPorch.IsChecked == true;
            _params.FrontPorch.Depth = ParseDouble(txtFrontDepth.Text, 8);
            _params.FrontPorch.RoofPitch = ParseDouble(txtFrontPitch.Text, 3);
            _params.FrontPorch.ColumnSpacing = ParseDouble(txtFrontColSpacing.Text, 8);
            _params.FrontPorch.ColumnType = (ColumnType)(cmbFrontColType.SelectedIndex);
            _params.FrontPorch.HasRailing = chkFrontRailing.IsChecked == true;

            // Back Porch
            _params.BackPorch.IsEnabled = chkBackPorch.IsChecked == true;
            _params.BackPorch.Depth = ParseDouble(txtBackDepth.Text, 8);
            _params.BackPorch.RoofPitch = ParseDouble(txtBackPitch.Text, 3);
            _params.BackPorch.ColumnType = (ColumnType)(cmbBackColType.SelectedIndex);
            _params.BackPorch.HasRailing = chkBackRailing.IsChecked == true;

            // Left Porch
            _params.LeftPorch.IsEnabled = chkLeftPorch.IsChecked == true;
            _params.LeftPorch.Depth = ParseDouble(txtLeftDepth.Text, 8);
            _params.LeftPorch.RoofPitch = ParseDouble(txtLeftPitch.Text, 3);
            _params.LeftPorch.HasRailing = chkLeftRailing.IsChecked == true;

            // Right Porch
            _params.RightPorch.IsEnabled = chkRightPorch.IsChecked == true;
            _params.RightPorch.Depth = ParseDouble(txtRightDepth.Text, 8);
            _params.RightPorch.RoofPitch = ParseDouble(txtRightPitch.Text, 3);
            _params.RightPorch.HasRailing = chkRightRailing.IsChecked == true;

            // Wainscot
            _params.Wainscot.IsEnabled = chkWainscot.IsChecked == true;
            _params.Wainscot.Height = ParseDouble(txtWainscotHeight.Text, 3.5);
            _params.Wainscot.Material = (WainscotMaterial)(cmbWainscotMaterial.SelectedIndex);
            _params.Wainscot.Walls[0] = chkWainFront.IsChecked == true;
            _params.Wainscot.Walls[1] = chkWainBack.IsChecked == true;
            _params.Wainscot.Walls[2] = chkWainLeft.IsChecked == true;
            _params.Wainscot.Walls[3] = chkWainRight.IsChecked == true;

            // Cupolas
            _params.Cupola.IsEnabled = chkCupola.IsChecked == true;
            _params.Cupola.Size = ParseDouble(txtCupolaSize.Text, 24);
            _params.Cupola.Count = (int)ParseDouble(txtCupolaCount.Text, 1);
            _params.Cupola.IsVented = chkCupolaVented.IsChecked == true;
            _params.Cupola.HasWindows = chkCupolaWindows.IsChecked == true;

            // Gutters
            _params.Gutters.IsEnabled = chkGutters.IsChecked == true;
            _params.Gutters.Style = (GutterStyle)(cmbGutterStyle.SelectedIndex);
            _params.Gutters.Eaves[0] = chkGutterFront.IsChecked == true;
            _params.Gutters.Eaves[1] = chkGutterBack.IsChecked == true;
            _params.Gutters.Eaves[2] = chkGutterLeft.IsChecked == true;
            _params.Gutters.Eaves[3] = chkGutterRight.IsChecked == true;
        }

        private static double ParseDouble(string text, double fallback)
        {
            return double.TryParse(text, out double val) ? val : fallback;
        }
    }
}
