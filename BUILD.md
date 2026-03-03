# FrameSmith - Build & Deployment Guide

## Prerequisites

### Development Environment
- **Visual Studio 2019/2022** (Community, Professional, or Enterprise)
- **.NET Framework 4.8 SDK** (for AutoCAD 2024 compatibility)
- **AutoCAD 2024 or newer** installed for testing
- **Git** for source control

### Alternative: .NET 8
For **AutoCAD 2025+**, you can target .NET 8 by changing the project file:
```xml
<TargetFramework>net8.0-windows</TargetFramework>
```

## Build Instructions

### 1. Clone Repository
```bash
git clone https://github.com/TheBobLoblaw/FrameSmith.git
cd FrameSmith
```

### 2. Configure AutoCAD Path
Edit `PoleBarnPlugin.csproj` and update the AutoCAD installation path:
```xml
<AutoCADPath>C:\Program Files\Autodesk\AutoCAD 2024\</AutoCADPath>
```

Common paths:
- **AutoCAD 2024**: `C:\Program Files\Autodesk\AutoCAD 2024\`
- **AutoCAD 2025**: `C:\Program Files\Autodesk\AutoCAD 2025\`

### 3. Build in Visual Studio
1. Open `PoleBarnPlugin.csproj` in Visual Studio
2. **Build → Build Solution** (or `Ctrl+Shift+B`)
3. Output: `bin\Debug\PoleBarnGenerator.dll` or `bin\Release\PoleBarnGenerator.dll`

### 4. Build from Command Line (Optional)
```cmd
# Using Visual Studio Developer Command Prompt
msbuild PoleBarnPlugin.csproj /p:Configuration=Release

