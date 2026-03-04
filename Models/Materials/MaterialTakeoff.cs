using System;
using System.Collections.Generic;
using System.Linq;

namespace PoleBarnGenerator.Models.Materials
{
    /// <summary>
    /// Master material takeoff aggregating all material categories.
    /// </summary>
    public class MaterialTakeoff
    {
        public LumberTakeoff Lumber { get; set; } = new();
        public HardwareTakeoff Hardware { get; set; } = new();
        public RoofingTakeoff Roofing { get; set; } = new();
        public SidingTakeoff Siding { get; set; } = new();
        public FoundationTakeoff Foundation { get; set; } = new();
        public DateTime CalculationDate { get; set; } = DateTime.UtcNow;
        public string ProjectDescription { get; set; } = "";

        public List<LumberItem> LumberItems => Lumber.AllItems;
        public List<HardwareItem> HardwareItems => Hardware.AllItems;

        public double TotalMaterialCost =>
            Lumber.TotalCost + Hardware.TotalHardwareCost +
            Roofing.TotalRoofingCost + Siding.TotalSidingCost +
            Foundation.TotalFoundationCost;
    }

    // ─── Lumber ───────────────────────────────────────────

    public enum LumberCategory
    {
        Posts, Girts, Trusses, Purlins, Headers, Plates, Blocking, Miscellaneous
    }

    public class LumberItem
    {
        public string Description { get; set; } = "";
        public string Size { get; set; } = "";
        public string Grade { get; set; } = "";
        public double Length { get; set; }
        public int Quantity { get; set; }
        public double LinearFeet { get; set; }
        public double BoardFeet { get; set; }
        public LumberCategory Category { get; set; }
        public string Usage { get; set; } = "";
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
    }

    public class LumberTakeoff
    {
        public List<LumberItem> Posts { get; set; } = new();
        public List<LumberItem> Girts { get; set; } = new();
        public List<LumberItem> Trusses { get; set; } = new();
        public List<LumberItem> Purlins { get; set; } = new();
        public List<LumberItem> Headers { get; set; } = new();
        public List<LumberItem> Plates { get; set; } = new();
        public List<LumberItem> Blocking { get; set; } = new();
        public double TotalBoardFeet { get; set; }
        public double WasteFactor { get; set; } = 0.10;
        public double TotalWithWaste { get; set; }
        public double TotalCost { get; set; }

        public List<LumberItem> AllItems =>
            Posts.Concat(Girts).Concat(Trusses).Concat(Purlins)
                 .Concat(Headers).Concat(Plates).Concat(Blocking).ToList();
    }

    // ─── Hardware ─────────────────────────────────────────

    public enum HardwareCategory
    {
        StructuralBolts, LagBolts, ConnectorHardware, TrussPlates,
        Fasteners, PurlinClips, AnchorBolts, Miscellaneous
    }

    public class HardwareItem
    {
        public string PartNumber { get; set; } = "";
        public string Description { get; set; } = "";
        public string Specification { get; set; } = "";
        public int Quantity { get; set; }
        public double UnitCost { get; set; }
        public double TotalCost { get; set; }
        public string Supplier { get; set; } = "";
        public HardwareCategory Category { get; set; }
        public string Usage { get; set; } = "";
        public bool RequiresSpecialOrder { get; set; }
    }

    public class HardwareTakeoff
    {
        public List<HardwareItem> StructuralBolts { get; set; } = new();
        public List<HardwareItem> LagBolts { get; set; } = new();
        public List<HardwareItem> ConnectorHardware { get; set; } = new();
        public List<HardwareItem> TrussPlates { get; set; } = new();
        public List<HardwareItem> Fasteners { get; set; } = new();
        public List<HardwareItem> PurlinClips { get; set; } = new();
        public List<HardwareItem> AnchorBolts { get; set; } = new();
        public List<HardwareItem> Miscellaneous { get; set; } = new();
        public double TotalHardwareCost { get; set; }

        public List<HardwareItem> AllItems =>
            StructuralBolts.Concat(LagBolts).Concat(ConnectorHardware)
                .Concat(TrussPlates).Concat(Fasteners).Concat(PurlinClips)
                .Concat(AnchorBolts).Concat(Miscellaneous).ToList();
    }

    // ─── Roofing ──────────────────────────────────────────

    public class RoofingItem
    {
        public string ProductCode { get; set; } = "";
        public string Description { get; set; } = "";
        public double Coverage { get; set; }
        public double Length { get; set; }
        public int Quantity { get; set; }
        public double SquareFeet { get; set; }
        public string Color { get; set; } = "Galvalume";
        public string Manufacturer { get; set; } = "";
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
    }

    public class RoofingTakeoff
    {
        public List<RoofingItem> MetalPanels { get; set; } = new();
        public List<RoofingItem> RidgeCap { get; set; } = new();
        public List<RoofingItem> EaveTrim { get; set; } = new();
        public List<RoofingItem> RakeTrim { get; set; } = new();
        public List<RoofingItem> Fasteners { get; set; } = new();
        public List<RoofingItem> Underlayment { get; set; } = new();
        public List<RoofingItem> SnowGuards { get; set; } = new();
        public List<RoofingItem> Accessories { get; set; } = new();
        public double TotalRoofArea { get; set; }
        public double WastePercentage { get; set; } = 0.10;
        public double TotalWithWaste { get; set; }
        public double TotalRoofingCost { get; set; }
    }

    // ─── Siding ───────────────────────────────────────────

    public class SidingItem
    {
        public string ProductCode { get; set; } = "";
        public string Description { get; set; } = "";
        public double Coverage { get; set; }
        public double Length { get; set; }
        public int Quantity { get; set; }
        public double SquareFeet { get; set; }
        public string Color { get; set; } = "Galvalume";
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
    }

    public class SidingTakeoff
    {
        public List<SidingItem> WallPanels { get; set; } = new();
        public List<SidingItem> CornerTrim { get; set; } = new();
        public List<SidingItem> BaseTrim { get; set; } = new();
        public List<SidingItem> JChannel { get; set; } = new();
        public List<SidingItem> Fasteners { get; set; } = new();
        public List<SidingItem> Flashing { get; set; } = new();
        public List<SidingItem> Accessories { get; set; } = new();
        public double TotalWallArea { get; set; }
        public double OpeningDeductions { get; set; }
        public double NetWallArea { get; set; }
        public double WastePercentage { get; set; } = 0.10;
        public double TotalSidingCost { get; set; }
    }

    // ─── Foundation ───────────────────────────────────────

    public class ConcreteItem
    {
        public string Description { get; set; } = "";
        public double Volume { get; set; }
        public double Strength { get; set; }
        public int Quantity { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
    }

    public class ReinforcementItem
    {
        public string Description { get; set; } = "";
        public string Size { get; set; } = "";
        public double Length { get; set; }
        public int Quantity { get; set; }
        public double WeightPerFoot { get; set; }
        public double TotalWeight { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
    }

    public class ExcavationItem
    {
        public string Description { get; set; } = "";
        public double Volume { get; set; }
        public double UnitPrice { get; set; }
        public double TotalPrice { get; set; }
    }

    public class FoundationTakeoff
    {
        public List<ConcreteItem> Concrete { get; set; } = new();
        public List<ReinforcementItem> Reinforcement { get; set; } = new();
        public List<ExcavationItem> Excavation { get; set; } = new();
        public List<HardwareItem> AnchorBolts { get; set; } = new();
        public double TotalConcreteYards { get; set; }
        public double TotalReinforcementWeight { get; set; }
        public double TotalFoundationCost { get; set; }
    }
}
