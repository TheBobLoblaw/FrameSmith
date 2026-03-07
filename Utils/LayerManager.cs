using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace PoleBarnGenerator.Utils
{
    /// <summary>
    /// Creates and manages all PB- prefixed layers following AIA/NCS standards.
    /// Layer naming: PB-MAJORGROUP-MINORGROUP
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
            public const string Posts          = "PB-POST";
            public const string PostsFill      = "PB-POST-FILL";
            public const string Girts          = "PB-GIRT";
            public const string Trusses        = "PB-TRSS";
            public const string TrussesHidden  = "PB-TRSS-HIDDEN";
            public const string Purlins        = "PB-PURL";
            public const string Ridge          = "PB-RIDG";
            public const string Bracing        = "PB-BRAC";
            public const string Headers        = "PB-HEADER";
            public const string Plates         = "PB-PLATE";

            // ENVELOPE
            public const string Walls          = "PB-WALL";
            public const string WallsBelow     = "PB-WALL-BELOW";
            public const string Roof           = "PB-ROOF";
            public const string RoofHidden     = "PB-ROOF-HIDDEN";
            public const string RoofOverhang   = "PB-ROOF-OVRHG";
            public const string Slab           = "PB-SLAB";
            public const string SlabHatch      = "PB-SLAB-HATCH";
            public const string Wainscot       = "PB-WAINSCOT";

            // OPENINGS
            public const string Doors          = "PB-DOOR";
            public const string DoorSwing      = "PB-DOOR-SWING";
            public const string DoorTrack      = "PB-DOOR-TRACK";
            public const string DoorPanel      = "PB-DOOR-PANEL";
            public const string Windows        = "PB-WIND";
            public const string WindowGlass    = "PB-WIND-GLASS";

            // LEAN-TO / PORCH
            public const string LeanTo         = "PB-LEAN";
            public const string LeanToRoof     = "PB-LEAN-ROOF";
            public const string Porches        = "PB-PORCH";
            public const string PorchRail      = "PB-PORCH-RAIL";

            // INTERIOR
            public const string Stalls         = "PB-STALL";
            public const string Partitions     = "PB-PART";
            public const string Loft           = "PB-LOFT";
            public const string LoftEdge       = "PB-LOFT-EDGE";
            public const string Workshop       = "PB-WORKSHOP";

            // REFERENCE
            public const string Grid           = "PB-GRID";
            public const string GridBubbles    = "PB-GRID-BUBS";
            public const string GridText       = "PB-GRID-TEXT";
            public const string CenterLines    = "PB-CNTR";

            // ANNOTATION
            public const string Dims           = "PB-DIM";
            public const string Anno           = "PB-TEXT";
            public const string TextTitle      = "PB-TEXT-TITLE";
            public const string Symbols        = "PB-SYMB";
            public const string Leaders        = "PB-ANNO-LEADER";
            public const string Callouts       = "PB-CALLOUT";

            // 3D WIREFRAME
            public const string Wire3D         = "PB-3D-STRUCT";
            public const string Wire3DRoof     = "PB-3D-ROOF";
            public const string Wire3DSlab     = "PB-3D-SLAB";

            // ADVANCED FEATURES (PB- legacy prefix requested)
            public const string Floor          = "PB-FLOOR";
            public const string Curved         = "PB-CURVED";
            public const string Joint          = "PB-JOINT";
            public const string JointDetail    = "PB-JOINT-DETAIL";
            public const string Dairy          = "PB-DAIRY";
            public const string Crane          = "PB-CRANE";
            public const string Vent           = "PB-VENT";
            public const string Drain          = "PB-DRAIN";
            public const string Grain          = "PB-GRAIN";
            public const string Equip          = "PB-EQUIP";

            // Legacy aliases
            public const string Rafters        = "PB-RAFT";
            public const string Details        = "PB-DETAIL";
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

            // ADVANCED FEATURES
            { Layers.Floor,         new LayerDef(34,  LineWeight.LineWeight025, "DASHED") },
            { Layers.Curved,        new LayerDef(151, LineWeight.LineWeight035) },
            { Layers.Joint,         new LayerDef(10,  LineWeight.LineWeight050, "CENTER") },
            { Layers.JointDetail,   new LayerDef(10,  LineWeight.LineWeight018) },
            { Layers.Dairy,         new LayerDef(33,  LineWeight.LineWeight025) },
            { Layers.Crane,         new LayerDef(21,  LineWeight.LineWeight035, "DASHED") },
            { Layers.Vent,          new LayerDef(92,  LineWeight.LineWeight025) },
            { Layers.Drain,         new LayerDef(150, LineWeight.LineWeight025, "CENTER") },
            { Layers.Grain,         new LayerDef(32,  LineWeight.LineWeight025) },
            { Layers.Equip,         new LayerDef(23,  LineWeight.LineWeight035) },

            // Legacy
            { Layers.Rafters,       new LayerDef(6,   LineWeight.LineWeight025) },
            { Layers.Details,       new LayerDef(90,  LineWeight.LineWeight018) },
        };

        /// <summary>
        /// Ensures all PB- layers exist with correct color, lineweight, and linetype.
        /// Must be called within an active Transaction.
        /// </summary>
        public static void EnsureLayers(Transaction tr, Database db, bool enforceStandards = false)
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
                    if (enforceStandards)
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
