# Plugin.Maui.DebugOverlay

Plugin.Maui.DebugOverlay is a .NET MAUI plugin that provides debug overlay functionality including a visual debug ribbon and comprehensive visual tree dumping capabilities for MAUI applications.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Repository Setup and Requirements
- Install .NET 8.0 or later SDK
- Install .NET MAUI workload: `dotnet workload install maui`
- CRITICAL: Repository currently targets .NET 10 (not yet released) causing build issues
- Repository has missing MauiVersion property definition in build configuration

### Build Process - CURRENT LIMITATIONS
- **BROKEN BUILD**: The repository currently cannot be built due to configuration issues:
  - Main library targets `net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0` which are not available
  - Sample app targets `net8.0-android;net8.0-ios;net8.0-maccatalyst` which require MAUI workload
  - Missing `$(MauiVersion)` property definition causes package reference failures
- **Required Fix**: Update library target frameworks to `net8.0-*` to match sample and stable SDK
- **MauiVersion Property**: Add to Directory.Build.props or define in project files (e.g., `<MauiVersion>8.0.91</MauiVersion>`)
- DO NOT attempt to build until these configuration issues are resolved
- Expected build command when working: `dotnet build src/Plugin.Maui.DebugOverlay.sln -c Debug`
- NEVER CANCEL: MAUI builds typically take 5-15 minutes. Set timeout to 30+ minutes.

### Temporary Build Workaround (If Needed)
If you need to test compilation of individual files:
1. Create temporary test project with net8.0 target
2. Copy plugin source files to test project  
3. Add MAUI package references manually
4. Test compilation and basic functionality

### Project Structure
- **Main Library**: `src/Plugin.Maui.DebugOverlay/Plugin.Maui.DebugOverlay.csproj`
- **Sample Application**: `samples/Plugin.Maui.DebugOverlay.Sample/Plugin.Maui.DebugOverlay.Sample.csproj`
- **Core Components**:
  - `DebugOverlay.cs` - Main overlay window management
  - `DebugRibbonElement.cs` - Corner ribbon UI element  
  - `DebugOverlayPanel.cs` - Interactive debug panel with tree view
  - `VisualTreeDumpService.cs` - Visual tree analysis and dumping
  - `MauiProgramExtensions.cs` - Extension methods for MAUI app setup

### Validation Scenarios
When making changes, ALWAYS test these complete scenarios:

#### Basic Integration Test
1. Add plugin to a MAUI app via `.UseDebugRibbon(Colors.Orange)` in `MauiProgram.cs`
2. Run app in DEBUG mode on at least one platform
3. Verify debug ribbon appears in top-left corner with "DEBUG" text
4. Tap ribbon to show/hide debug panel - NEVER CANCEL: UI interaction may take 10-30 seconds

#### Visual Tree Dump Test  
1. Open debug panel by tapping ribbon
2. Tap "🔍 Dump Visual Tree" button
3. Verify output appears in debug console
4. Check that dump files are created in `{AppDataDirectory}/debug-dumps/`
5. Validate tree structure shows proper hierarchy with indentation

#### Shell Hierarchy Test
1. In debug panel, tap "🧭 Dump Shell Hierarchy" button  
2. Verify Shell navigation structure is dumped to console and file
3. Confirm both latest and timestamped files are created

#### Platform Validation
- Test ribbon visibility on iOS, Android, Windows, macOS
- Verify graphics rendering works consistently across platforms
- Confirm debug-only exclusion in Release builds

## Configuration and Usage

### Plugin Integration
```csharp
// In MauiProgram.cs - typical setup
public static MauiApp CreateMauiApp()
{
    var builder = MauiApp.CreateBuilder();
    builder
        .UseMauiApp<App>()
        .UseDebugRibbon(Colors.Orange)  // Enable debug overlay
        .ConfigureFonts(fonts =>
        {
            fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
        });

    return builder.Build();
}

// Custom color examples
builder.UseDebugRibbon(Color.FromArgb("#FF6B35"));      // Specific hex color
builder.UseDebugRibbon(Colors.Red);                     // Named color
builder.UseDebugRibbon(Color.FromArgb("#FF3300"));      // Sample app uses this
```

### Extension Method Details
- **Method**: `UseDebugRibbon(this MauiAppBuilder builder, Color ribbonColor = null)`
- **Default Color**: `Colors.MediumPurple` if no color specified
- **Debug Only**: Automatically wrapped in `#if DEBUG` preprocessor directive
- **Handler Registration**: Uses `WindowHandler.Mapper.AppendToMapping` to add overlay to windows

### Key Features to Test
- **Debug Ribbon**: Corner ribbon showing "DEBUG" or MAUI version
- **Interactive Panel**: Tap ribbon to show/hide debug tools
- **Visual Tree Dump**: Comprehensive layout and handler information
- **Shell Hierarchy**: Navigation structure analysis
- **File Output**: Dumps saved to app data directory
- **Cross-Platform**: Consistent behavior across all MAUI platforms

### Development Commands

#### When Build Issues Are Fixed
```bash
# Build plugin library - NEVER CANCEL: Takes 5-15 minutes
dotnet build src/Plugin.Maui.DebugOverlay.sln -c Debug

# Build sample app - NEVER CANCEL: Takes 5-15 minutes  
dotnet build samples/Plugin.Maui.DebugOverlay.Sample.sln -c Release

# Pack for NuGet - NEVER CANCEL: Takes 2-5 minutes
dotnet pack src/Plugin.Maui.DebugOverlay.sln -c Debug
```

