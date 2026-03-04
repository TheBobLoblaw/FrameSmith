using System;
using System.Collections.Generic;
using System.Linq;
using PoleBarnGenerator.Models.Materials;

namespace PoleBarnGenerator.Models.Suppliers
{
    public class SupplierProvider
    {
        public string Name { get; set; } = "";
        public string ContactInfo { get; set; } = "";
        public string Website { get; set; } = "";
        public SupplierType Type { get; set; }
        public double MarkupFactor { get; set; } = 1.0;
        public double DeliveryBaseFee { get; set; } = 150.0;
        public double DeliveryPerMile { get; set; } = 3.50;
        public int LeadTimeDays { get; set; } = 3;
        public bool HasContractorPricing { get; set; }
    }

    public enum SupplierType { BigBox, LumberYard, Specialty, Local }

    public class SupplierQuote
    {
        public string SupplierName { get; set; } = "";
        public string ContactInfo { get; set; } = "";
        public double MaterialTotal { get; set; }
        public double DeliveryFee { get; set; }
        public double TotalPrice { get; set; }
        public DateTime QuoteDate { get; set; } = DateTime.UtcNow;
        public DateTime ExpirationDate { get; set; }
        public DateTime EarliestDelivery { get; set; }
        public List<QuoteLineItem> LineItems { get; set; } = new();
        public List<string> Terms { get; set; } = new();
        public List<string> Substitutions { get; set; } = new();
        public bool IncludesInstallation { get; set; }
        public double InstallationCost { get; set; }
    }

    public class QuoteLineItem
    {
        public string Description { get; set; } = "";
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double ExtendedPrice { get; set; }
        public bool InStock { get; set; } = true;
        public string AlternateProduct { get; set; } = "";
    }

    /// <summary>
    /// Generates supplier quote estimates based on material takeoffs.
    /// Uses built-in supplier profiles with typical pricing differentials.
    /// </summary>
    public static class SupplierIntegration
    {
        public static readonly List<SupplierProvider> DefaultSuppliers = new()
        {
            new SupplierProvider
            {
                Name = "84 Lumber",
                ContactInfo = "1-800-359-8484",
                Website = "https://www.84lumber.com",
                Type = SupplierType.LumberYard,
                MarkupFactor = 0.95, // competitive on lumber
                DeliveryBaseFee = 125,
                LeadTimeDays = 3,
                HasContractorPricing = true
            },
            new SupplierProvider
            {
                Name = "Menards",
                ContactInfo = "1-800-880-6318",
                Website = "https://www.menards.com",
                Type = SupplierType.BigBox,
                MarkupFactor = 1.0,
                DeliveryBaseFee = 79,
                LeadTimeDays = 5,
                HasContractorPricing = true
            },
            new SupplierProvider
            {
                Name = "Home Depot Pro",
                ContactInfo = "1-800-466-3337",
                Website = "https://www.homedepot.com/c/Pro",
                Type = SupplierType.BigBox,
                MarkupFactor = 1.05,
                DeliveryBaseFee = 99,
                LeadTimeDays = 5,
                HasContractorPricing = true
            },
            new SupplierProvider
            {
                Name = "Lowe's Pro Supply",
                ContactInfo = "1-800-445-6937",
                Website = "https://www.lowes.com/l/Pro",
                Type = SupplierType.BigBox,
                MarkupFactor = 1.03,
                DeliveryBaseFee = 89,
                LeadTimeDays = 5,
                HasContractorPricing = true
            },
            new SupplierProvider
            {
                Name = "Local Metal Building Supply",
                ContactInfo = "(555) 123-4567",
                Type = SupplierType.Specialty,
                MarkupFactor = 0.90, // best on metal
                DeliveryBaseFee = 200,
                LeadTimeDays = 7,
                HasContractorPricing = false
            }
        };

        /// <summary>
        /// Generate estimated quotes from multiple suppliers.
        /// </summary>
        public static List<SupplierQuote> GetEstimatedQuotes(MaterialTakeoff takeoff,
            string deliveryZipCode = "", List<SupplierProvider> suppliers = null)
        {
            suppliers ??= DefaultSuppliers;
            var quotes = new List<SupplierQuote>();

            double baseMaterialCost = takeoff.TotalMaterialCost;

            foreach (var supplier in suppliers)
            {
                double materialTotal = baseMaterialCost * supplier.MarkupFactor;

                // Volume discount for larger orders
                if (materialTotal > 15000) materialTotal *= 0.97; // 3% volume discount
                if (materialTotal > 30000) materialTotal *= 0.95; // additional 5%

                // Contractor pricing discount
                if (supplier.HasContractorPricing) materialTotal *= 0.95;

                var quote = new SupplierQuote
                {
                    SupplierName = supplier.Name,
                    ContactInfo = supplier.ContactInfo,
                    MaterialTotal = Math.Round(materialTotal, 2),
                    DeliveryFee = supplier.DeliveryBaseFee,
                    TotalPrice = Math.Round(materialTotal + supplier.DeliveryBaseFee, 2),
                    QuoteDate = DateTime.UtcNow,
                    ExpirationDate = DateTime.UtcNow.AddDays(30),
                    EarliestDelivery = DateTime.UtcNow.AddDays(supplier.LeadTimeDays),
                    Terms = new List<string>
                    {
                        "Net 30 with approved credit",
                        "2% discount for payment within 10 days",
                        "Returns accepted within 30 days (restocking fee may apply)"
                    }
                };

                // Generate line items summary
                quote.LineItems.Add(new QuoteLineItem
                {
                    Description = "Lumber package",
                    Quantity = 1,
                    UnitPrice = takeoff.Lumber.TotalCost * supplier.MarkupFactor,
                    ExtendedPrice = takeoff.Lumber.TotalCost * supplier.MarkupFactor
                });
                quote.LineItems.Add(new QuoteLineItem
                {
                    Description = "Hardware package",
                    Quantity = 1,
                    UnitPrice = takeoff.Hardware.TotalHardwareCost * supplier.MarkupFactor,
                    ExtendedPrice = takeoff.Hardware.TotalHardwareCost * supplier.MarkupFactor
                });
                quote.LineItems.Add(new QuoteLineItem
                {
                    Description = "Roofing materials",
                    Quantity = 1,
                    UnitPrice = takeoff.Roofing.TotalRoofingCost * supplier.MarkupFactor,
                    ExtendedPrice = takeoff.Roofing.TotalRoofingCost * supplier.MarkupFactor
                });
                quote.LineItems.Add(new QuoteLineItem
                {
                    Description = "Siding & trim",
                    Quantity = 1,
                    UnitPrice = takeoff.Siding.TotalSidingCost * supplier.MarkupFactor,
                    ExtendedPrice = takeoff.Siding.TotalSidingCost * supplier.MarkupFactor
                });
                quote.LineItems.Add(new QuoteLineItem
                {
                    Description = "Foundation materials",
                    Quantity = 1,
                    UnitPrice = takeoff.Foundation.TotalFoundationCost * supplier.MarkupFactor,
                    ExtendedPrice = takeoff.Foundation.TotalFoundationCost * supplier.MarkupFactor
                });

                quotes.Add(quote);
            }

            return quotes.OrderBy(q => q.TotalPrice).ToList();
        }
    }
}
