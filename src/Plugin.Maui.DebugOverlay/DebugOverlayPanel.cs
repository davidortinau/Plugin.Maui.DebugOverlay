using System.Diagnostics;
using System.Reflection;
using Microsoft.Maui.Graphics;

namespace Plugin.Maui.DebugOverlay;

public class TreeNode
{
    public string Name { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public List<TreeNode> Children { get; set; } = new();
    public bool IsExpanded { get; set; } = false;
    public int Depth { get; set; } = 0;
    public string FullText { get; set; } = string.Empty; // Original text content
}

/// <summary>
/// Represents a debug panel that displays over the app with MAUI version info and debugging tools.
/// Uses graphics rendering for cross-platform compatibility with the overlay system.
/// </summary>
public class DebugOverlayPanel : IWindowOverlayElement
{
    private readonly WindowOverlay _overlay;
    private readonly VisualTreeDumpService _dumpService;
    private readonly Color _panelBackgroundColor;
    private readonly Color _buttonBackgroundColor;
    private readonly Color _textColor;
    
    // Panel state management
    private enum PanelState { MainMenu, TreeView }
    private PanelState _currentState = PanelState.MainMenu;
    private List<TreeNode>? _currentTreeData = null;
    
    private RectF _panelRect;
    private RectF _closeButtonRect;
    private RectF _visualTreeButtonRect;
    private RectF _shellHierarchyButtonRect;
    private RectF _headerRect;
    private RectF _backButtonRect;
    
    // Tree view state
    private float _scrollOffset = 0f;
    private const float LineHeight = 24f;
    private List<RectF> _treeNodeRects = new();
    
    // Tree view scroll offset
    // (Removed unused panning fields since we simplified touch handling)
    
    private string _mauiVersion;
    private bool _isVisible;
    private DateTime _lastButtonTapTime = DateTime.MinValue;
    private const int ButtonTapDebounceMs = 500; // 500ms debounce for buttons
    private const float ContentPadding = 15; // Padding for content inside panel
    private const float ButtonHeight = 36;
    private const float ButtonSpacing = 8;
    private const float Padding = 12;

    public bool IsVisible 
    { 
        get => _isVisible; 
        set => _isVisible = value; 
    }

