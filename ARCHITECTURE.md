# Pole Barn Generator — AutoCAD .NET Plugin

## Architecture Overview

A C# .NET AutoCAD plugin that generates pole barn (post-frame) structures from user-input dimensions. Produces plan view, front elevation, side elevation, and a 3D wireframe — all placed into the active drawing with proper layers, dimensions, and annotation.

---

## Project Structure

```
PoleBarnGenerator/
├── PoleBarnPlugin/
│   ├── PoleBarnPlugin.csproj        # .NET project targeting AutoCAD API
│   ├── Commands/
│   │   └── PoleBarnCommand.cs       # IExtensionApplication + command definitions
│   ├── UI/
│   │   ├── PoleBarnDialog.xaml       # WPF input dialog layout
│   │   └── PoleBarnDialog.xaml.cs    # Dialog code-behind / ViewModel
│   ├── Generators/
│   │   ├── PlanViewGenerator.cs      # Top-down framing layout
│   │   ├── FrontElevationGenerator.cs# Front elevation view
│   │   ├── SideElevationGenerator.cs # Side elevation view
│   │   ├── Wireframe3DGenerator.cs   # 3D wireframe model
│   │   └── DimensionGenerator.cs     # Automatic dimensioning
│   ├── Models/
│   │   ├── BarnParameters.cs         # Input parameter data model
│   │   ├── StructuralMember.cs       # Post, girt, truss, purlin, etc.
│   │   └── BarnGeometry.cs           # Computed geometry from parameters
│   └── Utils/
│       ├── LayerManager.cs           # Layer creation and management
│       ├── DrawingHelpers.cs         # Common AutoCAD drawing utilities
│       └── UnitConverter.cs          # Imperial/metric conversion
├── docs/
│   ├── ARCHITECTURE.md               # This file
│   └── PARAMETER_REFERENCE.md        # Full parameter definitions
├── tests/
│   └── GeometryTests.cs              # Unit tests for geometry math
└── CLAUDE.md                         # Agent instructions for Claude Code
```

---

## Core Data Flow

```
User Input (WPF Dialog)
    │
    ▼
BarnParameters (validated model)
    │
    ▼
BarnGeometry (computed points, members, connections)
    │
    ├──▶ PlanViewGenerator      → Entities on "PLAN-*" layers
    ├──▶ FrontElevationGenerator → Entities on "ELEV-FRONT-*" layers
    ├──▶ SideElevationGenerator  → Entities on "ELEV-SIDE-*" layers
    ├──▶ Wireframe3DGenerator    → 3D lines on "3D-*" layers
    └──▶ DimensionGenerator      → Dims on "*-DIM" layers
```

---

## Key Design Decisions

### AutoCAD API References
- **AcDbMgd.dll** — Database/entity access (Lines, Polylines, Circles, etc.)
- **AcMgd.dll** — Application/Editor/Document access
- **AcCoreMgd.dll** — Core runtime

Target **AutoCAD 2024+** (.NET 4.8 or .NET 8 depending on version).

### Layer Scheme
| Layer Name         | Color | Purpose                    |
|--------------------|-------|----------------------------|
| PB-POSTS           | 1     | Post locations (all views) |
| PB-GIRTS           | 3     | Horizontal girts           |
| PB-TRUSSES         | 5     | Truss outlines             |
| PB-PURLINS         | 4     | Roof purlins               |
| PB-RAFTERS         | 6     | Rafter lines               |
| PB-ROOF            | 2     | Roof outline/sheathing     |
| PB-SLAB            | 8     | Slab/foundation outline    |
| PB-DOORS           | 30    | Door openings              |
| PB-WINDOWS         | 40    | Window openings            |
| PB-DIM             | 7     | Dimensions                 |
| PB-ANNO            | 7     | Annotations/labels         |
| PB-3D              | 150   | 3D wireframe members       |

### Coordinate Layout
Views are placed side-by-side in model space with a configurable gap:
- **Plan View**: Origin at (0, 0)
- **Front Elevation**: Offset to the right of plan
- **Side Elevation**: Offset further right
- **3D Wireframe**: Placed below or at a separate offset

---

## Input Parameters (BarnParameters)

### Primary Dimensions
| Parameter        | Type   | Unit    | Description                     |
|------------------|--------|---------|---------------------------------|
| BuildingWidth    | double | feet    | Overall width (sidewall to sidewall) |
| BuildingLength   | double | feet    | Overall length (endwall to endwall)  |
| EaveHeight       | double | feet    | Height at eave (ground to top plate) |
| RoofPitch        | string | X/12    | Roof pitch (e.g., "4/12", "6/12")   |
| BaySpacing       | double | feet    | On-center spacing between bays  |

