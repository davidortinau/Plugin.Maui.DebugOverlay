# Plugin.Maui.DebugOverlay

A powerful debug overlay plugin for .NET MAUI applications that provides visual tree inspection and debugging tools directly in your app. Features a sleek ribbon interface and comprehensive visual tree dumping capabilities.

## Features

- **Debug Ribbon**: Elegant corner ribbon that displays "DEBUG" by default, shows MAUI version when panel is open
- **Interactive Debug Panel**: Tap the ribbon to show/hide a debug panel with:
  - Current MAUI version display
  - Visual tree dump functionality
  - Shell hierarchy dump functionality
- **Comprehensive Visual Tree Analysis**: 
  - Layout properties (size, position, margins, etc.)
  - Handler and platform view information
  - MauiReactor component detection
  - Text content identification for easier debugging
  - Hierarchical tree structure with indentation
- **Cross-Platform Graphics**: Uses MAUI's graphics layer for consistent experience across all platforms
- **Debug-Only**: Automatically excluded from release builds

## Installation

### NuGet Package
```xml
<PackageReference Include="Plugin.Maui.DebugOverlay" Version="1.0.0" />
```

### Manual Installation
1. Add the project reference to your MAUI app
2. Follow the setup instructions below

## Setup

Add the debug overlay to your MAUI app in `MauiProgram.cs`:

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

#if DEBUG
        // Add debug ribbon overlay (only in debug builds)
        builder.UseDebugRibbon(ribbonColor: Colors.MediumPurple); // Optional color parameter
#endif

        return builder.Build();
    }
}
```

## Usage

### Basic Usage

Once installed, the debug overlay automatically appears in your app:

1. **Debug Ribbon**: A small "DEBUG" ribbon appears in the bottom-right corner
2. **Open Panel**: Tap the ribbon to open the debug panel
3. **MAUI Version**: When the panel is open, the ribbon shows your current MAUI version
4. **Close Panel**: Tap the ribbon again, tap the X button, or tap outside the panel to close

### Debug Panel Features

The debug panel provides several tools:

#### Visual Tree Dump
- **Button**: "🔍 Dump Visual Tree" 
- **Function**: Dumps the current page's visual tree hierarchy
- **Output**: Debug console and saved to file
- **Focus**: Shows only the currently active page content (not entire Shell structure)

#### Shell Hierarchy Dump  
- **Button**: "🧭 Dump Shell Hierarchy"
- **Function**: Dumps the entire Shell navigation structure
- **Output**: Debug console and saved to file  
- **Use Case**: Navigation debugging and understanding Shell structure

### Output Locations

Debug dumps are written to multiple locations:
- **Debug Console**: Visible in your IDE's debug output window
- **Console Output**: Standard console output  
- **Files**: Saved to `{AppDataDirectory}/debug-dumps/`
  - `visual-tree-dump_latest.txt` - Always the most recent visual tree dump
  - `shell-hierarchy-dump_latest.txt` - Always the most recent Shell dump
  - `visual-tree-dump_{timestamp}.txt` - Timestamped visual tree dumps
  - `shell-hierarchy-dump_{timestamp}.txt` - Timestamped Shell dumps

## Visual Tree Dump Output

The visual tree dump provides detailed information about your UI hierarchy:

```
================================================================================
VISUAL TREE DUMP (from Debug Overlay)
================================================================================
=== Visual Tree Dump ===
Timestamp: 2025-01-15 14:30:15
Current Page: DashboardPage
Main Page: AppShell
Navigation Context: Shell > Item: ShellItem > Title: 'Dashboard'

├─ DashboardPage [MauiReactor]
  Size: 375.0×812.0 | Position: (0.0, 0.0) | H: Fill | V: Fill
  Handler: ContentPageHandler | PlatformView: UIViewController
  ├─ Grid
    Size: 375.0×812.0 | Position: (0.0, 0.0) | Rows: [*,Auto]
    Cols: [*] | H: Fill | V: Fill | Handler: GridHandler
    ├─ ScrollView
      Size: 375.0×600.0 | Position: (0.0, 0.0) | H: FillAndExpand
      Handler: ScrollViewHandler
      ├─ VStack
        Orient: Vertical | Spacing: 15.0 | H: Center | V: Start
        ├─ Label "Welcome Message"
          Size: 200.0×21.0 | Position: (10.0, 5.0) | H: Start
          Handler: LabelHandler
        ├─ Button "Click To Start"
          Size: 120.0×44.0 | Position: (50.0, 30.0) | H: Center
          Handler: ButtonHandler
