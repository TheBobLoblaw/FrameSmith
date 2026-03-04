using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PoleBarnGenerator.Models.Materials;
using PoleBarnGenerator.Models.Pricing;
using PoleBarnGenerator.Models.Suppliers;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Generates professional material reports, purchase orders, and RFQ packages.
    /// Outputs CSV format for easy import into spreadsheets.
    /// </summary>
    public static class MaterialReportGenerator
    {
        /// <summary>
        /// Generate a complete material list organized by CSI MasterFormat divisions.
        /// </summary>
        public static string GenerateMaterialList(MaterialTakeoff takeoff,
            PricingResult pricing = null)
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("                    MATERIAL TAKEOFF REPORT");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  Project: {takeoff.ProjectDescription}");
            sb.AppendLine($"  Date:    {takeoff.CalculationDate:yyyy-MM-dd HH:mm} UTC");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine();

            // Division 03: Concrete
            sb.AppendLine("─── DIVISION 03: CONCRETE ────────────────────────────────────");
            sb.AppendLine();
            foreach (var item in takeoff.Foundation.Concrete)
                sb.AppendLine($"  {item.Quantity,4}  {item.Description,-50} {item.Volume:F2} CY  ${item.TotalPrice:N2}");
            foreach (var item in takeoff.Foundation.Reinforcement)
                sb.AppendLine($"  {item.Quantity,4}  {item.Description,-50} {item.TotalWeight:F0} lbs  ${item.TotalPrice:N2}");
            foreach (var item in takeoff.Foundation.AnchorBolts)
                sb.AppendLine($"  {item.Quantity,4}  {item.Description,-50}          ${item.TotalCost:N2}");
            sb.AppendLine($"  {"",4}  {"Subtotal — Foundation",-50}          ${takeoff.Foundation.TotalFoundationCost:N2}");
            sb.AppendLine();

            // Division 06: Wood & Plastics
            sb.AppendLine("─── DIVISION 06: WOOD, PLASTICS & COMPOSITES ──────────────────");
            sb.AppendLine();
            sb.AppendLine("  Qty   Description                                        Bd Ft    Total");
            sb.AppendLine("  ────  ─────────────────────────────────────────────────  ───────  ──────────");
            foreach (var item in takeoff.Lumber.AllItems)
                sb.AppendLine($"  {item.Quantity,4}  {item.Description,-50} {item.BoardFeet,7:F1}  ${item.TotalPrice,9:N2}");

            sb.AppendLine($"  {"",4}  {"Total Board Feet",-50} {takeoff.Lumber.TotalBoardFeet,7:F1}");
            sb.AppendLine($"  {"",4}  {$"Waste Factor ({takeoff.Lumber.WasteFactor:P0})",-50} {takeoff.Lumber.TotalWithWaste,7:F1}");
            sb.AppendLine($"  {"",4}  {"Subtotal — Lumber",-50}          ${takeoff.Lumber.TotalCost:N2}");
            sb.AppendLine();

            // Division 05: Metals (hardware)
            sb.AppendLine("─── DIVISION 05: METALS (HARDWARE) ───────────────────────────");
            sb.AppendLine();
            foreach (var item in takeoff.Hardware.AllItems)
                sb.AppendLine($"  {item.Quantity,4}  {item.Description,-50}          ${item.TotalCost:N2}");
            sb.AppendLine($"  {"",4}  {"Subtotal — Hardware",-50}          ${takeoff.Hardware.TotalHardwareCost:N2}");
            sb.AppendLine();

            // Division 07: Thermal & Moisture Protection
            sb.AppendLine("─── DIVISION 07: THERMAL & MOISTURE PROTECTION ────────────────");
            sb.AppendLine();
            sb.AppendLine("  ROOFING:");
            foreach (var item in takeoff.Roofing.MetalPanels.Concat(takeoff.Roofing.RidgeCap)
                .Concat(takeoff.Roofing.EaveTrim).Concat(takeoff.Roofing.RakeTrim)
                .Concat(takeoff.Roofing.Fasteners).Concat(takeoff.Roofing.Accessories))
                sb.AppendLine($"  {item.Quantity,4}  {item.Description,-50}          ${item.TotalPrice:N2}");
            sb.AppendLine($"  {"",4}  {"Subtotal — Roofing",-50}          ${takeoff.Roofing.TotalRoofingCost:N2}");
            sb.AppendLine();

            sb.AppendLine("  SIDING & TRIM:");
            foreach (var item in takeoff.Siding.WallPanels.Concat(takeoff.Siding.CornerTrim)
                .Concat(takeoff.Siding.BaseTrim).Concat(takeoff.Siding.JChannel)
                .Concat(takeoff.Siding.Fasteners).Concat(takeoff.Siding.Flashing))
                sb.AppendLine($"  {item.Quantity,4}  {item.Description,-50}          ${item.TotalPrice:N2}");
            sb.AppendLine($"  {"",4}  {"Subtotal — Siding",-50}          ${takeoff.Siding.TotalSidingCost:N2}");
            sb.AppendLine();

            // Grand total
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  {"",4}  {"TOTAL MATERIAL COST",-50}          ${takeoff.TotalMaterialCost:N2}");

            if (pricing != null)
            {
                sb.AppendLine();
                sb.AppendLine("─── PROJECT COST ESTIMATE ────────────────────────────────────");
                sb.AppendLine($"  Materials:     ${pricing.MaterialCosts.TotalCategoryCost,12:N2}");
                sb.AppendLine($"  Labor:         ${pricing.LaborCosts.TotalCategoryCost,12:N2}");
                sb.AppendLine($"  Equipment:     ${pricing.EquipmentCosts.TotalCategoryCost,12:N2}");
                sb.AppendLine($"  Other:         ${pricing.OtherCosts.TotalCategoryCost,12:N2}");
                sb.AppendLine($"  ─────────────────────────────────────");
                sb.AppendLine($"  Subtotal:      ${pricing.SubtotalCost,12:N2}");
                sb.AppendLine($"  Sales Tax:     ${pricing.SalesTax,12:N2}");
                sb.AppendLine($"  Contingency:   ${pricing.ContingencyCost,12:N2}");
                sb.AppendLine($"  ═════════════════════════════════════");
                sb.AppendLine($"  TOTAL PROJECT: ${pricing.TotalProjectCost,12:N2}");
                sb.AppendLine();
                sb.AppendLine("  Assumptions:");
                foreach (var a in pricing.Assumptions)
                    sb.AppendLine($"    • [{a.Category}] {a.Description}");
            }

            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            return sb.ToString();
        }

        /// <summary>
        /// Export material takeoff to CSV for spreadsheet import.
        /// </summary>
        public static string GenerateCsv(MaterialTakeoff takeoff)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Category,Description,Size,Grade,Length,Quantity,Linear Feet,Board Feet,Usage,Unit Price,Total Price");

            foreach (var item in takeoff.Lumber.AllItems)
            {
                sb.AppendLine($"\"{item.Category}\",\"{item.Description}\",\"{item.Size}\",\"{item.Grade}\"," +
                    $"{item.Length},{item.Quantity},{item.LinearFeet:F1},{item.BoardFeet:F1}," +
                    $"\"{item.Usage}\",{item.UnitPrice:F2},{item.TotalPrice:F2}");
            }

            sb.AppendLine();
            sb.AppendLine("Category,Part Number,Description,Specification,Quantity,Unit Cost,Total Cost,Usage");
            foreach (var item in takeoff.Hardware.AllItems)
            {
                sb.AppendLine($"\"{item.Category}\",\"{item.PartNumber}\",\"{item.Description}\"," +
                    $"\"{item.Specification}\",{item.Quantity},{item.UnitCost:F2},{item.TotalCost:F2},\"{item.Usage}\"");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generate a supplier RFQ (Request for Quote) package.
        /// </summary>
        public static string GenerateRfq(MaterialTakeoff takeoff, string projectName = "",
            string contactName = "", string deliveryAddress = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine("              REQUEST FOR QUOTATION (RFQ)");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");
            sb.AppendLine($"  Date:     {DateTime.UtcNow:yyyy-MM-dd}");
            sb.AppendLine($"  Project:  {(string.IsNullOrEmpty(projectName) ? takeoff.ProjectDescription : projectName)}");
            if (!string.IsNullOrEmpty(contactName))
                sb.AppendLine($"  Contact:  {contactName}");
            if (!string.IsNullOrEmpty(deliveryAddress))
                sb.AppendLine($"  Deliver:  {deliveryAddress}");
            sb.AppendLine($"  Due Date: {DateTime.UtcNow.AddDays(14):yyyy-MM-dd}");
            sb.AppendLine();
            sb.AppendLine("  Please provide pricing for the following materials:");
            sb.AppendLine("  ─────────────────────────────────────────────────────────");
            sb.AppendLine();

            sb.AppendLine("  LUMBER:");
            sb.AppendLine($"  {"Qty",5}  {"Description",-45}  {"Unit Price",10}  {"Total",10}");
            foreach (var item in takeoff.Lumber.AllItems)
                sb.AppendLine($"  {item.Quantity,5}  {item.Description,-45}  {"_________",10}  {"_________",10}");

            sb.AppendLine();
            sb.AppendLine("  HARDWARE:");
            foreach (var item in takeoff.Hardware.AllItems)
                sb.AppendLine($"  {item.Quantity,5}  {item.Description,-45}  {"_________",10}  {"_________",10}");

            sb.AppendLine();
            sb.AppendLine("  ROOFING:");
            foreach (var item in takeoff.Roofing.MetalPanels.Concat(takeoff.Roofing.RidgeCap)
                .Concat(takeoff.Roofing.EaveTrim).Concat(takeoff.Roofing.RakeTrim)
                .Concat(takeoff.Roofing.Fasteners))
                sb.AppendLine($"  {item.Quantity,5}  {item.Description,-45}  {"_________",10}  {"_________",10}");

            sb.AppendLine();
            sb.AppendLine("  SIDING & TRIM:");
            foreach (var item in takeoff.Siding.WallPanels.Concat(takeoff.Siding.CornerTrim)
                .Concat(takeoff.Siding.BaseTrim).Concat(takeoff.Siding.JChannel)
                .Concat(takeoff.Siding.Fasteners))
                sb.AppendLine($"  {item.Quantity,5}  {item.Description,-45}  {"_________",10}  {"_________",10}");

            sb.AppendLine();
            sb.AppendLine("  ─────────────────────────────────────────────────────────");
            sb.AppendLine("  Delivery Fee:      _____________");
            sb.AppendLine("  Total Quote:       _____________");
            sb.AppendLine("  Quote Valid Until:  _____________");
            sb.AppendLine("  Lead Time:         _____________");
            sb.AppendLine();
            sb.AppendLine("  Terms: Net 30 preferred. Please note any substitutions.");
            sb.AppendLine("═══════════════════════════════════════════════════════════════");

            return sb.ToString();
        }
    }
}
