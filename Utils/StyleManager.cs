using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace PoleBarnGenerator.Utils
{
    /// <summary>
    /// Creates professional dimension and text styles for FrameSmith drawings.
    /// All measurements in drawing units (inches for architectural).
    /// </summary>
    public static class StyleManager
    {
        // Text heights at 1:1 scale (in feet, since drawing units = feet)
        // These will appear correct at 1/4" = 1'-0" when plotted
        private const double TextHeight3_32  = 3.0 / 32.0 * 4.0;  // 3/32" at 1/4"=1'-0" → 0.375'
        private const double TextHeight1_8   = 1.0 / 8.0 * 4.0;   // 1/8" at 1/4"=1'-0" → 0.5'
        private const double TextHeight3_16  = 3.0 / 16.0 * 4.0;  // 3/16" at 1/4"=1'-0" → 0.75'

        /// <summary>
        /// Creates or updates the FS-DIM dimension style with architectural standards.
        /// </summary>
        public static void CreateDimensionStyle(Transaction tr, Database db)
        {
            DimStyleTable dst = tr.GetObject(db.DimStyleTableId, OpenMode.ForRead) as DimStyleTable;

            DimStyleTableRecord dimStyle;
            bool isNew = false;

            if (dst.Has("FS-DIM"))
            {
                dimStyle = tr.GetObject(dst["FS-DIM"], OpenMode.ForWrite) as DimStyleTableRecord;
            }
            else
            {
                dst.UpgradeOpen();
                dimStyle = new DimStyleTableRecord();
                dimStyle.Name = "FS-DIM";
                isNew = true;
            }

            // Architectural units: feet-inches with 1/2" precision
            dimStyle.Dimlunit = 4;          // Architectural
            dimStyle.Dimlfac = 12.0;        // Convert feet to inches for display
            dimStyle.Dimdec = 2;            // Precision (1/2")

            // Text
            dimStyle.Dimtxt = TextHeight3_32;   // Text height
            dimStyle.Dimgap = TextHeight3_32 / 3.0;  // Gap around text
            dimStyle.Dimtad = 1;            // Text above dimension line

            // Set text style if available
            TextStyleTable tst = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;
            if (tst.Has("FS-STANDARD"))
            {
                dimStyle.Dimtxsty = tst["FS-STANDARD"];
            }

            // Arrows: Architectural tick
            dimStyle.Dimtsz = TextHeight3_32;   // Tick size (oblique stroke)

            // Extension lines
            dimStyle.Dimexo = 1.0 / 16.0 * 4.0;  // Offset from origin (1/16" at scale)
            dimStyle.Dimexe = 1.0 / 16.0 * 4.0;  // Extension beyond dim line

            // Baseline spacing
            dimStyle.Dimdli = 3.0 / 8.0 * 4.0;   // 3/8" at scale

            // Suppress leading zeros, show feet-inches
            dimStyle.Dimzin = 0;

            // Color: ByBlock (inherits from layer)
            dimStyle.Dimclrd = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 256); // ByLayer
            dimStyle.Dimclre = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 256);
            dimStyle.Dimclrt = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                Autodesk.AutoCAD.Colors.ColorMethod.ByAci, 256);

            if (isNew)
            {
                dst.Add(dimStyle);
                tr.AddNewlyCreatedDBObject(dimStyle, true);
            }
        }

        /// <summary>
        /// Creates professional text styles for all annotation needs.
        /// </summary>
        public static void CreateTextStyles(Transaction tr, Database db)
        {
            TextStyleTable tst = tr.GetObject(db.TextStyleTableId, OpenMode.ForRead) as TextStyleTable;

            // FS-STANDARD: General notes, dimensions - Romans.shx, 3/32"
            CreateTextStyle(tr, tst, "FS-STANDARD", "romans.shx", TextHeight3_32);

            // FS-TITLE: View titles - Romand.shx (double stroke), 3/16"
            CreateTextStyle(tr, tst, "FS-TITLE", "romand.shx", TextHeight3_16);

            // FS-LABEL: Room labels, callouts - Romans.shx, 1/8"
            CreateTextStyle(tr, tst, "FS-LABEL", "romans.shx", TextHeight1_8);

            // FS-NOTE: Detail notes - Romans.shx, 3/32"
            CreateTextStyle(tr, tst, "FS-NOTE", "romans.shx", TextHeight3_32);
        }

        private static void CreateTextStyle(Transaction tr, TextStyleTable tst,
            string name, string fontFile, double height)
        {
            TextStyleTableRecord style;
            bool isNew = false;

            if (tst.Has(name))
            {
                style = tr.GetObject(tst[name], OpenMode.ForWrite) as TextStyleTableRecord;
            }
            else
            {
                tst.UpgradeOpen();
                style = new TextStyleTableRecord();
                style.Name = name;
                isNew = true;
            }

            style.FileName = fontFile;
            style.TextSize = height;
            style.XScale = 1.0;
            style.ObliquingAngle = 0.0;

            if (isNew)
            {
                tst.Add(style);
                tr.AddNewlyCreatedDBObject(style, true);
            }
        }
    }
}
