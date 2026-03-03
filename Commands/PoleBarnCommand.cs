using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using PoleBarnGenerator.Generators;
using PoleBarnGenerator.Models;
using PoleBarnGenerator.UI;
using PoleBarnGenerator.Utils;
using Autodesk.AutoCAD.Geometry;

[assembly: CommandClass(typeof(PoleBarnGenerator.Commands.PoleBarnCommand))]

namespace PoleBarnGenerator.Commands
{
    /// <summary>
    /// Registers the POLEBARN command in AutoCAD.
    /// Usage: Type POLEBARN at the command line to launch the generator.
    /// 
    /// Loading the plugin:
    ///   NETLOAD → browse to PoleBarnGenerator.dll
    ///   Or add to autoload via acad.lsp / registry
    /// </summary>
    public class PoleBarnCommand
    {
        [CommandMethod("POLEBARN", CommandFlags.Modal)]
        public void RunPoleBarn()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n=== Pole Barn Generator ===\n");

            // Show WPF dialog for parameter input
            PoleBarnDialog dialog = new PoleBarnDialog();

            // Must use AutoCAD's modal window host
            var result = Application.ShowModalWindow(dialog);

            if (result != true)
            {
                ed.WriteMessage("\nPole Barn generation cancelled.\n");
                return;
            }

            BarnParameters parameters = dialog.Parameters;

            // Validate
            var (isValid, error) = parameters.Validate();
            if (!isValid)
            {
                ed.WriteMessage($"\nValidation error: {error}\n");
                return;
            }

            // Compute geometry
            BarnGeometry geometry = new BarnGeometry(parameters);

            ed.WriteMessage($"\nGenerating {parameters.BuildingWidth}' x {parameters.BuildingLength}' pole barn...");
            ed.WriteMessage($"\n  Eave: {parameters.EaveHeight}', Peak: {geometry.PeakHeight:F1}', Pitch: {parameters.RoofPitchDisplay}");
            ed.WriteMessage($"\n  Bays: {geometry.NumBays} @ {geometry.ActualBaySpacing:F1}' O.C.");
            ed.WriteMessage($"\n  Posts: {geometry.Posts.Count}, Girt lines: {geometry.Girts.Count}");

            // Run generation in a single transaction
            using (Transaction tr = db.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                // Create all layers
                LayerManager.EnsureLayers(tr, db);

                // View placement offsets (side by side in model space)
                double gap = 20.0; // feet between views
                double planWidth = parameters.BuildingWidth + 10;
                double elevWidth = parameters.BuildingWidth + 10;

                Vector3d planOffset = new Vector3d(0, 0, 0);
                Vector3d frontOffset = new Vector3d(planWidth + gap, 0, 0);
                Vector3d sideOffset = new Vector3d(planWidth + elevWidth + gap * 2, 0, 0);

                int entityCount = 0;

                // Set dimension style before generating any views with dimensions
                DimensionGenerator.SetCurrentDimStyle(tr, db);

                // Generate each view with error handling
                if (parameters.GeneratePlan)
                {
                    try
                    {
                        ed.WriteMessage("\n  Drawing plan view...");
                        entityCount += PlanViewGenerator.Generate(tr, btr, geometry, planOffset);
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n  ERROR generating plan view: {ex.Message}");
                    }
                }

                if (parameters.GenerateFront)
                {
                    try
                    {
                        ed.WriteMessage("\n  Drawing front elevation...");
                        entityCount += FrontElevationGenerator.Generate(tr, btr, geometry, frontOffset);
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n  ERROR generating front elevation: {ex.Message}");
                    }
                }

                if (parameters.GenerateSide)
                {
                    try
                    {
                        ed.WriteMessage("\n  Drawing side elevation...");
                        entityCount += SideElevationGenerator.Generate(tr, btr, geometry, sideOffset);
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n  ERROR generating side elevation: {ex.Message}");
                    }
                }

                if (parameters.Generate3D)
                {
                    try
                    {
                        ed.WriteMessage("\n  Building 3D wireframe...");
                        entityCount += Wireframe3DGenerator.Generate(tr, btr, geometry);
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\n  ERROR generating 3D wireframe: {ex.Message}");
                    }
                }

                tr.Commit();

                ed.WriteMessage($"\n\nPole barn generated: {entityCount} entities created.");
                ed.WriteMessage("\n=== Complete ===\n");
            }

            // Zoom to extents
            ed.Command("_.ZOOM", "_E");
        }

        /// <summary>
        /// Quick command that skips the dialog and uses a preset.
        /// Usage: POLEBARN30X40
        /// </summary>
        [CommandMethod("POLEBARN3040", CommandFlags.Modal)]
        public void RunPoleBarn30x40()
        {
            GenerateFromPreset("30x40");
        }

        [CommandMethod("POLEBARN4060", CommandFlags.Modal)]
        public void RunPoleBarn40x60()
        {
            GenerateFromPreset("40x60");
        }

        private void GenerateFromPreset(string presetName)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage($"\nGenerating preset pole barn: {presetName}\n");

            BarnParameters parameters = BarnParameters.CreatePreset(presetName);
            BarnGeometry geometry = new BarnGeometry(parameters);

            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                BlockTable bt = tr.GetObject(doc.Database.BlockTableId, OpenMode.ForRead) as BlockTable;
                BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                LayerManager.EnsureLayers(tr, doc.Database);

                double gap = 20.0;
                double planWidth = parameters.BuildingWidth + 10;
                double elevWidth = parameters.BuildingWidth + 10;

                // Set dimension style before generating
                DimensionGenerator.SetCurrentDimStyle(tr, doc.Database);

                try
                {
                    PlanViewGenerator.Generate(tr, btr, geometry, new Vector3d(0, 0, 0));
                    FrontElevationGenerator.Generate(tr, btr, geometry, new Vector3d(planWidth + gap, 0, 0));
                    SideElevationGenerator.Generate(tr, btr, geometry, new Vector3d(planWidth + elevWidth + gap * 2, 0, 0));
                    Wireframe3DGenerator.Generate(tr, btr, geometry);
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nERROR during generation: {ex.Message}");
                }

                tr.Commit();
            }

            ed.Command("_.ZOOM", "_E");
            ed.WriteMessage($"\n{presetName} pole barn generated.\n");
        }
    }
}
