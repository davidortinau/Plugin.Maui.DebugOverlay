using Microsoft.Maui.Controls;
using Microsoft.Maui.Handlers;
using System.Reflection;
using System.Text;

namespace Plugin.Maui.DebugOverlay;

/// <summary>
/// Service for dumping the visual tree of MAUI applications, including MauiReactor components.
/// Focuses on layout properties and hierarchy visualization for debugging purposes.
/// </summary>
public class VisualTreeDumpService
{
    private readonly StringBuilder _output = new();
    private const int IndentSize = 2;

    /// <summary>
    /// Configuration for what information to include in the dump
    /// </summary>
    public class DumpOptions
    {
        public bool IncludeLoadingTime { get; set; } = true;
        public bool IncludeLayoutProperties { get; set; } = true;
        public bool IncludeStyleProperties { get; set; } = false;
        public bool IncludeAllProperties { get; set; } = false;
        public bool IncludeHandlerInfo { get; set; } = true;
        public bool IncludeMauiReactorInfo { get; set; } = true;
        public int MaxDepth { get; set; } = -1; // -1 for unlimited
    }

    /// <summary>
    /// Dumps the visual tree starting from the current application main page
    /// </summary>
    public string DumpCurrentPage(DumpOptions options = null)
    {
        options ??= new DumpOptions();
        _output.Clear();

        var currentPage = GetCurrentActivePage();
        if (currentPage == null)
        {
            return "No current page found.";
        }

        _output.AppendLine("=== Visual Tree Dump ===");
        _output.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _output.AppendLine($"Current Page: {currentPage.GetType().Name}");

        // Also show the navigation context
        var mainPage = Microsoft.Maui.Controls.Application.Current?.MainPage;
        if (mainPage != currentPage)
        {
            _output.AppendLine($"Main Page: {mainPage?.GetType().Name}");
            _output.AppendLine($"Navigation Context: {GetNavigationContext(currentPage)}");
        }

        _output.AppendLine();

        DumpElement(currentPage, options, 0);

        return _output.ToString();
    }

    /// <summary>
    /// Dumps the visual tree starting from a specific element
    /// </summary>
    public string DumpFromElement(Microsoft.Maui.Controls.Element element, DumpOptions options = null)
    {
        options ??= new DumpOptions();
        _output.Clear();

        if (element == null)
        {
            return "Element is null.";
        }

        _output.AppendLine("=== Visual Tree Dump ===");
        _output.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _output.AppendLine($"Root: {element.GetType().Name}");
        _output.AppendLine();

        DumpElement(element, options, 0);

        return _output.ToString();
    }

    /// <summary>
    /// Dumps the entire Shell navigation hierarchy including all items and sections
    /// </summary>
    public string DumpShellHierarchy(DumpOptions options = null)
    {
        options ??= new DumpOptions();
        _output.Clear();

        var mainPage = Microsoft.Maui.Controls.Application.Current?.MainPage;
        if (mainPage is not Microsoft.Maui.Controls.Shell shell)
        {
            return "Current application is not using Shell navigation.";
        }

        _output.AppendLine("=== Shell Navigation Hierarchy ===");
        _output.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _output.AppendLine($"Shell Type: {shell.GetType().Name}");
        _output.AppendLine($"Current Route: {shell.CurrentState?.Location}");
        _output.AppendLine();

        DumpElement(shell, options, 0);

        return _output.ToString();
    }