#### Platform-Specific Testing
- **Android**: Use emulator or device, check debug console in VS/IDE
- **iOS**: Use simulator or device, monitor Xcode console
- **Windows**: Run as Windows app, check Debug Output window
- **macOS**: Use Mac Catalyst target, verify overlay rendering

### Troubleshooting

#### Build Failures
- Current repository state has net10.0 targeting issues - document as known limitation
- Missing MAUI workload: Run `dotnet workload install maui`
- Missing MauiVersion property: Needs to be defined in Directory.Build.props or project files

#### Runtime Issues
- **Ribbon not appearing**: Verify DEBUG configuration and `UseDebugRibbon()` call
- **Panel not opening**: Check debug console for tap handling errors
- **No dump output**: Verify file permissions and app data directory access
- **Platform-specific failures**: Check platform handler registration and graphics support

### CI/CD Information
- **Build Workflow**: `.github/workflows/ci.yml` builds plugin on Windows
- **Sample Workflow**: `.github/workflows/ci-sample.yml` builds sample app
- **Release Process**: `.github/workflows/release-nuget.yml` publishes to NuGet on version tags
- All workflows use `dotnet build` with Debug/Release configurations

### File Locations Reference

#### Plugin Source Files
- `src/Plugin.Maui.DebugOverlay/` - Main plugin implementation
- `src/Plugin.Maui.DebugOverlay/README.md` - Detailed plugin documentation

#### Sample Application  
- `samples/Plugin.Maui.DebugOverlay.Sample/` - Example integration
- `samples/Plugin.Maui.DebugOverlay.Sample/MauiProgram.cs` - Setup example

#### Debug Output Locations (Runtime)
- Debug Console in IDE
- `{AppDataDirectory}/debug-dumps/visual-tree-dump_latest.txt`
- `{AppDataDirectory}/debug-dumps/shell-hierarchy-dump_latest.txt`
- Timestamped dump files with format `*_YYYY-MM-DD_HH-mm-ss.txt`

### Platform Support
- ✅ iOS 14.2+
- ✅ Android API 21+  
- ✅ Windows 10 version 1809+
- ✅ macOS 14.0+

### Common Tasks
CRITICAL: Always manually test plugin functionality after making changes. Simply building is not sufficient validation.

1. **Adding new debug features**: 
   - Extend `DebugOverlayPanel.cs` and test tap handling
   - Add new buttons to main menu area (Visual Tree, Shell Hierarchy examples)
   - Test button rendering and response across platforms

2. **Modifying visual tree output**: 
   - Update `VisualTreeDumpService.cs` DumpOptions configuration
   - Validate dump format with different MAUI controls (Button, Label, Layout, etc.)
   - Test file output and console output simultaneously

3. **Changing ribbon appearance**: 
   - Modify `DebugRibbonElement.cs` graphics rendering code
   - Test cross-platform rendering consistency
   - Verify ribbon position and size on different screen densities

4. **Platform-specific fixes**: 
   - Check platform-conditional compilation in project files
   - Test handler behavior on specific platforms
   - Validate overlay window behavior with different MAUI navigation patterns

### Debug Panel Interaction Details
- **Panel Toggle**: Tap ribbon to show/hide - panel slides in from top-left
- **Button Layout**: Main menu shows buttons vertically in panel area
- **Tree View Mode**: After dump, panel switches to tree navigation view
- **Back Navigation**: Use back button (🔙) to return to main menu from tree view
- **Scroll Support**: Tree view supports scrolling for large hierarchies

### Visual Tree Dump Content Analysis
Test that dumps include these critical elements:
- **Layout Properties**: X, Y, Width, Height, Margin, Padding values
- **Handler Information**: Platform-specific handler types (iOS UIView, Android View, etc.)
- **MAUI Control Types**: Button, Label, Grid, StackLayout identification
- **Text Content**: Actual text values from Labels, Buttons for easier debugging
- **Hierarchy Structure**: Proper indentation showing parent-child relationships
- **MauiReactor Detection**: Special handling for MauiReactor component libraries

### Expected Repository State Issues
- **Build Configuration**: Repository targets unreleased .NET 10, needs updating to stable versions
- **Missing Dependencies**: MauiVersion property undefined causing package reference failures  
- **Workload Requirements**: MAUI workload installation required but may not be available in all environments

### Quick Repository Assessment Commands
```bash
# Check current .NET SDK version
dotnet --version

# Check installed workloads  
dotnet workload list

# Check for missing MauiVersion property
grep -r "MauiVersion" --include="*.csproj" src/ samples/

# Validate project target frameworks
grep -r "TargetFrameworks" --include="*.csproj" src/ samples/
```

### Sample Application Usage Pattern
The sample app demonstrates minimal integration:
- Uses Shell navigation with single ContentPage
- Adds debug overlay with red color: `Color.FromArgb("#FF3300")`
- No additional UI elements - focuses on plugin functionality
- Supports all major platforms: Android, iOS, Windows, macOS, Tizen

Always document actual timings and failures when testing commands, and include "NEVER CANCEL" warnings for any operations that may take more than 2 minutes.