# Or using dotnet CLI (if available)
dotnet build --configuration Release
```

## Deployment

### Method 1: Manual Load (Recommended for Testing)
1. Copy `PoleBarnGenerator.dll` to a known location (e.g., `C:\AutoCAD\Plugins\`)
2. Open AutoCAD
3. Type `NETLOAD` and press Enter
4. Browse to and select `PoleBarnGenerator.dll`
5. Type `POLEBARN` to launch the generator

### Method 2: Auto-load via Registry (Production)
Create a registry entry to auto-load the plugin:

**Registry Path**: `HKEY_CURRENT_USER\SOFTWARE\Autodesk\AutoCAD\R24.0\ACAD-4001:409\Applications\PoleBarnGenerator`

**Values**:
- `DESCRIPTION` (String): "Pole Barn Structure Generator"
- `LOADCTRLS` (DWORD): `14` (load on demand + command invoke)
- `LOADER` (String): `"C:\Path\To\PoleBarnGenerator.dll"`

### Method 3: Bundle Installer (Advanced)
Create an MSI installer that:
1. Copies DLL to `%APPDATA%\Autodesk\ApplicationPlugins\PoleBarnGenerator\`
2. Creates registry entries
3. Installs desktop shortcuts

## Usage

1. Launch AutoCAD
2. Type `POLEBARN` at the command line
3. Fill out the WPF dialog with building parameters:
   - **Primary Dimensions**: Width, Length, Eave Height
   - **Structural Options**: Post size, Bay spacing, Roof pitch
   - **Openings**: Doors and windows (optional)
   - **Output Options**: Which views to generate
4. Click **Generate**
5. Views will be created in model space with proper layers

## Generated Layers

| Layer Name | Purpose | Color |
|------------|---------|-------|
| `PB-POSTS` | Post locations | Red (1) |
| `PB-GIRTS` | Horizontal girts | Green (3) |
| `PB-TRUSSES` | Truss outlines | Magenta (5) |
| `PB-PURLINS` | Roof purlins | Cyan (4) |
| `PB-RAFTERS` | Rafter lines | Blue (6) |
| `PB-ROOF` | Roof outline | Yellow (2) |
| `PB-SLAB` | Foundation outline | White (8) |
| `PB-DOORS` | Door openings & frames | Orange (30) |
| `PB-WINDOWS` | Window openings & frames | Lt Orange (40) |
| `PB-HEADERS` | Structural headers above openings | Brown (22) |
| `PB-DIM` | Dimensions | White (7) |
| `PB-ANNO` | Annotations | White (7) |
| `PB-3D` | 3D wireframe | Gray (150) |



## Opening System (Sprint 1)

### Supported Door Types
| Type | Plan Symbol | Elevation Details |
|------|-------------|-------------------|
| **Overhead** | Frame + track lines (CENTER linetype) | Sectional panels, track type label (OH/HL/VL) |
| **Walk** | 90 degree swing arc from hinge side | Frame, threshold, handle indication |
| **Sliding** | Track line + direction arrow | Vertical panel divisions, track hardware |
| **Dutch** | Dual swing arcs (full + 85% radius) | Horizontal split line at split height |
| **Double** | Paired swing arcs from center | Meeting stile, paired frame |

### Supported Window Types
| Type | Plan Symbol | Elevation Details |
|------|-------------|-------------------|
| **Fixed** | Frame rectangle + glass line | Outer frame, sill, inner sash |
| **Single Hung** | Frame rectangle + glass line | Meeting rail at mid-height, upper/lower sash |
| **Sliding** | (uses Single Hung renderer) | Same as Single Hung |

### Grid Patterns
- **Colonial**: Vertical center line in each sash
- **Prairie**: Vertical lines at 25% inset from edges

### Structural Headers
Headers are automatically sized based on span width:
| Span | Header |
|------|--------|
| 4 ft or less | (2) 2x6 DF |
| 6 ft or less | (2) 2x8 DF |
| 8 ft or less | (2) 2x10 DF |
| 10 ft or less | (2) 2x12 DF |
| 14 ft or less | (3) 2x12 DF |
| 20 ft or less | 3.5x11.875 LVL |
| 24 ft or less | 5.25x11.875 LVL |
| Over 24 ft | 7.0x11.875 LVL |

### Conflict Detection
The validator checks:
- **Edge clearance**: Min 6 inches from opening edge to wall corner
- **Opening spacing**: Min 6 inches between adjacent openings
- **Height limits**: Opening cannot exceed eave height
- **Post interference**: Openings cannot overlap structural posts
- **Sliding clearance**: Sliding doors need clear wall space equal to door width

### UI Workflow
1. Go to the **Openings** tab in the main dialog
2. Use **Quick Presets** buttons to add common configurations
3. Or click **Add Door/Window** for custom openings
4. Select an opening in the list to edit properties in the detail panel
5. Validation runs automatically with debounced 300ms timer
6. Structural info (header size, rough opening) updates in real-time

### Advanced Configurations

**Overhead door with high-lift track:**
Add overhead door preset, then change Track Type to High Lift.
Plan view shows extended track lines with DASHED linetype.

**Dutch door with custom split:**
Add Dutch door, then set Split Height (default 3.5 ft).
Elevation shows horizontal split line; plan shows dual arcs.

**Multiple openings per wall:**
Add openings freely. Validator prevents overlaps.
Duplicate button copies selected opening with offset to avoid conflict.

## Troubleshooting

### Build Issues
- **"Could not load file or assembly"**: Check AutoCAD path in `.csproj`
- **"Assembly not found"**: Verify AutoCAD 2024+ is installed
- **".NET Framework version"**: Ensure .NET 4.8 SDK is installed

### Runtime Issues
- **"Command POLEBARN not found"**: Plugin not loaded or registration failed
- **"System.IO.FileNotFoundException"**: Missing AutoCAD assemblies
- **"WPF dialog doesn't appear"**: Check UI thread and modal window setup

### Layer Issues
- **Entities on wrong layers**: Check `LayerManager.CreateLayers()` call
- **Colors not showing**: Verify layer color assignments

### Opening Issues
- **Overlapping openings error**: Adjust CenterOffset to space openings apart (min 6 inch gap)
- **Exceeds eave height**: Reduce door height or increase eave height
- **Conflicts with structural post**: Move opening away from post positions (corners + bay intervals)
- **Missing swing arc**: Check WalkDoorRenderer has valid hinge/free positions
- **No grid lines**: Ensure HasGrid is true AND GridPattern is not None
- **Header not showing**: Verify door height is less than eave height (header draws above door)

## Development Notes

### Code Structure
```
Commands/
├── PoleBarnCommand.cs      # POLEBARN command entry point
UI/
├── PoleBarnDialog.xaml     # WPF input dialog (tabbed: Dimensions, Structure, Openings, Output)
├── PoleBarnDialog.xaml.cs  # Dialog code-behind
├── OpeningManagerControl.xaml    # Opening management user control
├── OpeningManagerControl.xaml.cs # Opening manager logic + view models
├── OpeningPresets.cs             # Quick-add presets for common door/window configs
Generators/
├── PlanViewGenerator.cs    # Top-down view
├── FrontElevationGenerator.cs # Front elevation
├── SideElevationGenerator.cs  # Side elevation
├── Wireframe3DGenerator.cs    # 3D wireframe
├── DimensionGenerator.cs      # Automatic dimensioning
├── Renderers/
│   ├── IOpeningRenderer.cs     # Strategy interfaces for door/window rendering
│   ├── RendererFactory.cs      # Factory selecting renderer by opening type
│   ├── WallGeometry.cs         # Wall-relative coordinate transforms
│   ├── OverheadDoorRenderer.cs # Sectional panels + track indicators
│   ├── WalkDoorRenderer.cs     # Swing arc + frame (also handles Double)
│   ├── SlidingDoorRenderer.cs  # Track line + slide direction arrow
│   ├── DutchDoorRenderer.cs    # Dual swing arcs at split height
│   └── SingleHungWindowRenderer.cs # Sash + meeting rail + grid patterns
Models/
├── BarnParameters.cs       # Input parameter model (+ DoorOpening, WindowOpening types)
├── BarnGeometry.cs         # Computed structural geometry
├── StructuralCalculations.cs # Header sizing (sawn lumber + LVL beams)
Utils/
├── LayerManager.cs         # AutoCAD layer management
├── DrawingHelpers.cs       # Common drawing utilities
├── OpeningValidator.cs     # Conflict detection & clearance validation
```

### Adding Features
1. **New parameters**: Add to `BarnParameters.cs` with validation
2. **New geometry**: Extend `BarnGeometry.cs` calculations
3. **New drawing elements**: Use `DrawingHelpers` utilities
4. **New layers**: Add to `LayerManager.Layers` enum

### Testing
- **Unit tests**: Focus on `BarnGeometry` calculations
- **Manual testing**: Use AutoCAD with various building sizes
- **Edge cases**: Very small/large buildings, unusual proportions

## License
[Add your license information here]

## Support
[Add contact/support information here]