    /// <summary>
    /// Recursively dumps an element and its children
    /// </summary>
    private void DumpElement(Microsoft.Maui.Controls.Element element, DumpOptions options, int depth)
    {
        if (element == null) return;
        if (options.MaxDepth >= 0 && depth > options.MaxDepth) return;

        // Element header with type information
        var indent = new string(' ', depth * IndentSize);
        var elementType = element.GetType();
        var elementName = elementType.Name;

        // Check if this might be a MauiReactor component wrapper
        var isMauiReactorWrapper = IsMauiReactorWrapper(elementType);
        var elementId = GetElementId(element);
        var elementText = GetElementText(element);

        _output.Append(indent);
        _output.Append($"├─ {elementName}");

        // Add text content for easier identification
        if (!string.IsNullOrEmpty(elementText))
        {
            _output.Append($" \"{elementText}\"");
        }

        if (!string.IsNullOrEmpty(elementId))
        {
            _output.Append($" (Id: {elementId})");
        }

        if (isMauiReactorWrapper)
        {
            _output.Append(" [MauiReactor]");
        }

        _output.AppendLine();


        // Dump properties based on options
        if (options.IncludeLoadingTime)
        {
            DumpLoadingTime(element, depth + 1);
        }

        // Dump properties based on options
        if (options.IncludeLayoutProperties)
        {
            DumpLayoutProperties(element, depth + 1);
        }

        if (options.IncludeHandlerInfo)
        {
            DumpHandlerInfo(element, depth + 1);
        }

        if (options.IncludeStyleProperties)
        {
            DumpStyleProperties(element, depth + 1);
        }

        if (options.IncludeAllProperties)
        {
            DumpAllProperties(element, depth + 1);
        }

        // Recursively dump children
        DumpChildren(element, options, depth);
    }

    /// <summary>
    /// Dumps loading time of an element
    /// </summary>
    private void DumpLoadingTime(Microsoft.Maui.Controls.Element element, int depth)
    {
        var indent = new string(' ', depth * IndentSize);
        var layoutProps = new List<string>();

        // Add loading time if available
        if (DebugOverlayPanel.ElementsLoadingTime.ContainsKey(element.Id))
        {
            // Formats the loading time for display, adding an optional warning icon and HEX color at the end.
            // - Shows ⚠️ icon for yellow or red thresholds.
            // - Automatically converts milliseconds to seconds if >= 1000 ms.
            // - Appends the HEX color at the end of the string.
            var milliseconds = DebugOverlayPanel.ElementsLoadingTime[element.Id];

            // Choose icon and color based on thresholds
            string icon = string.Empty;
            string hexColor;

            if (milliseconds >= 1250)
            {
                icon = "⚠️ ";
                hexColor = "#FFFF0000"; // red
            }
            else if (milliseconds >= 1000)
            {
                icon = "⚠️ ";
                hexColor = "#FFFFFF00"; // yellow
            }
            else
            {
                hexColor = "#FFAAAAAA"; // grey (no icon)
            }

            // Format the time string
            string timeText = milliseconds >= 1100
                ? $"{milliseconds / 1000:F2} s" // convert to seconds
                : $"{milliseconds:F0} ms";      // keep in milliseconds

            // Return the complete formatted string
            ;
            _output.AppendLine($"{indent}  {icon}Loading time: {timeText} {hexColor}");
        } 
    }

