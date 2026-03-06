using System.Windows.Controls;
using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.UI
{
    /// <summary>
    /// Code-behind for the Interior Features tab control.
    /// Data binding handles most interaction via BarnParameters properties.
    /// </summary>
    public partial class InteriorControl : UserControl
    {
        private BarnParameters _parameters;

        public InteriorControl()
        {
            InitializeComponent();
        }

        public void BindParameters(BarnParameters parameters)
        {
            _parameters = parameters;
            DataContext = parameters;

            chkVentEnabled.IsChecked = _parameters.Ventilation.IsEnabled;
            chkRidgeVent.IsChecked = _parameters.Ventilation.RidgeVentEnabled;
            chkCupolas.IsChecked = _parameters.Ventilation.CupolaCount > 0;
            chkWallLouvers.IsChecked = _parameters.Ventilation.WallLouverCount > 0;
            txtCupolaCount.Text = _parameters.Ventilation.CupolaCount.ToString();
            txtLouverCount.Text = _parameters.Ventilation.WallLouverCount.ToString();

            chkDrainEnabled.IsChecked = _parameters.Drainage.IsEnabled;
            chkFrenchDrain.IsChecked = _parameters.Drainage.FrenchDrainEnabled;
            txtFloorSlope.Text = _parameters.Drainage.FloorSlopePercent.ToString();
        }

        public void SyncToParameters()
        {
            if (_parameters == null) return;

            _parameters.Ventilation.IsEnabled = chkVentEnabled.IsChecked == true;
            _parameters.Ventilation.RidgeVentEnabled = chkRidgeVent.IsChecked == true;
            _parameters.Ventilation.CupolaCount = chkCupolas.IsChecked == true
                ? ParseInt(txtCupolaCount.Text, 1)
                : 0;
            _parameters.Ventilation.WallLouverCount = chkWallLouvers.IsChecked == true
                ? ParseInt(txtLouverCount.Text, 4)
                : 0;

            _parameters.Drainage.IsEnabled = chkDrainEnabled.IsChecked == true;
            _parameters.Drainage.FrenchDrainEnabled = chkFrenchDrain.IsChecked == true;
            _parameters.Drainage.FloorSlopePercent = ParseDouble(txtFloorSlope.Text, 1.5);
        }

        private static int ParseInt(string text, int fallback)
        {
            return int.TryParse(text, out int value) ? value : fallback;
        }

        private static double ParseDouble(string text, double fallback)
        {
            return double.TryParse(text, out double value) ? value : fallback;
        }
    }
}
