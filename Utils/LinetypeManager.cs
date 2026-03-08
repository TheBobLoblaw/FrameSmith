using Autodesk.AutoCAD.DatabaseServices;
using System;

namespace PoleBarnGenerator.Utils
{
    /// <summary>
    /// Loads standard AutoCAD linetypes required for professional drawing output.
    /// Handles missing linetype files gracefully with fallbacks.
    /// </summary>
    public static class LinetypeManager
    {
        private static readonly string[] RequiredLinetypes = new[]
        {
            "CENTER",   // Ridge lines, center lines, grid lines
            "HIDDEN",   // Hidden members behind cut plane
            "DASHED",   // Roof lines above/below, wall below in plan
            "PHANTOM",  // Overhang outlines, match lines
            "DASHED2",  // Lean-to roof (shorter dash)
        };

        /// <summary>
        /// Load all required linetypes from acad.lin or acadiso.lin.
        /// Safe to call multiple times — skips already-loaded types.
        /// </summary>
        public static void LoadRequiredLinetypes(Database db)
        {
            foreach (string ltName in RequiredLinetypes)
            {
                try
                {
                    // Try acad.lin first (imperial), then acadiso.lin (metric)
                    try
                    {
                        db.LoadLineTypeFile(ltName, "acad.lin");
                    }
                    catch
                    {
                        try
                        {
                            db.LoadLineTypeFile(ltName, "acadiso.lin");
                        }
                        catch
                        {
                            // Linetype already loaded or file not found — both OK
                        }
                    }
                }
                catch
                {
                    // Silently continue — missing linetypes fall back to Continuous
                }
            }
        }

        /// <summary>
        /// Sets the global linetype scale for the drawing.
        /// For 1/4" = 1'-0" scale: LTSCALE = 48
        /// For 1/8" = 1'-0" scale: LTSCALE = 96
        /// </summary>
        public static void SetLinetypeScale(Database db, double scale = 48.0)
        {
            db.Ltscale = scale;
            db.Psltscale = true; // Paper space linetype scaling enabled
        }
    }
}