    /// <summary>
    /// Dumps layout-specific properties of an element
    /// </summary>
    private void DumpLayoutProperties(Microsoft.Maui.Controls.Element element, int depth)
    {
        var indent = new string(' ', depth * IndentSize);
        var layoutProps = new List<string>();

        // Common layout properties for VisualElement
        if (element is Microsoft.Maui.Controls.VisualElement visualElement)
        {
            // Core size and position - compact format
            layoutProps.Add($"Size: {visualElement.Width:F1}×{visualElement.Height:F1}");
            layoutProps.Add($"Position: ({visualElement.X:F1}, {visualElement.Y:F1})");

            // Only show non-default values to reduce noise
            if (!visualElement.IsVisible) layoutProps.Add("IsVisible: False");
            if (Math.Abs(visualElement.Opacity - 1.0) > 0.01) layoutProps.Add($"Opacity: {visualElement.Opacity:F2}");
            if (Math.Abs(visualElement.Scale - 1.0) > 0.01) layoutProps.Add($"Scale: {visualElement.Scale:F2}");
            if (Math.Abs(visualElement.Rotation) > 0.01) layoutProps.Add($"Rotation: {visualElement.Rotation:F1}°");

            // Layout options - Note: These properties may not exist in all MAUI versions
            try
            {
                var horizontalOptionsProperty = visualElement.GetType().GetProperty("HorizontalOptions");
                var verticalOptionsProperty = visualElement.GetType().GetProperty("VerticalOptions");

                if (horizontalOptionsProperty != null)
                {
                    var horizontalOptions = horizontalOptionsProperty.GetValue(visualElement);
                    if (horizontalOptions != null)
                    {
                        // Extract the Alignment value from LayoutOptions
                        var alignment = GetLayoutOptionsAlignment(horizontalOptions);
                        if (!string.IsNullOrEmpty(alignment))
                            layoutProps.Add($"H: {alignment}");
                    }
                }

                if (verticalOptionsProperty != null)
                {
                    var verticalOptions = verticalOptionsProperty.GetValue(visualElement);
                    if (verticalOptions != null)
                    {
                        // Extract the Alignment value from LayoutOptions
                        var alignment = GetLayoutOptionsAlignment(verticalOptions);
                        if (!string.IsNullOrEmpty(alignment))
                            layoutProps.Add($"V: {alignment}");
                    }
                }
            }
            catch { /* Ignore if properties don't exist */ }

            // Size requests - only if set
            if (visualElement.WidthRequest >= 0)
                layoutProps.Add($"WReq: {visualElement.WidthRequest:F1}");
            if (visualElement.HeightRequest >= 0)
                layoutProps.Add($"HReq: {visualElement.HeightRequest:F1}");

            // Minimum size - only if non-default
            if (visualElement.MinimumWidthRequest > -1)
                layoutProps.Add($"MinW: {visualElement.MinimumWidthRequest:F1}");
            if (visualElement.MinimumHeightRequest > -1)
                layoutProps.Add($"MinH: {visualElement.MinimumHeightRequest:F1}");
        }

        // Layout-specific properties for different types
        if (element is Microsoft.Maui.Controls.View view)
        {
            if (view.Margin != default)
                layoutProps.Add($"Margin: {view.Margin}");
        }

        // Grid-specific properties - compact format
        if (element is Microsoft.Maui.Controls.Grid grid)
        {
            if (grid.RowDefinitions.Count > 0)
                layoutProps.Add($"Rows: [{string.Join(",", grid.RowDefinitions.Select(r => r.Height.ToString()))}]");
            if (grid.ColumnDefinitions.Count > 0)
                layoutProps.Add($"Cols: [{string.Join(",", grid.ColumnDefinitions.Select(c => c.Width.ToString()))}]");
            if (grid.RowSpacing > 0) layoutProps.Add($"RowSp: {grid.RowSpacing:F1}");
            if (grid.ColumnSpacing > 0) layoutProps.Add($"ColSp: {grid.ColumnSpacing:F1}");
        }

        // StackLayout-specific properties
        if (element is Microsoft.Maui.Controls.StackLayout stackLayout)
        {
            layoutProps.Add($"Orient: {stackLayout.Orientation}");
            if (stackLayout.Spacing > 0) layoutProps.Add($"Spacing: {stackLayout.Spacing:F1}");
        }

        // FlexLayout-specific properties
        if (element is Microsoft.Maui.Controls.FlexLayout flexLayout)
        {
            layoutProps.Add($"Dir: {flexLayout.Direction}");
            layoutProps.Add($"Wrap: {flexLayout.Wrap}");
            layoutProps.Add($"Justify: {flexLayout.JustifyContent}");
            layoutProps.Add($"AlignC: {flexLayout.AlignContent}");
            layoutProps.Add($"AlignI: {flexLayout.AlignItems}");
        }

        // Output properties in a more compact format - multiple per line
        if (layoutProps.Any())
        {
            const int propsPerLine = 3;
            for (int i = 0; i < layoutProps.Count; i += propsPerLine)
            {
                var lineProps = layoutProps.Skip(i).Take(propsPerLine);
                _output.AppendLine($"{indent}  {string.Join(" | ", lineProps)}");
            }
        }
    }

    /// <summary>
    /// Dumps handler information for the element
    /// </summary>
    private void DumpHandlerInfo(Microsoft.Maui.Controls.Element element, int depth)
    {
        var indent = new string(' ', depth * IndentSize);

        if (element is Microsoft.Maui.Controls.VisualElement visualElement && visualElement.Handler != null)
        {
            var handler = visualElement.Handler;
            var handlerType = handler.GetType().Name;
            var platformViewType = handler.PlatformView?.GetType().Name ?? "null";

            _output.AppendLine($"{indent}  Handler: {handlerType}");
            _output.AppendLine($"{indent}  PlatformView: {platformViewType}");

            // Try to get platform-specific bounds if available
            if (handler.PlatformView != null)
            {
                try
                {
                    var platformBounds = GetPlatformViewBounds(handler.PlatformView);
                    if (!string.IsNullOrEmpty(platformBounds))
                    {
                        _output.AppendLine($"{indent}  PlatformBounds: {platformBounds}");
                    }
                }
                catch
                {
                    // Ignore platform-specific errors
                }
            }
        }
    }