    public DebugOverlayPanel(WindowOverlay overlay, Color panelBackgroundColor = null)
    {
        _overlay = overlay;
        _dumpService = new VisualTreeDumpService();
        _panelBackgroundColor = panelBackgroundColor ?? Color.FromArgb("#E0000000"); // Semi-transparent black
        _buttonBackgroundColor = Color.FromArgb("#FF4A4A4A"); // Dark gray buttons
        _textColor = Colors.White;
        _isVisible = false;
        
        // Get MAUI version
        var version = typeof(MauiApp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        _mauiVersion = version != null && version.Contains('+') ? version[..version.IndexOf('+')] : version ?? "Unknown";
    }

    public bool Contains(Point point)
    {
        // When panel is visible, consume ALL touches to prevent pass-through
        return _isVisible;
    }

    private (float top, float bottom, float left, float right) GetSafeAreaInsets(RectF windowRect)
    {
        // Default safe area insets
        float top = 50f;    // Status bar + notch area
        float bottom = 34f; // Home indicator area  
        float left = 20f;   // Side margins
        float right = 20f;  // Side margins

#if IOS
        try
        {
            // Get actual safe area from iOS
            if (UIKit.UIApplication.SharedApplication?.KeyWindow?.SafeAreaInsets is { } insets)
            {
                top = (float)insets.Top;
                bottom = (float)insets.Bottom;
                left = (float)insets.Left;
                right = (float)insets.Right;
            }
        }
        catch
        {
            // Fall back to defaults
        }
#elif ANDROID
        try
        {
            // Basic Android status bar height detection
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            var resourceId = context.Resources?.GetIdentifier("status_bar_height", "dimen", "android");
            if (resourceId.HasValue && resourceId > 0 && context.Resources != null)
            {
                top = context.Resources.GetDimensionPixelSize(resourceId.Value) / context.Resources.DisplayMetrics.Density;
            }
        }
        catch
        {
            // Fall back to defaults
        }
#endif

        return (top, bottom, left, right);
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        if (!_isVisible) return;

        try
        {
            // Get safe area insets
            var (safeTop, safeBottom, safeLeft, safeRight) = GetSafeAreaInsets(dirtyRect);
            
            // Panel background: edge-to-edge (full window)
            _panelRect = new RectF(0, 0, dirtyRect.Width, dirtyRect.Height);

            // Content area: within safe area + content padding
            var contentRect = new RectF(
                safeLeft + ContentPadding,
                safeTop + ContentPadding, 
                dirtyRect.Width - safeLeft - safeRight - (ContentPadding * 2),
                dirtyRect.Height - safeTop - safeBottom - (ContentPadding * 2));

            // Draw edge-to-edge panel background
            DrawPanelBackground(canvas, _panelRect);

            if (_currentState == PanelState.MainMenu)
            {
                // Draw main menu UI within content area
                DrawHeader(canvas, contentRect);
                DrawButtons(canvas, contentRect);
                DrawCloseButton(canvas, contentRect);
            }
            else if (_currentState == PanelState.TreeView)
            {
                // Draw tree view UI within content area
                DrawTreeViewHeader(canvas, contentRect);
                DrawTreeView(canvas, contentRect);
                DrawBackButton(canvas, contentRect);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error drawing debug overlay panel: {ex.Message}");
        }
    }

    private void DrawPanelBackground(ICanvas canvas, RectF rect)
    {
        // Save canvas state
        canvas.SaveState();

        // Draw shadow
        var shadowRect = new RectF(rect.X + 2, rect.Y + 2, rect.Width, rect.Height);
        canvas.FillColor = Color.FromArgb("#40000000");
        canvas.FillRoundedRectangle(shadowRect, 8);

        // Draw main panel background
        canvas.FillColor = _panelBackgroundColor;
        canvas.FillRoundedRectangle(rect, 8);

        // Draw border
        canvas.StrokeColor = Color.FromArgb("#FF666666");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(rect, 8);

        canvas.RestoreState();
    }

    private void DrawHeader(ICanvas canvas, RectF contentRect)
    {
        _headerRect = new RectF(contentRect.X, contentRect.Y, contentRect.Width, 40);

        canvas.SaveState();
        
        // Header background
        canvas.FillColor = Color.FromArgb("#FF2D2D30");
        canvas.FillRoundedRectangle(_headerRect, 4);

        // MAUI version text
        canvas.FontColor = _textColor;
        canvas.FontSize = 14;
        canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 600, FontStyleType.Normal);
        
        var headerText = $".NET MAUI {_mauiVersion}";
        canvas.DrawString(headerText, _headerRect, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.RestoreState();
    }

    private void DrawButtons(ICanvas canvas, RectF contentRect)
    {
        var buttonY = _headerRect.Bottom + ButtonSpacing;
        var buttonWidth = contentRect.Width;

        // Visual Tree Dump Button
        _visualTreeButtonRect = new RectF(contentRect.X, buttonY, buttonWidth, ButtonHeight);
        DrawButton(canvas, _visualTreeButtonRect, "🔍 Dump Visual Tree", _buttonBackgroundColor);

        // Shell Hierarchy Button
        buttonY += ButtonHeight + ButtonSpacing;
        _shellHierarchyButtonRect = new RectF(contentRect.X, buttonY, buttonWidth, ButtonHeight);
        DrawButton(canvas, _shellHierarchyButtonRect, "🧭 Dump Shell Hierarchy", _buttonBackgroundColor);
    }

    private void DrawButton(ICanvas canvas, RectF rect, string text, Color backgroundColor)
    {
        canvas.SaveState();

        // Button background
        canvas.FillColor = backgroundColor;
        canvas.FillRoundedRectangle(rect, 6);

        // Button border
        canvas.StrokeColor = Color.FromArgb("#FF666666");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(rect, 6);

        // Button text
        canvas.FontColor = _textColor;
        canvas.FontSize = 12;
        canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 400, FontStyleType.Normal);
        canvas.DrawString(text, rect, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.RestoreState();
    }

    private void DrawCloseButton(ICanvas canvas, RectF contentRect)
    {
        var closeButtonSize = 32f;
        _closeButtonRect = new RectF(
            contentRect.Right - closeButtonSize, 
            contentRect.Y, 
            closeButtonSize, 
            closeButtonSize);

        canvas.SaveState();

        // Close button background
        canvas.FillColor = Color.FromArgb("#FFFF4444");
        canvas.FillRoundedRectangle(_closeButtonRect, 6);

        // Close button border
        canvas.StrokeColor = Color.FromArgb("#FFAA2222");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(_closeButtonRect, 6);

        // Draw X
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2;
        var margin = 8f;
        canvas.DrawLine(
            _closeButtonRect.X + margin, 
            _closeButtonRect.Y + margin,
            _closeButtonRect.Right - margin, 
            _closeButtonRect.Bottom - margin);
        canvas.DrawLine(
            _closeButtonRect.Right - margin, 
            _closeButtonRect.Y + margin,
            _closeButtonRect.X + margin, 
            _closeButtonRect.Bottom - margin);

        canvas.RestoreState();
    }

    private void DrawTreeViewHeader(ICanvas canvas, RectF contentRect)
    {
        _headerRect = new RectF(contentRect.X, contentRect.Y, contentRect.Width, 50);

        canvas.SaveState();
        
        // Header background
        canvas.FillColor = Color.FromArgb("#FF2D2D30");
        canvas.FillRoundedRectangle(_headerRect, 4);

        // Title text
        canvas.FontColor = _textColor;
        canvas.FontSize = 16;
        canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 600, FontStyleType.Normal);
        
        var headerText = "Visual Tree Explorer";
        canvas.DrawString(headerText, _headerRect, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.RestoreState();
    }

    private void DrawBackButton(ICanvas canvas, RectF contentRect)
    {
        var backButtonSize = 40f;
        _backButtonRect = new RectF(
            contentRect.X, 
            contentRect.Y + 5, 
            backButtonSize, 
            backButtonSize);

        canvas.SaveState();

        // Back button background
        canvas.FillColor = Color.FromArgb("#FF4A4A4A");
        canvas.FillRoundedRectangle(_backButtonRect, 8);

        // Back button border
        canvas.StrokeColor = Color.FromArgb("#FF666666");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(_backButtonRect, 8);

        // Draw back arrow
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 3;
        var centerX = _backButtonRect.Center.X;
        var centerY = _backButtonRect.Center.Y;
        var size = 10f;
        
        // Arrow shape: <
        canvas.DrawLine(centerX + size/2, centerY - size, centerX - size/2, centerY);
        canvas.DrawLine(centerX - size/2, centerY, centerX + size/2, centerY + size);

        canvas.RestoreState();
    }

    private void DrawTreeView(ICanvas canvas, RectF contentRect)
    {
        if (_currentTreeData == null || _currentTreeData.Count == 0)
            return;

        canvas.SaveState();
        
        // Define scrollable content area (below header)
        var treeContentRect = new RectF(
            contentRect.X, 
            _headerRect.Bottom + Padding,
            contentRect.Width,
            contentRect.Height - _headerRect.Height - Padding);

        // Clear previous node rects
        _treeNodeRects.Clear();

        // Set up clipping for scrollable area
        canvas.ClipRectangle(treeContentRect);

        var currentY = treeContentRect.Y - _scrollOffset;
        
        foreach (var rootNode in _currentTreeData)
        {
            currentY = DrawTreeNode(canvas, rootNode, treeContentRect.X, currentY, treeContentRect.Width);
        }

        canvas.RestoreState();
    }

    private float DrawTreeNode(ICanvas canvas, TreeNode node, float x, float y, float width)
    {
        var nodeHeight = LineHeight;
        var indentSize = 30f; // Increased indent for better hierarchy
        var nodeX = x + (node.Depth * indentSize);
        
        // Determine if this is a property line early for use in rendering logic
        var name = node.Name;
        var details = node.Details;
        bool isPropertyLine = name.Equals("Size", StringComparison.OrdinalIgnoreCase) ||
                             name.Equals("Handler", StringComparison.OrdinalIgnoreCase) ||
                             name.Equals("PlatformView", StringComparison.OrdinalIgnoreCase) ||
                             name.Equals("PlatformBounds", StringComparison.OrdinalIgnoreCase) ||
                             name.Equals("Position", StringComparison.OrdinalIgnoreCase) ||
                             name.StartsWith("V:", StringComparison.OrdinalIgnoreCase) ||
                             name.StartsWith("H:", StringComparison.OrdinalIgnoreCase) ||
                             details.Contains("|"); // Multi-property lines like "400×300 | Position: (0,0) | H: Fill"
        
        // Create larger hit rect for better touch handling
        var expanderSize = 20f;
        var expanderRect = new RectF(nodeX - expanderSize, y + (nodeHeight - expanderSize) / 2, expanderSize, expanderSize);
        var nodeRect = new RectF(nodeX, y, width - nodeX, nodeHeight);
        
        // Store rect for hit testing (combine expander + text for easier tapping)
        var hitRect = new RectF(nodeX - expanderSize, y, width - nodeX + expanderSize, nodeHeight);
        _treeNodeRects.Add(hitRect);

        canvas.SaveState();

        // Draw connecting lines for better tree structure
        if (node.Depth > 0)
        {
            canvas.StrokeColor = Color.FromArgb("#FF666666");
            canvas.StrokeSize = 1;
            
            // Vertical line from parent
            var lineX = x + ((node.Depth - 1) * indentSize) + indentSize / 2;
            canvas.DrawLine(lineX, y, lineX, y + nodeHeight / 2);
            
            // Horizontal line to current node
            canvas.DrawLine(lineX, y + nodeHeight / 2, nodeX - 5, y + nodeHeight / 2);
        }

        // Draw expand/collapse indicator with better visibility
        if (node.Children.Count > 0)
        {
            // Background circle with better contrast
            canvas.FillColor = node.IsExpanded ? Color.FromArgb("#FF2E5F8A") : Color.FromArgb("#FF3F6F3F");  // Blue for expanded, green for collapsed
            canvas.FillEllipse(expanderRect);
            
            // Border for better definition
            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = 2;
            canvas.DrawEllipse(expanderRect);
            
            // Draw expand/collapse symbol with better visibility
            canvas.FontColor = Colors.White;
            canvas.FontSize = 16;
            canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 800, FontStyleType.Normal);
            
            var symbol = node.IsExpanded ? "−" : "+";
            canvas.DrawString(symbol, expanderRect, HorizontalAlignment.Center, VerticalAlignment.Center);
        }
        else
        {
            // For leaf nodes that are properties, use a smaller, different indicator
            if (isPropertyLine)
            {
                // Small property indicator
                var dotSize = 4f;
                var dotRect = new RectF(expanderRect.Center.X - dotSize/2, expanderRect.Center.Y - dotSize/2, dotSize, dotSize);
                canvas.FillColor = Color.FromArgb("#FF999999");
                canvas.FillEllipse(dotRect);
            }
            else
            {
                // Regular leaf node (element with no children)
                var dotSize = 8f;
                var dotRect = new RectF(expanderRect.Center.X - dotSize/2, expanderRect.Center.Y - dotSize/2, dotSize, dotSize);
                canvas.FillColor = Color.FromArgb("#FF666666");
                canvas.FillEllipse(dotRect);
                canvas.StrokeColor = Color.FromArgb("#FFAAAAAA");
                canvas.StrokeSize = 1;
                canvas.DrawEllipse(dotRect);
            }
        }

        // Draw node text with better formatting (variables already declared at top of method)
        if (isPropertyLine)
        {
            // Property lines: smaller, dimmed, indented text
            canvas.FontColor = Color.FromArgb("#FFAAAAAA");
            canvas.FontSize = 10;
            canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 400, FontStyleType.Normal);
            
            var displayText = string.IsNullOrEmpty(details) ? name : $"{name}: {details}";
            if (displayText.Length > 80)
                displayText = displayText[..77] + "...";
                
            // Add extra indentation for property lines to visually distinguish them
            var propertyIndent = nodeX + 25; 
            canvas.DrawString(displayText, new RectF(propertyIndent, y, width - propertyIndent - 4, nodeHeight), HorizontalAlignment.Left, VerticalAlignment.Center);
        }
        else
        {
            // Element lines: prominent display
            if (!string.IsNullOrEmpty(name))
            {
                // Draw element name in bold white
                canvas.FontColor = Colors.White;
                canvas.FontSize = 13;
                canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 700, FontStyleType.Normal);
                
                // Truncate very long names
                var displayName = name.Length > 40 ? name[..37] + "..." : name;
                canvas.DrawString(displayName, new RectF(nodeX + 4, y, width - nodeX - 4, nodeHeight), HorizontalAlignment.Left, VerticalAlignment.Center);
            }
            
            if (!string.IsNullOrEmpty(details))
            {
                // Calculate text width of name to position details
                var nameWidth = !string.IsNullOrEmpty(name) ? Math.Min(name.Length * 8, 300) : 0; // Cap width for layout
                
                // Draw details in green for MauiReactor, yellow for text content, gray for IDs
                Color detailColor;
                if (details.Contains("[MauiReactor]"))
                    detailColor = Color.FromArgb("#FF66CC66"); // Green for MauiReactor
                else if (details.StartsWith("\"") && details.EndsWith("\""))
                    detailColor = Color.FromArgb("#FFFFFF99"); // Yellow for text content
                else if (details.Contains("Id:"))
                    detailColor = Color.FromArgb("#FFCCCC99"); // Light yellow for IDs
                else
                    detailColor = Color.FromArgb("#FFCCCCCC"); // Default gray
                
                canvas.FontColor = detailColor;
                canvas.FontSize = 11;
                canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 400, FontStyleType.Normal);
                
                var detailsText = details.Length > 50 ? details[..47] + "..." : details;
                canvas.DrawString(detailsText, new RectF(nodeX + nameWidth + 12, y, width - nodeX - nameWidth - 12, nodeHeight), HorizontalAlignment.Left, VerticalAlignment.Center);
            }
        }

