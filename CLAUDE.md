# CLAUDE.md — Pole Barn Generator Plugin

## Project Context
AutoCAD .NET (C#) plugin that generates pole barn / post-frame structures from user measurements.
Produces plan view, front elevation, side elevation, and 3D wireframe in the active drawing.

## Tech Stack
- **Language**: C# (.NET 4.8 or .NET 8 depending on AutoCAD version target)
- **UI**: WPF (XAML) for input dialog
- **AutoCAD API**: AcDbMgd, AcMgd, AcCoreMgd assemblies
- **Build**: Visual Studio / dotnet CLI / VS Code with C# extension
- **Testing**: NUnit or xUnit for geometry/math unit tests

## Architecture
See `docs/ARCHITECTURE.md` for full details. Key pattern:
1. User inputs parameters via WPF dialog → BarnParameters model
2. BarnGeometry computes all structural points/members from parameters
3. Generator classes (Plan, FrontElev, SideElev, 3D) consume geometry and create AutoCAD entities
4. All generators use LayerManager for consistent layer setup
5. DimensionGenerator adds dimensions to each view

## Code Conventions
- All AutoCAD database operations wrapped in Transaction using `using` blocks
- Never leave transactions uncommitted — always Commit() or Abort()
- Entity creation pattern: `new Line(pt1, pt2)` → `btr.AppendEntity(line)` → `tr.AddNewlyCreatedDBObject(line, true)`
- Use `Editor.WriteMessage()` for debug/status output to command line
- All distances stored internally in feet (AutoCAD units = feet)
- WPF dialog must call `Autodesk.AutoCAD.ApplicationServices.Application.ShowModalWindow(dialog)`

## Layer Naming Convention
All layers prefixed with `PB-` (Pole Barn). See ARCHITECTURE.md for full table.

## Key AutoCAD API Patterns
```csharp
// Standard transaction pattern
Document doc = Application.DocumentManager.MdiActiveDocument;
Database db = doc.Database;
Editor ed = doc.Editor;

using (Transaction tr = db.TransactionManager.StartTransaction())
{
    BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
    BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

    // Create entities here
    Line line = new Line(new Point3d(0, 0, 0), new Point3d(10, 0, 0));
    btr.AppendEntity(line);
    tr.AddNewlyCreatedDBObject(line, true);

    tr.Commit();
}
```

## Task Assignment Rules
- Each phase from ARCHITECTURE.md is a milestone
- Work one generator at a time — complete and test before moving on
- BarnParameters and BarnGeometry must be solid before any generator work
- WPF dialog can be iterated incrementally (start minimal, add tabs)

## Testing Approach
- Unit test BarnGeometry calculations (roof peak height, post positions, truss angles)
- Manual test in AutoCAD after each generator phase
- Validate layer creation and entity counts

## Common Pitfalls
- Forgetting to set `line.Layer = "PB-POSTS"` before appending
- Not opening BlockTableRecord in OpenMode.ForWrite
- Using Point2d where Point3d is needed (3D generator)
- WPF dialog must be invoked on AutoCAD's UI thread
- Roof pitch math: rise = (Width/2) * (pitch/12), peak = EaveHeight + rise
