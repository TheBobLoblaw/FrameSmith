# FrameSmith Round 2 Review (Codex)

Date: 2026-03-07  
Scope: static review of all source files under `Commands/`, `Generators/`, `Models/`, `UI/`, `Utils/`, `Tests/`.

## Executive Result
Not clean. Multiple high-severity issues remain:
- **Build-breaking merge regression**: `PoleBarnCommand` calls generator overloads that do not exist.
- **Build-breaking test integration regression**: test files using xUnit are inside the main project with no xUnit package references.
- Several planned fixes are still **not implemented** or only **partially implemented**.

`dotnet` is not installed in this environment, so this is static analysis only.

## Findings (ordered by severity)

### Critical

1. **Compile break: generator method signature mismatch (merge integration bug)**  
   - `Commands/PoleBarnCommand.cs:110,135,160,185,265,266,267,268` call:
     - `PlanViewGenerator.Generate(..., ed, warnings)`
     - `FrontElevationGenerator.Generate(..., ed, warnings)`
     - `SideElevationGenerator.Generate(..., ed, warnings)`
     - `Wireframe3DGenerator.Generate(..., ed, warnings)`
   - Actual signatures:
     - `Generators/PlanViewGenerator.cs:18`
     - `Generators/FrontElevationGenerator.cs:15`
     - `Generators/SideElevationGenerator.cs:15`
     - `Generators/Wireframe3DGenerator.cs:21`
   - None accept `Editor`/`WarningCollector` parameters.

2. **Compile break: tests added to main project without xUnit dependencies**  
   - xUnit usage in:
     - `Tests/StructuralCalculationsHeaderSizingTests.cs`
     - `Tests/BarnGeometryPostPlacementTests.cs`
     - `Tests/GridBubbleLabelGeneratorTests.cs`
     - `Tests/OpeningValidatorTests.cs`
   - Main project file `PoleBarnPlugin.csproj` has **no** `xunit` / `xunit.runner` / `Microsoft.NET.Test.Sdk` references.
   - This is not a separate test project; these files are compiled as part of plugin build.

### Major

3. **FIX-012 not implemented: silent catch blocks still widespread in generators/services**  
   - Examples:
     - `Generators/PlanViewGenerator.cs:188,197,235`
     - `Generators/FrontElevationGenerator.cs:127,136`
     - `Generators/SideElevationGenerator.cs:103,113`
     - `Generators/Wireframe3DGenerator.cs:159,169,178`
     - `Generators/Services/OpeningDrawingService.cs:27-30,41-44,67-70,85-88,111-114,129-132`
     - `Generators/Services/ExteriorDetailDrawingService.cs:24-27,45-48,64-67`
   - Failures are still swallowed instead of reported/aggregated.

4. **FIX-006 not implemented: no null guard for `MdiActiveDocument`**  
   - `Commands/PoleBarnCommand.cs:28-30` and `Commands/PoleBarnCommand.cs:233-234` dereference `doc` immediately.

5. **FIX-007 not implemented: `as` cast null checks missing for BlockTable/ModelSpace BTR**  
   - `Commands/PoleBarnCommand.cs:80-81`, `Commands/PoleBarnCommand.cs:246-247`.

6. **FIX-005 not implemented: unknown truss type still silently falls back**  
   - `Generators/TrussProfiles/TrussFactory.cs:20` returns `CommonTrussProfile` for default instead of handling invalid enum explicitly.

7. **FIX-026 partially implemented: UI numeric parsing still culture-inconsistent**  
   - Still using culture-default `double.TryParse(...)` in:
     - `UI/OpeningManagerControl.xaml.cs:488-490,503,656-659`
     - `UI/MaterialsControl.xaml.cs:122-123`
   - Other UI files moved to invariant parse; this remains inconsistent.

8. **FIX-038 only partially implemented: window renderer fallback remains for most window types**  
   - `Generators/Renderers/RendererFactory.cs:39-45` still routes `Sliding`, `BarnSash`, `Awning`, `Casement` to `SingleHungWindowRenderer`.

9. **FIX-037 not implemented: BarnParameters still a monolithic god-model**  
   - `Models/BarnParameters.cs` remains large and cross-cutting (hundreds of lines, multiple domains/features tightly coupled).

10. **FIX-039 partially implemented: tests exist but no dedicated test project wiring**  
    - Test files were added, but no proper test project setup / package references in solution metadata present here.

11. **FIX-040 partially implemented: dead code still present**  
    - `Models/Analysis/StructuralAnalysis.cs:80` captures `maxLateral` but never uses it.

### Minor

12. **Potential duplicate geometry for curved footprints in plan view**  
    - `Generators/PlanViewGenerator.cs:29-48` draws curved footprint segments.
    - `Generators/PlanViewGenerator.cs:76-93` draws wall segments again, including same arcs on `PB-CURVED`.
    - May produce duplicated curved arc entities.

13. **One remaining silent catch outside generator core path**  
    - `UI/MaterialsControl.xaml.cs:99` still uses bare `catch { }` for structural pre-run fallback.

## 40-Fix Verification Matrix

