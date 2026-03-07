# FrameSmith Remediation Plan

Combined findings from Codex + Sonnet code reviews (March 7, 2026).

## Phase 1: Critical Bugs & Compile Fixes (Sprint 11)
**Goal:** Make it build and not crash.

- [ ] **FIX-001** GridBubbleGenerator.cs:27 — `geo.Parameters` → `geo.Params` (compile break)
- [ ] **FIX-002** SnowLoadCalculator.cs:84 — Remove `dynamic` cast, fix null-coalescing precedence
- [ ] **FIX-003** BarnGeometry.cs:36 — TrussProfile null dereference before Compute()
- [ ] **FIX-004** BarnGeometry.cs:424 — Division by zero when ArcRadius=0
- [ ] **FIX-005** TrussFactory.cs:21 — Unhandled ArgumentException for unknown truss types
- [ ] **FIX-006** PoleBarnCommand.cs:28,182 — Null guard on MdiActiveDocument
- [ ] **FIX-007** PoleBarnCommand.cs:77-78 — Null checks after `as` casts (BlockTable, BTR)
- [ ] **FIX-008** BarnParameters.cs:78 — BaySpacing=0 divide-by-zero guard
- [ ] **FIX-009** StructuralCalculations.cs:82-83 — Unreachable code, wrong header sizing for 10-12ft spans
- [ ] **FIX-010** LeanToGenerator.cs:164-167 — Zero-length entity (degenerate fascia line)
- [ ] **FIX-011** PorchGenerator.cs:339-343 — Duplicate 3D railing + missing count++

## Phase 2: Exception Handling & Reporting (Sprint 12)
**Goal:** Stop hiding failures. Surface errors properly.

- [ ] **FIX-012** All generators — Replace silent catch blocks with specific catches + editor warnings
- [ ] **FIX-013** PoleBarnCommand.cs:161,223 — Replace ed.Command ZOOM with safer SendStringToExecute
- [ ] **FIX-014** UI/StructuralControl.xaml.cs:46 — Don't show stack traces to users
- [ ] **FIX-015** UI/OpeningManagerControl.xaml.cs:151,159,167 — ConvertBack return Binding.DoNothing
- [ ] **FIX-016** Aggregate warnings/errors during generation, show summary to user at end

## Phase 3: Data Integrity & Correctness (Sprint 13)
**Goal:** Make the math and data right.

- [ ] **FIX-017** WindLoadCalculator — Actually use WindParameters.ImportanceFactor in equations
- [ ] **FIX-018** EngineeringReportGenerator.cs:67 — Fix mislabel: velocity pressure (qh psf) != wind speed (V mph)
- [ ] **FIX-019** IndustryParameters.cs:81-83 — Deep clone Doors/Windows/Dairy in FromBarn
- [ ] **FIX-020** StyleManager.cs:42 vs DimensionGenerator.cs:261 — Unify FS-DIM style definition
- [ ] **FIX-021** OpeningValidator.cs:149-175 — Validate against actual BarnGeometry.Posts
- [ ] **FIX-022** OpeningValidator.cs:160 — Fix hardcoded 20ft threshold (should be 24ft)
- [ ] **FIX-023** Models/Loads — Mark simplified calcs as preliminary, isolate assumptions

## Phase 4: Standards & Consistency (Sprint 14)
**Goal:** Clean up naming, layers, UI patterns.

- [ ] **FIX-024** LayerManager.cs:234-242 — Don't clobber existing layer properties
- [ ] **FIX-025** LayerManager.cs — Standardize layer prefix (FS-S- vs PB-)
- [ ] **FIX-026** UI numeric parsing — Unify culture handling (invariant everywhere)
- [ ] **FIX-027** GridBubbleGenerator.cs:44 — Support >26 bays (AA..AZ labeling)
- [ ] **FIX-028** PoleBarnCommand.cs:168 — Fix command name POLEBARN3040 → POLEBARN30X40
- [ ] **FIX-029** PlotStyleManager — Rename to doc-focused or implement real CTB

## Phase 5: Security & Performance (Sprint 15)
**Goal:** Harden and optimize.

- [ ] **FIX-030** MaterialReportGenerator.cs:123-133 — CSV injection protection
- [ ] **FIX-031** DrawingHelpers.cs — Add null/validation guards on all public methods
- [ ] **FIX-032** BarnGeometry.cs:452-463 — O(n2) AddUniquePoint → HashSet approach
- [ ] **FIX-033** RendererFactory — Cache stateless renderer instances
- [ ] **FIX-034** UI/PoleBarnDialog.xaml.cs:176 — Cache geometry for UI display
- [ ] **FIX-035** UI/PoleBarnDialog.xaml.cs:22-23 — Detach event handlers (memory leak)

## Phase 6: Architecture & Testing (Sprint 16)
**Goal:** Long-term maintainability.

- [ ] **FIX-036** Extract per-feature drawing services from monolithic generators
- [ ] **FIX-037** Break up BarnParameters god-model into composable modules
- [ ] **FIX-038** Implement specific window renderers (not just SingleHung fallback)
- [ ] **FIX-039** Add unit test project — geometry math, load calcs, validator edge cases
- [ ] **FIX-040** Clean dead code (unused variables, misleading stubs)
