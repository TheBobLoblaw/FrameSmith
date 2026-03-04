using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.Models.Analysis;
using PoleBarnGenerator.Models.Materials;
using PoleBarnGenerator.Models.Pricing;
using PoleBarnGenerator.Models.Suppliers;
using PoleBarnGenerator.Generators;

namespace PoleBarnGenerator.UI
{
    public partial class MaterialsControl : UserControl
    {
        private MaterialTakeoff _takeoff;
        private PricingResult _pricing;
        private CutList _cutList;
        private StructuralDesignResult _structuralResult;

        public MaterialsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Calculate materials from geometry and optional structural results.
        /// Called externally from the main dialog or internally from the button.
        /// </summary>
        public void RunTakeoff(BarnGeometry geometry, StructuralDesignResult structural = null)
        {
            _structuralResult = structural;

            try
            {
                _takeoff = MaterialTakeoffEngine.RunFullTakeoff(geometry, structural);

                // Apply default pricing to lumber items
                _pricing = MaterialPricingEngine.CalculateProjectCost(_takeoff);

                // Bind UI
                dgLumber.ItemsSource = _takeoff.LumberItems;
                dgHardware.ItemsSource = _takeoff.HardwareItems;

                // Summary
                var sb = new StringBuilder();
                sb.AppendLine($"Project: {_takeoff.ProjectDescription}");
                sb.AppendLine();
                sb.AppendLine($"  Lumber:     {_takeoff.Lumber.TotalBoardFeet:F0} board feet  " +
                    $"(+{_takeoff.Lumber.WasteFactor:P0} waste = {_takeoff.Lumber.TotalWithWaste:F0} BF)");
                sb.AppendLine($"  Hardware:   {_takeoff.Hardware.AllItems.Count} line items");
                sb.AppendLine($"  Roofing:    {_takeoff.Roofing.TotalRoofArea:F0} sq ft");
                sb.AppendLine($"  Siding:     {_takeoff.Siding.NetWallArea:F0} sq ft (net)");
                sb.AppendLine($"  Foundation: {_takeoff.Foundation.TotalConcreteYards:F2} cubic yards concrete");
                sb.AppendLine();
                sb.AppendLine($"  TOTAL MATERIAL COST: ${_takeoff.TotalMaterialCost:N2}");

                txtSummary.Text = sb.ToString();
                txtStatus.Text = $"✅ Calculated {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                txtStatus.Text = $"❌ Error: {ex.Message}";
                MessageBox.Show($"Material calculation error:\n{ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCalculate_Click(object sender, RoutedEventArgs e)
        {
            // Try to get parameters from parent dialog's DataContext or walk up tree
            var parameters = DataContext as BarnParameters;
            if (parameters == null)
            {
                // Try parent window
                var parent = Window.GetWindow(this);
                if (parent is PoleBarnDialog dlg)
                    parameters = dlg.Parameters;
            }

            if (parameters == null)
            {
                MessageBox.Show("No building parameters available. Configure dimensions first.",
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var geometry = new BarnGeometry(parameters);

            // Run structural analysis if available
            StructuralDesignResult structural = _structuralResult;
            if (structural == null && parameters.IncludeStructuralAnalysis)
            {
                try
                {
                    structural = StructuralEngine.RunFullAnalysis(geometry, parameters.Structural);
                }
                catch { /* proceed without structural */ }
            }

            RunTakeoff(geometry, structural);
        }

        private void BtnPrice_Click(object sender, RoutedEventArgs e)
        {
            if (_takeoff == null)
            {
                MessageBox.Show("Calculate materials first.", "Info");
                return;
            }

            string zip = txtZipCode.Text?.Trim() ?? "43001";
            var tier = cmbTier.SelectedIndex switch
            {
                0 => PricingTier.Budget,
                2 => PricingTier.Premium,
                _ => PricingTier.Standard
            };

            double taxRate = 0.07;
            if (double.TryParse(txtTaxRate.Text, out double tr))
                taxRate = tr / 100.0;

            _pricing = MaterialPricingEngine.CalculateProjectCost(_takeoff, zip, tier, taxRate);

            var sb = new StringBuilder();
            sb.AppendLine($"  Materials:     ${_pricing.MaterialCosts.TotalCategoryCost,12:N2}");
            sb.AppendLine($"    Lumber:      ${_pricing.MaterialCosts.LumberCost,12:N2}");
            sb.AppendLine($"    Hardware:    ${_pricing.MaterialCosts.HardwareCost,12:N2}");
            sb.AppendLine($"    Roofing:     ${_pricing.MaterialCosts.RoofingCost,12:N2}");
            sb.AppendLine($"    Siding:      ${_pricing.MaterialCosts.SidingCost,12:N2}");
            sb.AppendLine($"    Foundation:  ${_pricing.MaterialCosts.FoundationCost,12:N2}");
            sb.AppendLine();
            sb.AppendLine($"  Labor:         ${_pricing.LaborCosts.TotalCategoryCost,12:N2}");
            sb.AppendLine($"  Equipment:     ${_pricing.EquipmentCosts.TotalCategoryCost,12:N2}");
            sb.AppendLine($"  Other:         ${_pricing.OtherCosts.TotalCategoryCost,12:N2}");
            sb.AppendLine($"  ─────────────────────────────────────");
            sb.AppendLine($"  Subtotal:      ${_pricing.SubtotalCost,12:N2}");
            sb.AppendLine($"  Sales Tax:     ${_pricing.SalesTax,12:N2}");
            sb.AppendLine($"  Contingency:   ${_pricing.ContingencyCost,12:N2}");
            sb.AppendLine($"  ═══════════════════════════════════════");
            sb.AppendLine($"  TOTAL PROJECT: ${_pricing.TotalProjectCost,12:N2}");

            txtPricing.Text = sb.ToString();
        }

        private void BtnQuotes_Click(object sender, RoutedEventArgs e)
        {
            if (_takeoff == null)
            {
                MessageBox.Show("Calculate materials first.", "Info");
                return;
            }

            var quotes = SupplierIntegration.GetEstimatedQuotes(_takeoff, txtZipCode.Text?.Trim());
            dgQuotes.ItemsSource = quotes;
        }

        private void BtnCutList_Click(object sender, RoutedEventArgs e)
        {
            if (_takeoff == null)
            {
                MessageBox.Show("Calculate materials first.", "Info");
                return;
            }

            _cutList = CutListGenerator.Generate(_takeoff.Lumber);
            dgCutList.ItemsSource = _cutList.Items;

            txtCutStats.Text = $"Waste: {_cutList.WastePercentage:P1}  |  " +
                $"Optimization: {_cutList.OptimizationScore:P1}  |  " +
                $"Est. Savings: ${_cutList.CostSavings:N2}";

            // Waste analysis
            var waste = WasteCalculator.Calculate(_takeoff);
            var sb = new StringBuilder();
            sb.AppendLine($"  Projected Waste Cost:  ${waste.ProjectedWasteCost:N2}");
            sb.AppendLine($"  Salvage Value:         ${waste.SalvageValue:N2}");
            sb.AppendLine($"  Net Waste Cost:        ${waste.NetWasteCost:N2}");
            sb.AppendLine();
            sb.AppendLine("  Recommendations:");
            foreach (var rec in waste.Recommendations)
                sb.AppendLine($"    [{rec.Difficulty}] {rec.Category}: {rec.Recommendation} " +
                    $"(save ~${rec.PotentialSavings:N0})");
            txtWaste.Text = sb.ToString();
        }

        private void BtnExportCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_takeoff == null) return;

            var dlg = new SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv",
                FileName = "MaterialTakeoff.csv"
            };
            if (dlg.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dlg.FileName,
                    MaterialReportGenerator.GenerateCsv(_takeoff));
                MessageBox.Show($"Exported to {dlg.FileName}", "Export Complete");
            }
        }

        private void BtnExportReport_Click(object sender, RoutedEventArgs e)
        {
            if (_takeoff == null) return;

            var dlg = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                FileName = "MaterialReport.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dlg.FileName,
                    MaterialReportGenerator.GenerateMaterialList(_takeoff, _pricing));
                MessageBox.Show($"Exported to {dlg.FileName}", "Export Complete");
            }
        }

        private void BtnExportRfq_Click(object sender, RoutedEventArgs e)
        {
            if (_takeoff == null) return;

            var dlg = new SaveFileDialog
            {
                Filter = "Text files (*.txt)|*.txt",
                FileName = "RFQ.txt"
            };
            if (dlg.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(dlg.FileName,
                    MaterialReportGenerator.GenerateRfq(_takeoff));
                MessageBox.Show($"Exported to {dlg.FileName}", "RFQ Generated");
            }
        }
    }
}
