using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.UI
{
    public partial class LeanToControl : UserControl
    {
        private BarnParameters _parameters;

        public LeanToControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Bind to the main BarnParameters instance.
        /// </summary>
        public void BindParameters(BarnParameters parameters)
        {
            _parameters = parameters;

            // If parameters already have lean-tos, populate the UI
            foreach (var lt in _parameters.LeanTos)
            {
                if (!lt.Enabled) continue;
                switch (lt.AttachmentWall)
                {
                    case WallSide.Left:
                        chkLeftEnabled.IsChecked = true;
                        txtLeftWidth.Text = lt.Width.ToString();
                        txtLeftEaveHeight.Text = lt.EaveHeight.ToString();
                        txtLeftPitch.Text = lt.RoofPitch.ToString();
                        txtLeftStart.Text = lt.StartPosition.ToString();
                        txtLeftEnd.Text = lt.EndPosition.ToString();
                        cmbLeftType.SelectedIndex = (int)lt.Type;
                        gridLeft.Visibility = Visibility.Visible;
                        break;
                    case WallSide.Right:
                        chkRightEnabled.IsChecked = true;
                        txtRightWidth.Text = lt.Width.ToString();
                        txtRightEaveHeight.Text = lt.EaveHeight.ToString();
                        txtRightPitch.Text = lt.RoofPitch.ToString();
                        txtRightStart.Text = lt.StartPosition.ToString();
                        txtRightEnd.Text = lt.EndPosition.ToString();
                        cmbRightType.SelectedIndex = (int)lt.Type;
                        gridRight.Visibility = Visibility.Visible;
                        break;
                    case WallSide.Front:
                        chkFrontEnabled.IsChecked = true;
                        txtFrontWidth.Text = lt.Width.ToString();
                        txtFrontEaveHeight.Text = lt.EaveHeight.ToString();
                        txtFrontPitch.Text = lt.RoofPitch.ToString();
                        txtFrontStart.Text = lt.StartPosition.ToString();
                        txtFrontEnd.Text = lt.EndPosition.ToString();
                        cmbFrontType.SelectedIndex = (int)lt.Type;
                        gridFront.Visibility = Visibility.Visible;
                        break;
                    case WallSide.Back:
                        chkBackEnabled.IsChecked = true;
                        txtBackWidth.Text = lt.Width.ToString();
                        txtBackEaveHeight.Text = lt.EaveHeight.ToString();
                        txtBackPitch.Text = lt.RoofPitch.ToString();
                        txtBackStart.Text = lt.StartPosition.ToString();
                        txtBackEnd.Text = lt.EndPosition.ToString();
                        cmbBackType.SelectedIndex = (int)lt.Type;
                        gridBack.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void OnLeanToChanged(object sender, RoutedEventArgs e)
        {
            // Toggle visibility of detail grids
            if (gridLeft != null) gridLeft.Visibility = chkLeftEnabled.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (gridRight != null) gridRight.Visibility = chkRightEnabled.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (gridFront != null) gridFront.Visibility = chkFrontEnabled.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (gridBack != null) gridBack.Visibility = chkBackEnabled.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Reads the UI state and populates BarnParameters.LeanTos.
        /// Called before generation.
        /// </summary>
        public void SyncToParameters()
        {
            if (_parameters == null) return;

            _parameters.LeanTos.Clear();

            if (chkLeftEnabled.IsChecked == true)
            {
                _parameters.LeanTos.Add(ReadLeanTo(WallSide.Left,
                    txtLeftWidth, txtLeftEaveHeight, txtLeftPitch,
                    txtLeftStart, txtLeftEnd, cmbLeftType));
            }

            if (chkRightEnabled.IsChecked == true)
            {
                _parameters.LeanTos.Add(ReadLeanTo(WallSide.Right,
                    txtRightWidth, txtRightEaveHeight, txtRightPitch,
                    txtRightStart, txtRightEnd, cmbRightType));
            }

            if (chkFrontEnabled.IsChecked == true)
            {
                _parameters.LeanTos.Add(ReadLeanTo(WallSide.Front,
                    txtFrontWidth, txtFrontEaveHeight, txtFrontPitch,
                    txtFrontStart, txtFrontEnd, cmbFrontType));
            }

            if (chkBackEnabled.IsChecked == true)
            {
                _parameters.LeanTos.Add(ReadLeanTo(WallSide.Back,
                    txtBackWidth, txtBackEaveHeight, txtBackPitch,
                    txtBackStart, txtBackEnd, cmbBackType));
            }
        }

        private LeanToParameters ReadLeanTo(WallSide wall,
            TextBox widthBox, TextBox eaveBox, TextBox pitchBox,
            TextBox startBox, TextBox endBox, ComboBox typeCombo)
        {
            return new LeanToParameters
            {
                Enabled = true,
                AttachmentWall = wall,
                Width = ParseDouble(widthBox.Text, 12),
                EaveHeight = ParseDouble(eaveBox.Text, 10),
                RoofPitch = ParseDouble(pitchBox.Text, 3),
                StartPosition = ParseDouble(startBox.Text, 0),
                EndPosition = ParseDouble(endBox.Text, 0),
                Type = (LeanToType)typeCombo.SelectedIndex,
                EnclosedWalls = typeCombo.SelectedIndex == 2
                    ? new bool[] { true, true, true }
                    : typeCombo.SelectedIndex == 1
                        ? new bool[] { true, false, false }
                        : new bool[] { false, false, false }
            };
        }

        private static double ParseDouble(string text, double fallback)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double val)
                ? val
                : fallback;
        }
    }
}
