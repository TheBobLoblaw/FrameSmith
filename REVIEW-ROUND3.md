# FrameSmith Round 3 Code Review

Date: 2026-03-07  
Scope: Static analysis only (no build/runtime), reviewing all `.cs` under `Commands/`, `Generators/`, `Models/`, `UI/`, `Utils/`, `Tests/`.

## Executive summary
Round 2 remediation is largely in place: **35 FIX items are fully fixed, 3 are partial, 2 remain broken**.  
However, I found **new high-impact defects** in structural math and quantity takeoff logic that can produce materially incorrect engineering/material outputs.

Top new risks:
1. Incorrect purlin force equations in structural analysis.
2. Snow-load minimum logic bug that overstates flat-roof snow for many inputs.
3. Multi-story post counting bug inflating foundation/hardware/material quantities.

## Round 2 fix verification matrix
| Fix ID | Status | Verification |
|---|---|---|
| FIX-001 | FIXED | `Generators/GridBubbleGenerator.cs:27` uses `geo.Params`. |
| FIX-002 | FIXED | `Models/Loads/SnowLoadCalculator.cs:86-88` uses typed lean-to access (no `dynamic`). |
| FIX-003 | FIXED | `Models/BarnGeometry.cs:44` initializes `TrussProfile` in ctor, null-safe `PeakHeight`. |
| FIX-004 | FIXED | `Models/BarnGeometry.cs:425-430` guards non-positive arc radius. |
| FIX-005 | BROKEN | `Generators/TrussProfiles/TrussFactory.cs:20` still silently falls back to `CommonTrussProfile` on unknown enum. |
| FIX-006 | FIXED | `Commands/PoleBarnCommand.cs:28-33` and `250-255` now null-guard active document. |
| FIX-007 | FIXED | `Commands/PoleBarnCommand.cs:86-98` and `269-281` now null-check `BlockTable`/`BlockTableRecord` after `as` casts. |
| FIX-008 | FIXED | `Models/BarnParameters.cs:78` guards `BaySpacing <= 0`. |
| FIX-009 | FIXED | `Models/StructuralCalculations.cs:80-94` 10'/12' paths now distinct/reachable. |
| FIX-010 | FIXED | `Generators/LeanToGenerator.cs:167-174` zero-length fascia guard added. |
| FIX-011 | FIXED | `Generators/PorchGenerator.cs:350-365` duplicate rail suppression with symmetric key handling. |
| FIX-012 | FIXED | Prior silent catches in plan/elevation/3D/services now report via `WarningCollector.Report(...)`. |
| FIX-013 | FIXED | `Commands/PoleBarnCommand.cs:229,326` uses `SendStringToExecute`. |
| FIX-014 | FIXED | `UI/StructuralControl.xaml.cs:47-52,86-90` user-safe messages; details logged to AutoCAD editor. |
| FIX-015 | FIXED | `UI/OpeningManagerControl.xaml.cs:151,159,167` converters return `Binding.DoNothing`. |
| FIX-016 | FIXED | `Utils/WarningCollector.cs` + command session usage (`Commands/PoleBarnCommand.cs:81,265`) aggregates warnings. |
| FIX-017 | FIXED | `Models/Loads/WindLoadCalculator.cs:59,63,103` importance factor used in qh and references. |
| FIX-018 | FIXED | `Generators/EngineeringReportGenerator.cs:69-70` uses velocity pressure label `qh`. |
| FIX-019 | FIXED | Deep clone helpers in `Models/DairyBarnParameters.cs:74-123` prevent reference sharing. |
| FIX-020 | FIXED | `Generators/DimensionGenerator.cs:239-252` unified through `StyleManager.GetDimensionStyleId`; `Utils/StyleManager.cs` owns style. |
| FIX-021 | FIXED | `Utils/OpeningValidator.cs:165-173` gets post positions from `BarnGeometry.Posts`. |
| FIX-022 | FIXED | `Models/BarnGeometryPostPlacement.cs:8` centralized threshold constant in use. |
| FIX-023 | FIXED | Preliminary/simplified disclaimers present in load/analysis/report codepaths. |
| FIX-024 | FIXED | `Utils/LayerManager.cs:199,234-245` has `enforceStandards` and update path. |
| FIX-025 | FIXED | `Utils/LayerManager.cs` now PB-prefixed layer constants throughout. |
| FIX-026 | FIXED | Invariant culture parsing now used in prior UI hotspots (`UI/OpeningManagerControl.xaml.cs`, `UI/MaterialsControl.xaml.cs`). |
| FIX-027 | FIXED | `Generators/GridLabelGenerator.cs:20-25` supports beyond 26 bays. |
| FIX-028 | FIXED | `Commands/PoleBarnCommand.cs:236` command is `POLEBARN30X40`. |
| FIX-029 | FIXED | No `PlotStyleManager` code/references found. |
| FIX-030 | FIXED | CSV injection mitigation in `Generators/MaterialReportGenerator.cs:139-154`. |
| FIX-031 | FIXED | Public `DrawingHelpers` methods now validate arguments (`Utils/DrawingHelpers.cs`). |
| FIX-032 | FIXED | `Models/BarnGeometry.cs:417-500` hash-bucket dedupe for sampled points. |
| FIX-033 | FIXED | Renderer instances cached in `Generators/Renderers/RendererFactory.cs:10-20`. |
| FIX-034 | FIXED | Geometry cache added in `UI/PoleBarnDialog.xaml.cs:16-17,403-454`. |
| FIX-035 | FIXED | Event detach cleanup in `UI/PoleBarnDialog.xaml.cs:395-400`. |
| FIX-036 | FIXED | Service extraction present under `Generators/Services/`. |
| FIX-037 | BROKEN | `Models/BarnParameters.cs` remains monolithic cross-domain model. |
| FIX-038 | PARTIAL | `RendererFactory` still maps `BarnSash` to sliding and `Casement` to single-hung (`Generators/Renderers/RendererFactory.cs:42-46`). |
| FIX-039 | PARTIAL | Separate test project exists, but test wiring still has compile risks (see new findings). |
| FIX-040 | FIXED | Prior cited dead var in `Models/Analysis/StructuralAnalysis.cs` removed. |