        canvas.RestoreState();

        var nextY = y + nodeHeight;

        // Draw children if expanded
        if (node.IsExpanded && node.Children.Count > 0)
        {
            foreach (var child in node.Children)
            {
                nextY = DrawTreeNode(canvas, child, x, nextY, width);
            }
        }

        return nextY;
    }

    /// <summary>
    /// Handles tap events on the panel. Returns true if the tap was handled.
    /// </summary>
    public bool HandleTap(Point point)
    {
        if (!_isVisible || !_panelRect.Contains(point))
            return false;

        var currentTime = DateTime.Now;
        var timeSinceLastButtonTap = (currentTime - _lastButtonTapTime).TotalMilliseconds;

        try
        {
            if (_currentState == PanelState.MainMenu)
            {
                return HandleMainMenuTap(point, currentTime, timeSinceLastButtonTap);
            }
            else if (_currentState == PanelState.TreeView)
            {
                return HandleTreeViewTap(point);
            }

            // Panel was tapped but no specific element - consume the tap to prevent closing
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error handling panel tap: {ex.Message}");
            return true; // Still consume the tap to prevent issues
        }
    }

    private bool HandleMainMenuTap(Point point, DateTime currentTime, double timeSinceLastButtonTap)
    {
        // Check if close button was tapped
        if (_closeButtonRect.Contains(point))
        {
            Hide();
            return true;
        }

        // Check if Visual Tree button was tapped
        if (_visualTreeButtonRect.Contains(point))
        {
            // Debounce button taps to prevent accidental double execution
            if (timeSinceLastButtonTap < ButtonTapDebounceMs)
            {
                Debug.WriteLine($"Visual Tree button tap ignored due to debouncing (< {ButtonTapDebounceMs}ms)");
                return true;
            }
            
            _lastButtonTapTime = currentTime;
            Debug.WriteLine("Visual Tree button tapped - showing tree view");
            _ = Task.Run(async () => await ShowVisualTreeView());
            return true;
        }

        // Check if Shell Hierarchy button was tapped
        if (_shellHierarchyButtonRect.Contains(point))
        {
            // Debounce button taps to prevent accidental double execution
            if (timeSinceLastButtonTap < ButtonTapDebounceMs)
            {
                Debug.WriteLine($"Shell Hierarchy button tap ignored due to debouncing (< {ButtonTapDebounceMs}ms)");
                return true;
            }
            
            _lastButtonTapTime = currentTime;
            Debug.WriteLine("Shell Hierarchy button tapped - showing tree view");
            _ = Task.Run(async () => await ShowShellHierarchyView());
            return true;
        }

        return true;
    }

    private bool HandleTreeViewTap(Point point)
    {
        // Check if back button was tapped
        if (_backButtonRect.Contains(point))
        {
            _currentState = PanelState.MainMenu;
            _currentTreeData = null;
            _scrollOffset = 0;
            _overlay.Invalidate();
            return true;
        }

        // Handle tapping on tree nodes directly
        HandleTreeNodeTap(point);
        return true;
    }

    private void HandleTreeNodeTap(Point point)
    {
        // Check if any tree node was tapped for expand/collapse
        for (int i = 0; i < _treeNodeRects.Count && i < GetFlattenedNodes().Count; i++)
        {
            if (_treeNodeRects[i].Contains(point))
            {
                var node = GetFlattenedNodes()[i];
                if (node.Children.Count > 0)
                {
                    node.IsExpanded = !node.IsExpanded;
                    _overlay.Invalidate();
                }
                break;
            }
        }
    }

    private List<TreeNode> GetFlattenedNodes()
    {
        var flattened = new List<TreeNode>();
        if (_currentTreeData != null)
        {
            foreach (var root in _currentTreeData)
            {
                FlattenNodes(root, flattened);
            }
        }
        return flattened;
    }

    private void FlattenNodes(TreeNode node, List<TreeNode> flattened)
    {
        flattened.Add(node);
        if (node.IsExpanded)
        {
            foreach (var child in node.Children)
            {
                FlattenNodes(child, flattened);
            }
        }
    }

    public void Show()
    {
        _isVisible = true;
        _currentState = PanelState.MainMenu; // Reset to main menu when showing
        _currentTreeData = null;
        _scrollOffset = 0;
        _overlay.Invalidate(); // Force redraw
    }

    public void Hide()
    {
        _isVisible = false;
        _currentState = PanelState.MainMenu;
        _currentTreeData = null;
        _scrollOffset = 0;
        _overlay.Invalidate(); // Force redraw
    }

    public void Toggle()
    {
        if (_isVisible)
            Hide();
        else
            Show();
    }

    private async Task ShowVisualTreeView()
    {
        try
        {
            var options = new VisualTreeDumpService.DumpOptions
            {
                IncludeLayoutProperties = true,
                IncludeHandlerInfo = true,
                IncludeMauiReactorInfo = true,
                MaxDepth = 10
            };

            var dump = _dumpService.DumpCurrentPage(options);
            _currentTreeData = ParseVisualTreeDump(dump);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _currentState = PanelState.TreeView;
                _overlay.Invalidate();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing visual tree view: {ex.Message}");
        }
    }

    private async Task ShowShellHierarchyView()
    {
        try
        {
            var options = new VisualTreeDumpService.DumpOptions
            {
                IncludeLayoutProperties = true,
                IncludeHandlerInfo = true,
                IncludeMauiReactorInfo = true,
                MaxDepth = 10
            };

            var dump = _dumpService.DumpShellHierarchy(options);
            _currentTreeData = ParseVisualTreeDump(dump);
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                _currentState = PanelState.TreeView;
                _overlay.Invalidate();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error showing shell hierarchy view: {ex.Message}");
        }
    }

    private List<TreeNode> ParseVisualTreeDump(string dump)
    {
        var lines = dump.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var rootNodes = new List<TreeNode>();
        var nodeStack = new Stack<TreeNode>();
        
        foreach (var line in lines)
        {
            // Skip header lines and empty lines
            if (line.StartsWith("===") || line.StartsWith("Timestamp:") || 
                line.StartsWith("Current Page:") || line.StartsWith("Main Page:") ||
                line.StartsWith("Navigation Context:") || string.IsNullOrWhiteSpace(line))
                continue;

            var depth = GetLineDepth(line);
            var cleanLine = line.Trim();
            
            // Extract element name and details
            var (name, details) = ExtractNodeInfo(cleanLine);
            
            var node = new TreeNode
            {
                Name = name,
                Details = details,
                Depth = depth,
                FullText = cleanLine,
                IsExpanded = false // Start collapsed by default
            };
            
            // Debug logging to understand the parsing
            Debug.WriteLine($"Parsed node - Depth: {depth}, Name: '{name}', Details: '{details}', Line: '{cleanLine.Substring(0, Math.Min(50, cleanLine.Length))}...'");

            // Manage the node hierarchy
            while (nodeStack.Count > 0 && nodeStack.Peek().Depth >= depth)
            {
                nodeStack.Pop();
            }

            if (nodeStack.Count == 0)
            {
                rootNodes.Add(node);
            }
            else
            {
                nodeStack.Peek().Children.Add(node);
            }

            nodeStack.Push(node);
        }

        return rootNodes;
    }

    private int GetLineDepth(string line)
    {
        // Count leading spaces to determine depth (IndentSize = 2 from VisualTreeDumpService)
        int spaces = 0;
        for (int i = 0; i < line.Length && line[i] == ' '; i++)
        {
            spaces++;
        }
        
        // Each level of depth uses IndentSize (2) spaces
        var depth = spaces / 2;
        
        Debug.WriteLine($"Line depth analysis: '{line.Substring(0, Math.Min(30, line.Length))}...' -> Depth: {depth} (spaces: {spaces})");
        return depth;
    }

    private (string name, string details) ExtractNodeInfo(string line)
    {
        // Remove leading whitespace and tree structure characters
        var cleaned = line.TrimStart();
        
        // Remove tree structure characters (├─ prefix used by VisualTreeDumpService)
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"^├─\s*", "");
        
        // Check if this is a property line (indented further, no ├─ prefix in original)
        var isPropertyLine = line.TrimStart().StartsWith("Size:") ||
                            line.TrimStart().StartsWith("Position:") ||
                            line.TrimStart().StartsWith("Handler:") ||
                            line.TrimStart().StartsWith("PlatformView:") ||
                            line.TrimStart().StartsWith("PlatformBounds:") ||
                            (line.Contains("|") && (line.Contains("Size:") || line.Contains("Position:") || line.Contains("H:") || line.Contains("V:")));
        
        if (isPropertyLine)
        {
            // Property lines: "Size: 400×300 | Position: (0,0) | H: Fill"
            if (cleaned.Contains(':'))
            {
                var colonIndex = cleaned.IndexOf(':');
                var beforeColon = cleaned[..colonIndex].Trim();
                var afterColon = cleaned[(colonIndex + 1)..].Trim();
                return (beforeColon, afterColon);
            }
            return (cleaned, "");
        }
        
        // Element lines: "ElementName [MauiReactor]" or "ElementName \"Text Content\""
        
        // Handle MauiReactor annotation: "ElementName [MauiReactor]"
        if (cleaned.Contains('[') && cleaned.Contains(']'))
        {
            var bracketStart = cleaned.IndexOf('[');
            var name = cleaned[..bracketStart].Trim();
            var details = cleaned[bracketStart..].Trim();
            return (string.IsNullOrEmpty(name) ? details : name, string.IsNullOrEmpty(name) ? "" : details);
        }
        
        // Handle quoted text content: 'ElementName "Text Content"'
        if (cleaned.Contains('"'))
        {
            var quoteStart = cleaned.IndexOf('"');
            var name = cleaned[..quoteStart].Trim();
            var details = cleaned[quoteStart..].Trim();
            return (string.IsNullOrEmpty(name) ? details : name, string.IsNullOrEmpty(name) ? "" : details);
        }
        
        // Handle ID annotations: "ElementName (Id: someId)"
        if (cleaned.Contains('(') && cleaned.Contains(')'))
        {
            var parenStart = cleaned.IndexOf('(');
            var name = cleaned[..parenStart].Trim();
            var details = cleaned[parenStart..].Trim();
            return (string.IsNullOrEmpty(name) ? details : name, string.IsNullOrEmpty(name) ? "" : details);
        }
        
        // Simple element name
        return (cleaned.Trim(), "");
    }

    // Legacy methods - keeping for backward compatibility but functionality moved to ShowVisualTreeView/ShowShellHierarchyView
    private async Task ExecuteVisualTreeDump()
    {
        await ShowVisualTreeView();
    }

    private async Task ExecuteShellHierarchyDump()
    {
        await ShowShellHierarchyView();
    }

    private async Task WriteToDebugFile(string content, string type)
    {
        try
        {
            // Get a suitable directory for debug files
            var debugDir = System.IO.Path.Combine(FileSystem.Current.AppDataDirectory, "debug-dumps");
            System.IO.Directory.CreateDirectory(debugDir);
            
            // Create filename with timestamp
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var filename = $"{type}-dump_{timestamp}.txt";
            var filePath = System.IO.Path.Combine(debugDir, filename);
            
            // Write content to file
            await System.IO.File.WriteAllTextAsync(filePath, content);
            
            // Also create a "latest" file for easy access
            var latestPath = System.IO.Path.Combine(debugDir, $"{type}-dump_latest.txt");
            await System.IO.File.WriteAllTextAsync(latestPath, content);
            
            Debug.WriteLine($"Debug dump saved to: {filePath}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to write debug file: {ex.Message}");
        }
    }
}