    /// <summary>
    /// Dumps style-related properties (placeholder for future implementation)
    /// </summary>
    private void DumpStyleProperties(Microsoft.Maui.Controls.Element element, int depth)
    {
        var indent = new string(' ', depth * IndentSize);

        if (element is Microsoft.Maui.Controls.VisualElement visualElement)
        {
            _output.AppendLine($"{indent}  Style: {visualElement.Style?.GetType().Name ?? "None"}");
            _output.AppendLine($"{indent}  Resources: {visualElement.Resources?.Count ?? 0} items");
        }
    }

    /// <summary>
    /// Dumps all properties (placeholder for future implementation)
    /// </summary>
    private void DumpAllProperties(Microsoft.Maui.Controls.Element element, int depth)
    {
        // This would be a comprehensive property dump - implementation can be added later
        var indent = new string(' ', depth * IndentSize);
        _output.AppendLine($"{indent}  [All Properties - Not implemented yet]");
    }

    /// <summary>
    /// Dumps child elements
    /// </summary>
    private void DumpChildren(Microsoft.Maui.Controls.Element element, DumpOptions options, int depth)
    {
        // Get children using different methods based on element type
        var children = GetChildElements(element);

        foreach (var child in children)
        {
            DumpElement(child, options, depth + 1);
        }
    }

