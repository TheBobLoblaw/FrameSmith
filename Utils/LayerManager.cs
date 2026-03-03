using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using System.Collections.Generic;

namespace PoleBarnGenerator.Utils
{
    /// <summary>
    /// Creates and manages all PB- prefixed layers for the pole barn drawing.
    /// Call EnsureLayers() once at the start of generation.
    /// </summary>
    public static class LayerManager
    {
        public static class Layers
        {
            public const string Posts    = "PB-POSTS";
            public const string Girts    = "PB-GIRTS";
            public const string Trusses  = "PB-TRUSSES";
            public const string Purlins  = "PB-PURLINS";
            public const string Rafters  = "PB-RAFTERS";
            public const string Roof     = "PB-ROOF";
            public const string Slab     = "PB-SLAB";
            public const string Doors    = "PB-DOORS";
            public const string Windows  = "PB-WINDOWS";
            public const string Dims     = "PB-DIM";
            public const string Anno     = "PB-ANNO";
            public const string Wire3D   = "PB-3D";
        }

        private static readonly Dictionary<string, short> LayerColors = new Dictionary<string, short>
        {
            { Layers.Posts,    1 },    // Red
            { Layers.Girts,    3 },    // Green
            { Layers.Trusses,  5 },    // Blue
            { Layers.Purlins,  4 },    // Cyan
            { Layers.Rafters,  6 },    // Magenta
            { Layers.Roof,     2 },    // Yellow
            { Layers.Slab,     8 },    // Dark gray
            { Layers.Doors,    30 },   // Orange
            { Layers.Windows,  40 },   // Light orange
            { Layers.Dims,     7 },    // White
            { Layers.Anno,     7 },    // White
            { Layers.Wire3D,   150 },  // Light blue
        };

        /// <summary>
        /// Ensures all PB- layers exist in the drawing database.
        /// Must be called within an active Transaction.
        /// </summary>
        public static void EnsureLayers(Transaction tr, Database db)
        {
            LayerTable lt = tr.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

            foreach (var kvp in LayerColors)
            {
                if (!lt.Has(kvp.Key))
                {
                    lt.UpgradeOpen();

                    LayerTableRecord ltr = new LayerTableRecord
                    {
                        Name = kvp.Key,
                        Color = Color.FromColorIndex(ColorMethod.ByAci, kvp.Value)
                    };

                    lt.Add(ltr);
                    tr.AddNewlyCreatedDBObject(ltr, true);
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
