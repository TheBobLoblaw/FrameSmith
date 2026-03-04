using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace PoleBarnGenerator.Utils
{
    /// <summary>
    /// Creates and manages all FS-S- prefixed layers following AIA/NCS standards.
    /// Layer naming: FS-S-MAJORGROUP-MINORGROUP
    /// Line weight hierarchy:
    ///   Heavy  (0.50mm): Walls, posts, slab
    ///   Medium (0.35mm): Doors, windows, visible structure
    ///   Light  (0.18mm): Dimensions, annotations, text
    ///   Fine   (0.13mm): Detail lines, glass, door tracks
    ///   Hair   (0.09mm): Grid lines, hatch patterns
    /// </summary>
    public static class LayerManager
    {
        public static class Layers
        {
            // STRUCTURAL
            public const string Posts          = "FS-S-POST";
            public const string PostsFill      = "FS-S-POST-FILL";
            public const string Girts          = "FS-S-GIRT";
            public const string Trusses        = "FS-S-TRSS";
            public const string TrussesHidden  = "FS-S-TRSS-HIDDEN";
            public const string Purlins        = "FS-S-PURL";
            public const string Ridge          = "FS-S-RIDG";
            public const string Bracing        = "FS-S-BRAC";
            public const string Headers        = "FS-S-HEADER";
            public const string Plates         = "FS-S-PLATE";

            // ENVELOPE
            public const string Walls          = "FS-S-WALL";
            public const string WallsBelow     = "FS-S-WALL-BELOW";
            public const string Roof           = "FS-S-ROOF";
            public const string RoofHidden     = "FS-S-ROOF-HIDDEN";
            public const string RoofOverhang   = "FS-S-ROOF-OVRHG";
            public const string Slab           = "FS-S-SLAB";
            public const string SlabHatch      = "FS-S-SLAB-HATCH";
            public const string Wainscot       = "FS-S-WAINSCOT";

            // OPENINGS
            public const string Doors          = "FS-S-DOOR";
            public const string DoorSwing      = "FS-S-DOOR-SWING";
            public const string DoorTrack      = "FS-S-DOOR-TRACK";
            public const string DoorPanel      = "FS-S-DOOR-PANEL";
            public const string Windows        = "FS-S-WIND";
            public const string WindowGlass    = "FS-S-WIND-GLASS";

            // LEAN-TO / PORCH
            public const string LeanTo         = "FS-S-LEAN";
            public const string LeanToRoof     = "FS-S-LEAN-ROOF";
            public const string Porches        = "FS-S-PORCH";
            public const string PorchRail      = "FS-S-PORCH-RAIL";

            // INTERIOR
            public const string Stalls         = "FS-S-STALL";
            public const string Partitions     = "FS-S-PART";
            public const string Loft           = "FS-S-LOFT";
            public const string LoftEdge       = "FS-S-LOFT-EDGE";
            public const string Workshop       = "FS-S-WORKSHOP";

            // REFERENCE
            public const string Grid           = "FS-S-GRID";
            public const string GridBubbles    = "FS-S-GRID-BUBS";
            public const string GridText       = "FS-S-GRID-TEXT";
            public const string CenterLines    = "FS-S-CNTR";

            // ANNOTATION
            public const string Dims           = "FS-S-DIM";
            public const string Anno           = "FS-S-TEXT";
            public const string TextTitle      = "FS-S-TEXT-TITLE";
            public const string Symbols        = "FS-S-SYMB";
            public const string Leaders        = "FS-S-ANNO-LEADER";
            public const string Callouts       = "FS-S-CALLOUT";

            // 3D WIREFRAME
            public const string Wire3D         = "FS-S-3D-STRUCT";
            public const string Wire3DRoof     = "FS-S-3D-ROOF";
            public const string Wire3DSlab     = "FS-S-3D-SLAB";

            // Legacy aliases
            public const string Rafters        = "FS-S-RAFT";
            public const string Details        = "FS-S-DETAIL";
        }

        private struct LayerDef
        {
            public short Color;
            public LineWeight Weight;
            public string Linetype;

            public LayerDef(short color, LineWeight weight, string linetype = "Continuous")
            {
                Color = color;
                Weight = weight;
                Linetype = linetype;
            }
        }

        private static readonly Dictionary<string, LayerDef> LayerDefs = new Dictionary<string, LayerDef>
        {
            // STRUCTURAL
            { Layers.Posts,         new LayerDef(30,  LineWeight.LineWeight050) },
            { Layers.PostsFill,     new LayerDef(31,  LineWeight.LineWeight000) },
            { Layers.Girts,         new LayerDef(130, LineWeight.LineWeight035) },
            { Layers.Trusses,       new LayerDef(150, LineWeight.LineWeight035) },
            { Layers.TrussesHidden, new LayerDef(150, LineWeight.LineWeight018, "HIDDEN") },
            { Layers.Purlins,       new LayerDef(170, LineWeight.LineWeight025) },
            { Layers.Ridge,         new LayerDef(170, LineWeight.LineWeight025, "CENTER") },
            { Layers.Bracing,       new LayerDef(134, LineWeight.LineWeight025) },
            { Layers.Headers,       new LayerDef(30,  LineWeight.LineWeight050) },
            { Layers.Plates,        new LayerDef(130, LineWeight.LineWeight035) },

            // ENVELOPE
            { Layers.Walls,         new LayerDef(9,   LineWeight.LineWeight050) },
            { Layers.WallsBelow,    new LayerDef(9,   LineWeight.LineWeight018, "DASHED") },
            { Layers.Roof,          new LayerDef(3,   LineWeight.LineWeight035) },
            { Layers.RoofHidden,    new LayerDef(3,   LineWeight.LineWeight018, "DASHED") },
            { Layers.RoofOverhang,  new LayerDef(3,   LineWeight.LineWeight025, "PHANTOM") },
            { Layers.Slab,          new LayerDef(8,   LineWeight.LineWeight050) },
            { Layers.SlabHatch,     new LayerDef(253, LineWeight.LineWeight009) },
            { Layers.Wainscot,      new LayerDef(40,  LineWeight.LineWeight018) },

            // OPENINGS
            { Layers.Doors,         new LayerDef(1,   LineWeight.LineWeight035) },
            { Layers.DoorSwing,     new LayerDef(1,   LineWeight.LineWeight018) },
            { Layers.DoorTrack,     new LayerDef(1,   LineWeight.LineWeight013, "DASHED") },
            { Layers.DoorPanel,     new LayerDef(1,   LineWeight.LineWeight013) },
            { Layers.Windows,       new LayerDef(5,   LineWeight.LineWeight035) },
            { Layers.WindowGlass,   new LayerDef(5,   LineWeight.LineWeight013) },

            // LEAN-TO / PORCH
            { Layers.LeanTo,        new LayerDef(110, LineWeight.LineWeight035) },
            { Layers.LeanToRoof,    new LayerDef(110, LineWeight.LineWeight025, "DASHED2") },
            { Layers.Porches,       new LayerDef(62,  LineWeight.LineWeight035) },
            { Layers.PorchRail,     new LayerDef(62,  LineWeight.LineWeight018) },

            // INTERIOR
            { Layers.Stalls,        new LayerDef(50,  LineWeight.LineWeight025) },
            { Layers.Partitions,    new LayerDef(50,  LineWeight.LineWeight035) },
            { Layers.Loft,          new LayerDef(174, LineWeight.LineWeight025, "DASHED") },
            { Layers.LoftEdge,      new LayerDef(174, LineWeight.LineWeight035) },
            { Layers.Workshop,      new LayerDef(130, LineWeight.LineWeight025) },

            // REFERENCE
            { Layers.Grid,          new LayerDef(8,   LineWeight.LineWeight009, "CENTER") },
            { Layers.GridBubbles,   new LayerDef(8,   LineWeight.LineWeight018) },
            { Layers.GridText,      new LayerDef(8,   LineWeight.LineWeight013) },
            { Layers.CenterLines,   new LayerDef(8,   LineWeight.LineWeight009, "CENTER") },

            // ANNOTATION
            { Layers.Dims,          new LayerDef(7,   LineWeight.LineWeight018) },
            { Layers.Anno,          new LayerDef(7,   LineWeight.LineWeight018) },
            { Layers.TextTitle,     new LayerDef(7,   LineWeight.LineWeight025) },
            { Layers.Symbols,       new LayerDef(7,   LineWeight.LineWeight018) },
            { Layers.Leaders,       new LayerDef(7,   LineWeight.LineWeight013) },
            { Layers.Callouts,      new LayerDef(1,   LineWeight.LineWeight025) },

            // 3D WIREFRAME
            { Layers.Wire3D,        new LayerDef(150, LineWeight.LineWeight025) },
            { Layers.Wire3DRoof,    new LayerDef(3,   LineWeight.LineWeight025) },
            { Layers.Wire3DSlab,    new LayerDef(8,   LineWeight.LineWeight025) },

            // Legacy
            { Layers.Rafters,       new LayerDef(6,   LineWeight.LineWeight025) },
            { Layers.Details,       new LayerDef(90,  LineWeight.LineWeight018) },
        };

        /// <summary>
        /// Ensures all FS-S- layers exist with correct color, lineweight, and linetype.
        /// Must be called within an active Transaction.
        /// </summary>
        public static void EnsureLayers(Transaction tr, Database db)
        {
            // Load required linetypes first
            LinetypeManager.LoadRequiredLinetypes(db);

            LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;
            LinetypeTable ltt = tr.GetObject(db.LinetypeTableId, OpenMode.ForRead) as LinetypeTable;

            foreach (var kvp in LayerDefs)
            {
                string layerName = kvp.Key;
                LayerDef def = kvp.Value;

                if (!lt.Has(layerName))
                {
                    lt.UpgradeOpen();

                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = layerName,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, def.Color),
                        LineWeight = def.Weight
                    };

                    // Set linetype if available
                    if (def.Linetype != "Continuous" && ltt.Has(def.Linetype))
                    {
                        ltr.LinetypeObjectId = ltt[def.Linetype];
                    }

                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
                }
                else
                {
                    // Update existing layer to match professional specs
                    LayerTableRecord ltr = tr.GetObject(lt[layerName], OpenMode.ForWrite) as LayerTableRecord;
                    ltr.Color = Color.FromColorIndex(ColorMethod.ByAci, def.Color);
                    ltr.LineWeight = def.Weight;

                    if (def.Linetype != "Continuous" && ltt.Has(def.Linetype))
                    {
                        ltr.LinetypeObjectId = ltt[def.Linetype];
                    }
                }
            }
        }

        /// <summary>
        /// Sets the layer for an entity. Call before AppendEntity.
        /// </summary>
        public static void SetLayer(Entity entity, string layerName)
        {
            entity.Layer = layerName;
        }
    }
}
