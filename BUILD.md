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
| `PB-DOORS` | Door openings | Red (30) |
| `PB-WINDOWS` | Window openings | Blue (40) |
| `PB-DIM` | Dimensions | White (7) |
| `PB-ANNO` | Annotations | White (7) |
| `PB-3D` | 3D wireframe | Gray (150) |

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

## Development Notes

### Code Structure
```
Commands/
├── PoleBarnCommand.cs      # POLEBARN command entry point
UI/
├── PoleBarnDialog.xaml     # WPF input dialog
├── PoleBarnDialog.xaml.cs  # Dialog code-behind
Generators/
├── PlanViewGenerator.cs    # Top-down view
├── FrontElevationGenerator.cs # Front elevation
├── SideElevationGenerator.cs  # Side elevation
├── Wireframe3DGenerator.cs    # 3D wireframe
├── DimensionGenerator.cs      # Automatic dimensioning
Models/
├── BarnParameters.cs       # Input parameter model
├── BarnGeometry.cs         # Computed structural geometry
Utils/
├── LayerManager.cs         # AutoCAD layer management
├── DrawingHelpers.cs       # Common drawing utilities
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