```

### Information Provided

- **Element Hierarchy**: Tree structure with proper indentation
- **Element Types**: Class names and MauiReactor detection
- **Text Content**: Displayed text, titles, or placeholder text
- **Layout Properties**: Size, position, alignment, margins
- **Platform Information**: Handlers and platform-specific views
- **Navigation Context**: Shell structure and current page location

## Architecture

### Components

- **`DebugOverlay`**: Main overlay manager, handles ribbon and panel coordination
- **`DebugRibbonElement`**: Graphics-based ribbon element with tap detection
- **`DebugOverlayPanel`**: Graphics-based debug panel with interactive buttons
- **`VisualTreeDumpService`**: Core service for analyzing and dumping visual tree hierarchy
- **`MauiProgramExtensions`**: Extension methods for easy integration

### Graphics-Based Rendering

All UI elements are rendered using MAUI's graphics layer (`ICanvas`) for:
- **Cross-platform consistency**: Identical appearance on all platforms
- **Performance**: Leverages hardware acceleration
- **Integration**: Works seamlessly with the WindowOverlay system
- **Flexibility**: Custom shapes, colors, and animations

### Navigation Detection

The plugin automatically detects different MAUI navigation patterns:

- **Shell Navigation**: Finds active page within Shell → ShellItem → ShellSection → ShellContent hierarchy
- **NavigationPage**: Gets current page from navigation stack  
- **TabbedPage**: Identifies currently selected tab
- **FlyoutPage**: Accesses detail page content

## Configuration

### Ribbon Color Customization

```csharp
builder.UseDebugRibbon(ribbonColor: Colors.Red);           // Red ribbon
builder.UseDebugRibbon(ribbonColor: Colors.DeepSkyBlue);   // Blue ribbon
builder.UseDebugRibbon(ribbonColor: Color.FromArgb("#FF6B35")); // Custom color
```

### Dump Options (Programmatic Access)

```csharp
// Custom dump configuration
var options = new VisualTreeDumpService.DumpOptions
{
    IncludeLayoutProperties = true,    // Size, position, margins
    IncludeHandlerInfo = true,         // Platform handlers and views
    IncludeMauiReactorInfo = true,     // MauiReactor component detection  
    IncludeStyleProperties = false,    // Style and resource information
    MaxDepth = 10                      // Limit traversal depth (-1 for unlimited)
};
```

## Platform Support

- ✅ **iOS**: Full support
- ✅ **Android**: Full support  
- ✅ **Windows**: Full support
- ✅ **macOS**: Full support

## Development

### Requirements

- .NET 8.0 or later
- .NET MAUI workload installed

### Building

```bash
dotnet build Plugin.Maui.DebugOverlay.csproj
```

### Testing

The plugin includes comprehensive debug information and error handling. Test by:

1. Adding to a MAUI app project
2. Running in debug mode
3. Tapping the debug ribbon
4. Attempting visual tree dumps
5. Checking debug console output

## Troubleshooting

### Ribbon Not Appearing

- Ensure you're running in DEBUG mode
- Verify `UseDebugRibbon()` is called in `MauiProgram.cs` 
- Check that it's wrapped in `#if DEBUG` preprocessor directive

### Panel Not Opening

- Check debug console for error messages
- Ensure ribbon area is tappable (not covered by other UI)
- Verify touch/tap events are not being intercepted by your app

### Dump Files Not Created

- Check app permissions for file system access
- Verify `AppDataDirectory` is accessible  
- Look for error messages in debug console
- Check the debug-dumps folder: `{AppDataDirectory}/debug-dumps/`

### Empty or Incomplete Dumps

- Ensure the page is fully loaded before dumping
- Check if Shell navigation is properly initialized
- Try both Visual Tree and Shell Hierarchy dumps to compare
- Some custom controls may not expose all properties via reflection

## Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Add tests for new functionality  
4. Ensure all platforms work correctly
5. Submit a pull request

## License

This project is licensed under the MIT License. See LICENSE file for details.

## Credits

Created for enhanced MAUI development and debugging productivity. Special thanks to the MAUI community for feedback and testing.