## New findings (not flagged in rounds 1-2)
| Severity | File:line | Issue |
|---|---|---|
| Critical | `Models/Analysis/StructuralAnalysis.cs:129-130` | **Incorrect purlin formulas**: `M = wL/8` and `V = w/2` are used instead of `M = wL^2/8` and `V = wL/2`. This materially understates design demand. |
| Critical | `Models/Loads/SnowLoadCalculator.cs:55-56` | **Flat-roof snow min logic bug**: computed `pfMin` is ignored; expression effectively forces minimum `20*Is` whenever `pg>0`. This overstates loads for lower snow regions. |
| Major | `Models/Materials/FoundationTakeoffCalculator.cs:20`; `Models/Materials/HardwareTakeoffCalculator.cs:21`; `Models/Materials/LumberTakeoffCalculator.cs:51-56` | **Multi-story quantity inflation**: uses `geometry.Posts.Count` (includes spliced story segments) as footing/anchor/post count; additionally applies embedment length to every post segment in lumber takeoff. |
| Major | `Generators/Services/OpeningDrawingService.cs:67-69,89-91,119-121,141-143` | **Elevation wall mix-up**: front elevation renders both front+back openings; side elevation renders both left+right. Non-matching opposite-wall openings will overlap incorrectly. |
| Major | `Generators/ExteriorDetailGenerator.cs:138,243` with missing checks in `Models/BarnParameters.cs:449-610` | **Cupola input not validated**: negative `Cupola.Count` can make `(cupola.Count + 1)` zero/negative, causing divide-by-zero/invalid spacing behavior. |
| Major | `Tests/Support/ModelStubs.cs:5-63` + `Tests/OpeningValidatorTests.cs:35,60` | **Type identity collision in tests**: stubs redefine `PoleBarnGenerator.Models.*` types while test project references plugin assembly with same full type names; calls into plugin APIs will have conflicting type identities. |
| Major | `Tests/FrameSmith.Tests.csproj:3,22` | **Likely TFM incompatibility**: test project targets `net8.0` but references plugin targeting `net8.0-windows`; this is typically an incompatible project reference configuration. |
| Minor | `UI/PoleBarnDialog.xaml.cs:416-453` | **Geometry cache key is incomplete**: excludes openings/lean-tos/porches/interior options, so cached geometry can become stale in dialog summary for those edits. |

## Overall grade
**B-**

Reasoning: substantial round-2 remediation completion, but new structural/takeoff correctness defects are serious and must be fixed before trusting engineering or material outputs.

## Prioritized fix recommendations
1. Fix structural equation defects first:
   - `StructuralAnalysis` purlin moment/shear equations.
   - `SnowLoadCalculator` flat-roof minimum load expression (`pfMin` application).
2. Correct post counting semantics across takeoff modules:
   - Use plan-instance posts for footing/anchor counts.
   - Separate embedded post lengths from upper splice segments in lumber takeoff.
3. Resolve elevation opening projection logic:
   - Render only the wall corresponding to each elevation view, or explicitly support rear/opposite views with offsets.
4. Add BarnParameters-level validation for cupola inputs (`Count >= 0`, reasonable size/spacing bounds).
5. Repair test project wiring:
   - Remove/rename conflicting model stubs, or isolate them in a separate namespace and adapter layer.
   - Align test TFM with plugin (`net8.0-windows`) or multi-target appropriately.
6. Improve cache-key completeness (or invalidate cache on any parameter object mutation) to avoid stale dialog summaries.