### Structural Options
| Parameter        | Type   | Unit    | Description                     |
|------------------|--------|---------|---------------------------------|
| PostSize         | string | inches  | Post cross-section (e.g., "6x6") |
| GirtSpacing      | double | inches  | Vertical spacing of wall girts  |
| PurlinSpacing    | double | inches  | On-center purlin spacing        |
| TrussType        | enum   | —       | Common, Scissor, Mono-slope     |
| OverhangEave     | double | feet    | Eave overhang distance          |
| OverhangGable    | double | feet    | Gable overhang distance         |

### Openings
| Parameter             | Type   | Description                          |
|-----------------------|--------|--------------------------------------|
| Doors[]               | list   | Collection of door definitions       |
| Door.Wall             | enum   | Front, Back, Left, Right             |
| Door.Type             | enum   | Overhead, Sliding, Walk              |
| Door.Width            | double | Opening width (feet)                 |
| Door.Height           | double | Opening height (feet)                |
| Door.CenterOffset     | double | Distance from left corner of wall    |
| Windows[]             | list   | Collection of window definitions     |
| Window.Wall           | enum   | Front, Back, Left, Right             |
| Window.Width          | double | Opening width (feet)                 |
| Window.Height         | double | Opening height (feet)                |
| Window.SillHeight     | double | Height from ground to sill           |
| Window.CenterOffset   | double | Distance from left corner of wall    |

### Output Options
| Parameter         | Type | Description                          |
|-------------------|------|--------------------------------------|
| GeneratePlan      | bool | Generate plan view                   |
| GenerateFront     | bool | Generate front elevation             |
| GenerateSide      | bool | Generate side elevation              |
| Generate3D        | bool | Generate 3D wireframe                |
| AddDimensions     | bool | Auto-dimension the views             |
| DimensionStyle    | string | Named dim style to use             |
| DrawingScale      | string | e.g., "1/4\" = 1'-0\""            |

---

## Generator Responsibilities

### PlanViewGenerator
- Draw post locations as filled rectangles at each bay intersection
- Draw slab/foundation outline
- Draw girt lines along sidewalls and endwalls
- Draw truss ridge line (dashed)
- Place door and window openings as breaks in wall lines
- Label bays and overall dimensions

### FrontElevationGenerator
- Draw front endwall posts (visible)
- Draw eave line, peak, and roof slope
- Draw girts as horizontal lines between posts
- Draw visible door/window openings on front wall
- Show overhang profile
- Dimension: eave height, peak height, building width

### SideElevationGenerator
- Draw sidewall posts at each bay
- Draw eave line and roof slope (showing pitch)
- Draw girts between posts
- Draw visible door/window openings on side walls
- Dimension: bay spacing, eave height, building length

### Wireframe3DGenerator
- Create 3D Line entities for all posts (vertical)
- Connect posts with girt lines at each girt elevation
- Draw truss outlines in 3D (bottom chord, top chords, web members)
- Draw purlins along roof slope
- Draw ridge line
- All placed at actual 3D coordinates (not projected)

### DimensionGenerator
- Add aligned dimensions for overall building size
- Add linear dims for bay spacing
- Add angular or text annotation for roof pitch
- Add height dims on elevations
- Respect the user's chosen dim style or create a default

---

## Implementation Phases

### Phase 1: Foundation
- [ ] Project setup, NuGet refs, build targeting AutoCAD
- [ ] BarnParameters model with validation
- [ ] LayerManager utility
- [ ] Basic command registration (POLEBARN command)
- [ ] Minimal WPF dialog with primary dimension inputs

### Phase 2: Plan View
- [ ] Post placement at grid intersections
- [ ] Wall outline (foundation/slab)
- [ ] Girt representation
- [ ] Truss ridge line
- [ ] Basic dimensioning

### Phase 3: Elevations
- [ ] Front elevation with posts, girts, roof profile
- [ ] Side elevation with bay spacing, posts, roof slope
- [ ] Door/window openings in elevations
- [ ] Elevation dimensioning

### Phase 4: 3D Wireframe
- [ ] 3D post lines
- [ ] 3D girt connections
- [ ] 3D truss geometry
- [ ] 3D purlins and ridge

### Phase 5: Polish
- [ ] Full WPF dialog with tabs (Dims, Structure, Openings, Output)
- [ ] Preset templates (24x30, 30x40, 40x60, etc.)
- [ ] Input validation and error handling
- [ ] Door/window opening manager in dialog
- [ ] Save/load parameter sets to JSON