    /// <summary>
    /// Gets child elements from various container types
    /// </summary>
    private IEnumerable<Microsoft.Maui.Controls.Element> GetChildElements(Microsoft.Maui.Controls.Element element)
    {
        var children = new List<Microsoft.Maui.Controls.Element>();

        // Direct children access
        if (element is Microsoft.Maui.Controls.Layout layout)
        {
            children.AddRange(layout.Children.Cast<Microsoft.Maui.Controls.Element>());
        }
        else if (element is Microsoft.Maui.Controls.ContentView contentView && contentView.Content != null)
        {
            children.Add(contentView.Content);
        }
        else if (element is Microsoft.Maui.Controls.ContentPage contentPage && contentPage.Content != null)
        {
            children.Add(contentPage.Content);
        }
        else if (element is Microsoft.Maui.Controls.Shell shell)
        {
            children.AddRange(shell.Items.Cast<Microsoft.Maui.Controls.Element>());
        }
        else if (element is Microsoft.Maui.Controls.ScrollView scrollView && scrollView.Content != null)
        {
            children.Add(scrollView.Content);
        }
        else if (element is Microsoft.Maui.Controls.Frame frame && frame.Content != null)
        {
            children.Add(frame.Content);
        }
        else if (element is Microsoft.Maui.Controls.Border border && border.Content != null)
        {
            children.Add(border.Content);
        }

        // Try to use reflection to find other child collections
        try
        {
            var childrenProperty = element.GetType().GetProperty("Children");
            if (childrenProperty != null && childrenProperty.PropertyType.IsGenericType)
            {
                var childCollection = childrenProperty.GetValue(element);
                if (childCollection is System.Collections.IEnumerable enumerable)
                {
                    foreach (var child in enumerable)
                    {
                        if (child is Microsoft.Maui.Controls.Element elementChild && !children.Contains(elementChild))
                        {
                            children.Add(elementChild);
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore reflection errors
        }

        // Try VisualTreeElementExtensions if available
        try
        {
            if (element is Microsoft.Maui.Controls.VisualElement visualElement)
            {
                // Note: GetVisualTreeElements method signature may vary in different MAUI versions
                // var visualChildren = VisualTreeElementExtensions.GetVisualTreeElements(visualElement);
                // This method may require different parameters or may not be available
                // Commenting out for now to avoid compilation errors
                /*
                foreach (var child in visualChildren.OfType<Microsoft.Maui.Controls.Element>())
                {
                    if (!children.Contains(child))
                    {
                        children.Add(child);
                    }
                }
                */
            }
        }
        catch
        {
            // Ignore if method doesn't work as expected
        }

        return children.Distinct();
    }

    /// <summary>
    /// Gets a string identifier for an element (ClassId, StyleId, or AutomationId)
    /// </summary>
    private string GetElementId(Microsoft.Maui.Controls.Element element)
    {
        if (!string.IsNullOrEmpty(element.ClassId))
            return element.ClassId;

        if (!string.IsNullOrEmpty(element.StyleId))
            return element.StyleId;

        if (element is Microsoft.Maui.Controls.VisualElement visualElement && !string.IsNullOrEmpty(visualElement.AutomationId))
            return visualElement.AutomationId;

        return string.Empty;
    }

    /// <summary>
    /// Attempts to determine if this element is part of a MauiReactor component
    /// </summary>
    private bool IsMauiReactorWrapper(Type elementType)
    {
        // Check if the type name or namespace suggests MauiReactor
        var typeName = elementType.FullName ?? elementType.Name;

        return typeName.Contains("MauiReactor") ||
               typeName.Contains("Reactor") ||
               elementType.Assembly.GetName().Name?.Contains("MauiReactor") == true ||
               elementType.Assembly.GetName().Name?.Contains("Reactor") == true;
    }

    /// <summary>
    /// Attempts to get platform-specific bounds information
    /// </summary>
    private string GetPlatformViewBounds(object platformView)
    {
        try
        {
            // Try different platform approaches
            var bounds = string.Empty;

            // iOS/macOS
            var frameProperty = platformView.GetType().GetProperty("Frame");
            if (frameProperty != null)
            {
                var frame = frameProperty.GetValue(platformView);
                bounds = frame?.ToString() ?? "";
            }

            // Android  
            var boundsProperty = platformView.GetType().GetProperty("Bounds");
            if (boundsProperty != null && string.IsNullOrEmpty(bounds))
            {
                var boundsValue = boundsProperty.GetValue(platformView);
                bounds = boundsValue?.ToString() ?? "";
            }

            // Windows
            var actualWidthProperty = platformView.GetType().GetProperty("ActualWidth");
            var actualHeightProperty = platformView.GetType().GetProperty("ActualHeight");
            if (actualWidthProperty != null && actualHeightProperty != null && string.IsNullOrEmpty(bounds))
            {
                var width = actualWidthProperty.GetValue(platformView);
                var height = actualHeightProperty.GetValue(platformView);
                bounds = $"Width: {width}, Height: {height}";
            }

            return bounds;
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Gets the currently active page by traversing the navigation hierarchy
    /// </summary>
    private Microsoft.Maui.Controls.Element GetCurrentActivePage()
    {
        var mainPage = Microsoft.Maui.Controls.Application.Current?.MainPage;
        if (mainPage == null)
        {
            return null;
        }

        // If the main page is a Shell, get the current page from it
        if (mainPage is Microsoft.Maui.Controls.Shell shell)
        {
            // Try to get the current page from Shell
            var currentPage = shell.CurrentPage;
            if (currentPage != null)
            {
                return currentPage;
            }

            // Fallback: try to navigate the Shell hierarchy
            var currentItem = shell.CurrentItem;
            if (currentItem != null)
            {
                // Shell hierarchy: Shell -> ShellItem -> ShellSection -> ShellContent -> Page
                // currentItem is a ShellItem, so we need to get its current section, then current content
                var currentSection = currentItem.CurrentItem; // This gets the ShellSection
                if (currentSection != null)
                {
                    var currentContent = currentSection.CurrentItem; // This gets the ShellContent
                    if (currentContent?.Content is Microsoft.Maui.Controls.Page contentPage)
                    {
                        return contentPage;
                    }
                }
            }
        }

        // If the main page is a NavigationPage, get the current page
        if (mainPage is Microsoft.Maui.Controls.NavigationPage navigationPage)
        {
            return navigationPage.CurrentPage;
        }

        // If the main page is a TabbedPage, get the current page
        if (mainPage is Microsoft.Maui.Controls.TabbedPage tabbedPage)
        {
            return tabbedPage.CurrentPage;
        }

        // If the main page is a FlyoutPage, get the detail page
        if (mainPage is Microsoft.Maui.Controls.FlyoutPage flyoutPage)
        {
            return flyoutPage.Detail;
        }

        // Default: return the main page itself
        return mainPage;
    }

    /// <summary>
    /// Gets a string describing the navigation context for a page
    /// </summary>
    private string GetNavigationContext(Microsoft.Maui.Controls.Element page)
    {
        var context = new List<string>();
        var mainPage = Microsoft.Maui.Controls.Application.Current?.MainPage;

        if (mainPage is Microsoft.Maui.Controls.Shell shell)
        {
            context.Add("Shell");

            if (shell.CurrentItem != null)
            {
                context.Add($"Item: {shell.CurrentItem.GetType().Name}");

                if (shell.CurrentItem.Title != null)
                {
                    context.Add($"Title: '{shell.CurrentItem.Title}'");
                }

                // Add route information if available
                try
                {
                    // Try to get route information - the API may vary between MAUI versions
                    var routeProperty = typeof(Microsoft.Maui.Controls.Shell).GetMethod("GetRoute", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (routeProperty != null)
                    {
                        var route = routeProperty.Invoke(null, new[] { page }) as string;
                        if (!string.IsNullOrEmpty(route))
                        {
                            context.Add($"Route: '{route}'");
                        }
                    }
                }
                catch
                {
                    // Ignore if route method is not available
                }
            }
        }
        else if (mainPage is Microsoft.Maui.Controls.NavigationPage navigationPage)
        {
            context.Add("NavigationPage");
            context.Add($"Stack Depth: {navigationPage.Navigation.NavigationStack.Count}");
        }
        else if (mainPage is Microsoft.Maui.Controls.TabbedPage tabbedPage)
        {
            context.Add("TabbedPage");
            var selectedIndex = tabbedPage.Children.IndexOf(page as Microsoft.Maui.Controls.Page);
            if (selectedIndex >= 0)
            {
                context.Add($"Selected Tab: {selectedIndex}");
            }
        }
        else if (mainPage is Microsoft.Maui.Controls.FlyoutPage)
        {
            context.Add("FlyoutPage Detail");
        }

        return context.Count > 0 ? string.Join(" > ", context) : "Direct";
    }

    /// <summary>
    /// Extracts the alignment value from LayoutOptions object
    /// </summary>
    private string GetLayoutOptionsAlignment(object layoutOptions)
    {
        if (layoutOptions == null) return string.Empty;

        try
        {
            // First, try to get the Alignment and Expands properties
            var alignmentProperty = layoutOptions.GetType().GetProperty("Alignment");
            var expandsProperty = layoutOptions.GetType().GetProperty("Expands");

            if (alignmentProperty != null)
            {
                var alignment = alignmentProperty.GetValue(layoutOptions);
                var expands = expandsProperty?.GetValue(layoutOptions);

                if (alignment != null)
                {
                    var alignmentStr = alignment.ToString();

                    // Add "AndExpand" if the Expands property is true
                    if (expands is bool expandsBool && expandsBool)
                    {
                        alignmentStr += "AndExpand";
                    }

                    return alignmentStr;
                }
            }

            // Fallback: Check against common LayoutOptions static values by comparing the object
            var layoutOptionsType = layoutOptions.GetType();
            var layoutOptionsFullName = layoutOptionsType.FullName;

            // If it's a Microsoft.Maui.Controls.LayoutOptions, try to match against known values
            if (layoutOptionsFullName?.Contains("LayoutOptions") == true)
            {
                // Try to get the declaring type to access static properties
                var declaringType = layoutOptionsType.DeclaringType ?? layoutOptionsType;

                // Check common static properties
                var commonOptions = new[]
                {
                    ("Fill", "Fill"),
                    ("FillAndExpand", "FillAndExpand"),
                    ("Start", "Start"),
                    ("StartAndExpand", "StartAndExpand"),
                    ("Center", "Center"),
                    ("CenterAndExpand", "CenterAndExpand"),
                    ("End", "End"),
                    ("EndAndExpand", "EndAndExpand")
                };

                foreach (var (propertyName, displayName) in commonOptions)
                {
                    try
                    {
                        var staticProperty = declaringType.GetProperty(propertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                        if (staticProperty != null)
                        {
                            var staticValue = staticProperty.GetValue(null);
                            if (staticValue != null && staticValue.Equals(layoutOptions))
                            {
                                return displayName;
                            }
                        }
                    }
                    catch { /* Continue checking other options */ }
                }
            }

            // Last resort: return ToString() result
            var str = layoutOptions.ToString();
            return str?.Contains("LayoutOptions") == true ?
                   str.Replace("Microsoft.Maui.Controls.LayoutOptions", "").Trim() :
                   str ?? string.Empty;
        }
        catch
        {
            // If all else fails, return a safe representation
            return layoutOptions.ToString() ?? string.Empty;
        }
    }

    /// <summary>
    /// Gets text content from various control types for easier identification
    /// </summary>
    private string GetElementText(Microsoft.Maui.Controls.Element element)
    {
        if (element == null) return string.Empty;

        try
        {
            // Label - most common case
            if (element is Microsoft.Maui.Controls.Label label && !string.IsNullOrEmpty(label.Text))
            {
                return TruncateText(label.Text);
            }

            // Button
            if (element is Microsoft.Maui.Controls.Button button && !string.IsNullOrEmpty(button.Text))
            {
                return TruncateText(button.Text);
            }

            // Entry
            if (element is Microsoft.Maui.Controls.Entry entry)
            {
                var text = !string.IsNullOrEmpty(entry.Text) ? entry.Text : entry.Placeholder;
                if (!string.IsNullOrEmpty(text))
                    return TruncateText(text);
            }

            // Editor
            if (element is Microsoft.Maui.Controls.Editor editor)
            {
                var text = !string.IsNullOrEmpty(editor.Text) ? editor.Text : editor.Placeholder;
                if (!string.IsNullOrEmpty(text))
                    return TruncateText(text);
            }

            // SearchBar
            if (element is Microsoft.Maui.Controls.SearchBar searchBar)
            {
                var text = !string.IsNullOrEmpty(searchBar.Text) ? searchBar.Text : searchBar.Placeholder;
                if (!string.IsNullOrEmpty(text))
                    return TruncateText(text);
            }

            // ContentPage - get title
            if (element is Microsoft.Maui.Controls.ContentPage contentPage && !string.IsNullOrEmpty(contentPage.Title))
            {
                return TruncateText(contentPage.Title);
            }

            // TabbedPage - get title
            if (element is Microsoft.Maui.Controls.TabbedPage tabbedPage && !string.IsNullOrEmpty(tabbedPage.Title))
            {
                return TruncateText(tabbedPage.Title);
            }

            // Shell items - get title
            if (element.GetType().Name.Contains("Shell") && element.GetType().GetProperty("Title") != null)
            {
                var title = element.GetType().GetProperty("Title")?.GetValue(element) as string;
                if (!string.IsNullOrEmpty(title))
                    return TruncateText(title);
            }

            // Generic Text property check for custom controls
            var textProperty = element.GetType().GetProperty("Text");
            if (textProperty?.PropertyType == typeof(string))
            {
                var text = textProperty.GetValue(element) as string;
                if (!string.IsNullOrEmpty(text))
                    return TruncateText(text);
            }

            // Generic Title property check
            var titleProperty = element.GetType().GetProperty("Title");
            if (titleProperty?.PropertyType == typeof(string))
            {
                var title = titleProperty.GetValue(element) as string;
                if (!string.IsNullOrEmpty(title))
                    return TruncateText(title);
            }

            // Placeholder property check for input controls
            var placeholderProperty = element.GetType().GetProperty("Placeholder");
            if (placeholderProperty?.PropertyType == typeof(string))
            {
                var placeholder = placeholderProperty.GetValue(element) as string;
                if (!string.IsNullOrEmpty(placeholder))
                    return $"[{TruncateText(placeholder)}]"; // Brackets to indicate it's placeholder text
            }
        }
        catch
        {
            // Ignore errors in text extraction
        }

        return string.Empty;
    }

    /// <summary>
    /// Truncates text to a reasonable length for display in the tree
    /// </summary>
    private string TruncateText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        const int maxLength = 50;

        // Remove newlines and extra whitespace
        text = text.Replace('\n', ' ').Replace('\r', ' ');
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();

        if (text.Length > maxLength)
        {
            return text.Substring(0, maxLength - 3) + "...";
        }

        return text;
    }
}