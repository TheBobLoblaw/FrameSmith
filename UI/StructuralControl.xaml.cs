using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.AutoCAD.ApplicationServices;
using Microsoft.Win32;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Models.Analysis;
using PoleBarnGenerator.Generators;

namespace PoleBarnGenerator.UI
{
    public partial class StructuralControl : UserControl
    {
        private StructuralDesignResult _lastResult;

        /// <summary>Last structural analysis result, accessible for material takeoffs.</summary>
        public StructuralDesignResult LastResult => _lastResult;

        /// <summary>Fired when structural analysis completes successfully.</summary>
        public event EventHandler<StructuralDesignResult> AnalysisCompleted;

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
                AnalysisCompleted?.Invoke(this, _lastResult);
            }
            catch (Exception ex)
            {
                txtResults.Text = "Analysis failed. Review inputs and try again.";
                MessageBox.Show(
                    "Structural analysis failed. Please verify your inputs and try again.",
                    "Analysis Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.DocumentManager.MdiActiveDocument?.Editor?
                    .WriteMessage($"\nStructural analysis error:\n{ex}");
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
                MessageBox.Show(
                    "Failed to export the calculation package. Please check the selected path and try again.",
                    "Export Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Application.DocumentManager.MdiActiveDocument?.Editor?
                    .WriteMessage($"\nStructural export error:\n{ex}");
            }
        }
    }
}
