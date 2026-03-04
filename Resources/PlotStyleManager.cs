using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PoleBarnGenerator.Resources
{
    /// <summary>
    /// Generates FrameSmith.ctb plot style table for professional output.
    /// Maps ACI colors to appropriate pen weights and plot colors.
    /// All colors plot as black except hatch (50% gray screening).
    /// </summary>
    public static class PlotStyleManager
    {
        private struct PlotStyle
        {
            public double LineWeight;  // mm
            public int GrayPercent;    // 0=black, 50=gray

            public PlotStyle(double weight, int gray = 0)
            {
                LineWeight = weight;
                GrayPercent = gray;
            }
        }

        // Map ACI color index → plot style
        private static readonly Dictionary<int, PlotStyle> StyleMap = new Dictionary<int, PlotStyle>
        {
            // Heavy (0.50mm) — walls, posts, slab, headers
            { 30,  new PlotStyle(0.50) },  // Posts, Headers
            { 9,   new PlotStyle(0.50) },  // Walls
            { 8,   new PlotStyle(0.50) },  // Slab (note: also grid, handled by lineweight)

            // Medium (0.35mm) — doors, windows, structure, girts, trusses
            { 1,   new PlotStyle(0.35) },  // Doors, Callouts
            { 5,   new PlotStyle(0.35) },  // Windows
            { 130, new PlotStyle(0.35) },  // Girts, Plates, Workshop
            { 150, new PlotStyle(0.35) },  // Trusses, 3D wireframe
            { 3,   new PlotStyle(0.35) },  // Roof
            { 110, new PlotStyle(0.35) },  // Lean-to
            { 62,  new PlotStyle(0.35) },  // Porches
            { 50,  new PlotStyle(0.35) },  // Stalls, Partitions

            // Medium-Light (0.25mm) — purlins, bracing, loft
            { 170, new PlotStyle(0.25) },  // Purlins, Ridge
            { 134, new PlotStyle(0.25) },  // Bracing
            { 174, new PlotStyle(0.25) },  // Loft

            // Light (0.18mm) — annotations, dims, text
            { 7,   new PlotStyle(0.18) },  // Dims, Anno, Text
            { 40,  new PlotStyle(0.18) },  // Wainscot
            { 31,  new PlotStyle(0.00) },  // Post fill (solid)

            // Hairline (0.09mm) — hatch at 50% gray
            { 253, new PlotStyle(0.09, 50) },  // Slab hatch

            // Legacy
            { 6,   new PlotStyle(0.25) },  // Rafters
            { 90,  new PlotStyle(0.18) },  // Details
        };

        /// <summary>
        /// Generates a CTB plot style table file.
        /// CTB files are proprietary binary format — this generates a text-based
        /// version that AutoCAD can import, or serves as documentation.
        /// In practice, the CTB should be distributed as a resource file.
        /// </summary>
        public static string GeneratePlotStyleDocumentation()
        {
            var sb = new StringBuilder();
            sb.AppendLine("FrameSmith Plot Style Table (FS-Standard.ctb)");
            sb.AppendLine("=============================================");
            sb.AppendLine();
            sb.AppendLine("Color | Weight (mm) | Plot Color | Usage");
            sb.AppendLine("------|-------------|------------|------");

            foreach (var kvp in StyleMap)
            {
                string plotColor = kvp.Value.GrayPercent > 0 ? $"{kvp.Value.GrayPercent}% Gray" : "Black";
                sb.AppendLine($"  {kvp.Key,3} | {kvp.Value.LineWeight,11:F2} | {plotColor,-10} |");
            }

            sb.AppendLine();
            sb.AppendLine("Line Weight Hierarchy:");
            sb.AppendLine("  Heavy  (0.50mm): Walls, posts, slab, headers");
            sb.AppendLine("  Medium (0.35mm): Doors, windows, visible structure");
            sb.AppendLine("  Med-Lt (0.25mm): Purlins, bracing, secondary structure");
            sb.AppendLine("  Light  (0.18mm): Dimensions, annotations, text");
            sb.AppendLine("  Fine   (0.13mm): Glass, door tracks, detail lines");
            sb.AppendLine("  Hair   (0.09mm): Hatch patterns (50% gray)");

            return sb.ToString();
        }

        /// <summary>
        /// Writes a PCP/PC3 compatible plot configuration reference file.
        /// </summary>
        public static void WriteDocumentation(string filePath)
        {
            File.WriteAllText(filePath, GeneratePlotStyleDocumentation());
        }
    }
}
