using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Models.Analysis;
using PoleBarnGenerator.Generators;

namespace PoleBarnGenerator.UI
{
    public partial class StructuralControl : UserControl
    {
        private StructuralDesignResult _lastResult;

        public StructuralControl()
        {
            InitializeComponent();
        }

        private void BtnRunAnalysis_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var parameters = DataContext as BarnParameters;
                if (parameters == null)
                {
                    txtResults.Text = "Error: No building parameters available.";
                    return;
                }

                var geometry = new BarnGeometry(parameters);
                _lastResult = StructuralEngine.RunFullAnalysis(geometry, parameters.Structural);

                txtResults.Text = _lastResult.DesignSummary;
                btnExportReport.IsEnabled = true;
            }
            catch (Exception ex)
            {
                txtResults.Text = $"Analysis Error: {ex.Message}\n\n{ex.StackTrace}";
                btnExportReport.IsEnabled = false;
            }
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (_lastResult == null) return;

            try
            {
                var parameters = DataContext as BarnParameters;
                var geometry = new BarnGeometry(parameters);

                var dlg = new SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = ".txt",
                    FileName = $"FrameSmith_Calcs_{parameters.BuildingWidth}x{parameters.BuildingLength}_{DateTime.Now:yyyyMMdd}.txt"
                };

                if (dlg.ShowDialog() == true)
                {
                    string report = EngineeringReportGenerator.GenerateCalculationReport(
                        geometry, _lastResult);
                    System.IO.File.WriteAllText(dlg.FileName, report);
                    MessageBox.Show($"Calculation package exported to:\n{dlg.FileName}",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export Error: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