### Phase 1
- FIX-001: **Implemented** (`Generators/GridBubbleGenerator.cs:27` uses `geo.Params`).
- FIX-002: **Implemented** (`Models/Loads/SnowLoadCalculator.cs:86-88`, no `dynamic`, clearer fallback).
- FIX-003: **Implemented** (`Models/BarnGeometry.cs:36`, null-safe truss profile use).
- FIX-004: **Implemented** (`Models/BarnGeometry.cs:425-430`, arc radius guard).
- FIX-005: **Not implemented** (`Generators/TrussProfiles/TrussFactory.cs:20`).
- FIX-006: **Not implemented** (`Commands/PoleBarnCommand.cs:28-30`, `233-234`).
- FIX-007: **Not implemented** (`Commands/PoleBarnCommand.cs:80-81`, `246-247`).
- FIX-008: **Implemented** (`Models/BarnParameters.cs:78`).
- FIX-009: **Implemented** (`Models/StructuralCalculations.cs:80-87` + tests).
- FIX-010: **Implemented** (`Generators/LeanToGenerator.cs:164-174`).
- FIX-011: **Implemented** (`Generators/PorchGenerator.cs:350-365` dedupe + count alignment).

### Phase 2
- FIX-012: **Not implemented** (many silent catches remain; see finding #3).
- FIX-013: **Implemented** (`Commands/PoleBarnCommand.cs:212,292` uses `SendStringToExecute`).
- FIX-014: **Implemented** (`UI/StructuralControl.xaml.cs:47-52` user-safe message).
- FIX-015: **Implemented** (`UI/OpeningManagerControl.xaml.cs:151,159,167`).
- FIX-016: **Implemented** (`Utils/WarningCollector.cs`, `Commands/PoleBarnCommand.cs:296-310`).

### Phase 3
- FIX-017: **Implemented** (`Models/Loads/WindLoadCalculator.cs:59,63,103`).
- FIX-018: **Implemented** (`Generators/EngineeringReportGenerator.cs:69-70`).
- FIX-019: **Implemented** (`Models/IndustryParameters.cs:82-85` with clone helpers).
- FIX-020: **Implemented** (`Generators/DimensionGenerator.cs:239-252`, `Utils/StyleManager.cs:29-93`).
- FIX-021: **Implemented** (`Utils/OpeningValidator.cs:165-173` uses geometry posts).
- FIX-022: **Implemented** (`Models/BarnGeometryPostPlacement.cs:8`).
- FIX-023: **Implemented** (preliminary/simplified disclaimers in structural/load/report classes).

### Phase 4
- FIX-024: **Implemented** (`Utils/LayerManager.cs:199,234-245` with `enforceStandards`).
- FIX-025: **Implemented** (PB- prefix standardized in `Utils/LayerManager.cs`).
- FIX-026: **Partially implemented** (remaining parse inconsistencies in OpeningManager/MaterialsControl).
- FIX-027: **Implemented** (`Generators/GridBubbleGenerator.cs:44` + `Generators/GridLabelGenerator.cs`).
- FIX-028: **Implemented** (`Commands/PoleBarnCommand.cs:219` command is `POLEBARN30X40`).
- FIX-029: **N/A/likely resolved by removal** (no `PlotStyleManager` source remains or references found).

### Phase 5
- FIX-030: **Implemented** (`Generators/MaterialReportGenerator.cs:123-154`).
- FIX-031: **Implemented** (`Utils/DrawingHelpers.cs` public methods guarded).
- FIX-032: **Implemented** (`Models/BarnGeometry.cs:417-500` hash bucket dedupe).
- FIX-033: **Implemented** (`Generators/Renderers/RendererFactory.cs:10-18` cached stateless instances).
- FIX-034: **Implemented** (`UI/PoleBarnDialog.xaml.cs:16-17,403-454` geometry cache).
- FIX-035: **Implemented** (`UI/PoleBarnDialog.xaml.cs:395-400` event detach).

### Phase 6
- FIX-036: **Implemented** (service extraction in `Generators/Services/*`).
- FIX-037: **Not implemented** (`BarnParameters` still monolithic).
- FIX-038: **Partially implemented** (only fixed/single/double-hung specialized).
- FIX-039: **Partially implemented** (tests added, but not a proper test project integration).
- FIX-040: **Partially implemented** (dead code remains, e.g., `StructuralAnalysis.cs:80`).

## Merge/Parallel-Branch Integration Assessment
- **Not clean**. At least one major branch merge mismatch is present (generator call signatures vs definitions), and test code integration is incomplete for current project structure.
- No textual conflict markers were found (`<<<<<<<`, `=======`, `>>>>>>>`), but semantic merge breakage exists.

## Remaining issues from original REVIEW-CODEX.md
Still unresolved from original set:
- Silent exception swallowing across generators/services (FIX-012 scope).
- Missing null guards/cast checks in command path (FIX-006, FIX-007).
- Truss factory unknown-type handling (FIX-005).
- Incomplete window renderer coverage (FIX-038).
- `BarnParameters` decomposition not completed (FIX-037).
- Test architecture not fully realized (FIX-039).

