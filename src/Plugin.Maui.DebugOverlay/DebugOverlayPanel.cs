using Plugin.Maui.DebugOverlay.Platforms;
using System.Diagnostics;
using System.Reflection;

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
    protected DebugRibbonOptions _debugRibbonOptions;
    private readonly DebugOverlay _overlay;
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
    private RectF _scrollUpButtonRect;
    private RectF _scrollDownButtonRect;
    private RectF _exportButtonRect;

    // Tree view state
    private float _scrollOffset = 0f;
    private const float LineHeight = 20f;
    private List<RectF> _treeNodeRects = new();

    // Touch-based scrolling state
    private Point _touchStartPoint;
    private DateTime _touchStartTime;
    private bool _isScrolling = false;
    private float _scrollVelocity = 0f;
    private const float MinScrollDistance = 10f; // Minimum distance to trigger scroll
    private const float ScrollSensitivity = 1.5f; // Scroll speed multiplier
    private const float MaxScrollVelocity = 500f; // Maximum scroll velocity

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

    public DebugOverlayPanel(DebugOverlay overlay, DebugRibbonOptions debugRibbonOptions, Color panelBackgroundColor = null)
    {
        _debugRibbonOptions = debugRibbonOptions;
        _overlay = overlay;

        _dumpService = new VisualTreeDumpService();
        _panelBackgroundColor = panelBackgroundColor ?? Color.FromArgb("#E0000000"); // Semi-transparent black
        _buttonBackgroundColor = Color.FromArgb("#FF4A4A4A"); // Dark gray buttons
        _textColor = Colors.White;
        _isVisible = false;

        // Get MAUI version
        var version = typeof(MauiApp).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        _mauiVersion = version != null && version.Contains('+') ? version[..version.IndexOf('+')] : version ?? "Unknown";

        // Performances
        _stopwatch = new Stopwatch();

        _fpsService = new FpsService();
        _fpsService.OnFrameTimeCalculated += _fpsService_OnFrameTimeCalculated;
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
                DrawScrollButtons(canvas, contentRect);
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
        _headerRect = new RectF(contentRect.X, contentRect.Y, contentRect.Width, 50);

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
        DrawButton(canvas, _visualTreeButtonRect, "🔍 View Visual Tree", _buttonBackgroundColor);

        // Shell Hierarchy Button
        buttonY += ButtonHeight + ButtonSpacing;
        _shellHierarchyButtonRect = new RectF(contentRect.X, buttonY, buttonWidth, ButtonHeight);
        DrawButton(canvas, _shellHierarchyButtonRect, "🐚 View Shell Hierarchy", _buttonBackgroundColor);

        // FPS
        buttonY += ButtonHeight + ButtonSpacing;
        var fpsButtonRect = new RectF(contentRect.X, buttonY, buttonWidth, ButtonHeight);
        DrawButton(canvas, fpsButtonRect, $"🐚 Fps: {_emaFps:F1}", _buttonBackgroundColor);
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
        var closeButtonSize = 30f;
        var margin = 10f;
        // Center the close button vertically within the header area (header height is 50px)
        var headerHeight = 50f;
        var buttonY = contentRect.Y + (headerHeight - closeButtonSize) / 2;

        _closeButtonRect = new RectF(
            contentRect.Right - closeButtonSize - margin,
            buttonY,
            closeButtonSize,
            closeButtonSize);

        canvas.SaveState();

        // Close button background
        canvas.FillColor = Color.FromArgb("#FFFF4444");
        canvas.FillRoundedRectangle(_closeButtonRect, 4);

        // Close button border
        canvas.StrokeColor = Color.FromArgb("#FFAA2222");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(_closeButtonRect, 4);

        // Draw X
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 2;
        var xMargin = 8f;
        canvas.DrawLine(
            _closeButtonRect.X + xMargin,
            _closeButtonRect.Y + xMargin,
            _closeButtonRect.Right - xMargin,
            _closeButtonRect.Bottom - xMargin);
        canvas.DrawLine(
            _closeButtonRect.Right - xMargin,
            _closeButtonRect.Y + xMargin,
            _closeButtonRect.X + xMargin,
            _closeButtonRect.Bottom - xMargin);

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

        // Export button in top-right corner of header
        var buttonSize = 30f;
        var margin = 10f;
        _exportButtonRect = new RectF(
            _headerRect.Right - buttonSize - margin,
            _headerRect.Y + (_headerRect.Height - buttonSize) / 2,
            buttonSize,
            buttonSize);

        // Draw export button
        canvas.FillColor = Color.FromArgb("#FF4A4A4A");
        canvas.FillRoundedRectangle(_exportButtonRect, 4);
        canvas.StrokeColor = Color.FromArgb("#FF666666");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(_exportButtonRect, 4);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 14;
        canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 600, FontStyleType.Normal);
        canvas.DrawString("💾", _exportButtonRect, HorizontalAlignment.Center, VerticalAlignment.Center);

        canvas.RestoreState();
    }

    private void DrawBackButton(ICanvas canvas, RectF contentRect)
    {
        var backButtonSize = 30f;
        var margin = 10f;
        // Center the back button vertically within the header area (header height is 50px)
        var headerHeight = 50f;
        var buttonY = contentRect.Y + (headerHeight - backButtonSize) / 2;

        _backButtonRect = new RectF(
            contentRect.X + margin,
            buttonY,
            backButtonSize,
            backButtonSize);

        canvas.SaveState();

        // Back button background
        canvas.FillColor = Color.FromArgb("#FF4A4A4A");
        canvas.FillRoundedRectangle(_backButtonRect, 4);

        // Back button border
        canvas.StrokeColor = Color.FromArgb("#FF666666");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(_backButtonRect, 4);

        // Draw back arrow
        canvas.StrokeColor = Colors.White;
        canvas.StrokeSize = 3;
        var centerX = _backButtonRect.Center.X;
        var centerY = _backButtonRect.Center.Y;
        var size = 10f;

        // Arrow shape: <
        canvas.DrawLine(centerX + size / 2, centerY - size, centerX - size / 2, centerY);
        canvas.DrawLine(centerX - size / 2, centerY, centerX + size / 2, centerY + size);

        canvas.RestoreState();
    }

    private void DrawScrollButtons(ICanvas canvas, RectF contentRect)
    {
        if (_currentTreeData == null) return;

        var buttonSize = 32f;
        var margin = 8f;
        var spacing = 4f;

        // Position scroll buttons vertically stacked, flush left at bottom
        var startX = contentRect.X + Padding;
        var startY = contentRect.Bottom - (buttonSize * 2) - spacing - margin;

        _scrollUpButtonRect = new RectF(
            startX,
            startY,
            buttonSize,
            buttonSize);

        _scrollDownButtonRect = new RectF(
            startX,
            startY + buttonSize + spacing,
            buttonSize,
            buttonSize);

        canvas.SaveState();

        // Draw scroll up button
        canvas.FillColor = Color.FromArgb("#FF4A4A4A");
        canvas.FillRoundedRectangle(_scrollUpButtonRect, 4);
        canvas.StrokeColor = Color.FromArgb("#FF666666");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(_scrollUpButtonRect, 4);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 18;
        canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 700, FontStyleType.Normal);
        canvas.DrawString("↑", _scrollUpButtonRect, HorizontalAlignment.Center, VerticalAlignment.Center);

        // Draw scroll down button
        canvas.FillColor = Color.FromArgb("#FF4A4A4A");
        canvas.FillRoundedRectangle(_scrollDownButtonRect, 4);
        canvas.StrokeColor = Color.FromArgb("#FF666666");
        canvas.StrokeSize = 1;
        canvas.DrawRoundedRectangle(_scrollDownButtonRect, 4);

        canvas.FontColor = Colors.White;
        canvas.FontSize = 18;
        canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 700, FontStyleType.Normal);
        canvas.DrawString("↓", _scrollDownButtonRect, HorizontalAlignment.Center, VerticalAlignment.Center);

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
        var indentSize = 15f; // Optimized indent for better screen utilization
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
        var expanderX = nodeX + (node.Depth > 0 ? 5f : 0f); // Small offset from node position
        var expanderRect = new RectF(expanderX, y + (nodeHeight - expanderSize) / 2, expanderSize, expanderSize);
        var nodeRect = new RectF(nodeX, y, width - nodeX, nodeHeight);

        // Store rect for hit testing (include expander area)
        var hitRect = new RectF(nodeX, y, width - nodeX, nodeHeight);
        _treeNodeRects.Add(hitRect);

        canvas.SaveState();

        // Skip drawing connecting lines for cleaner look

        // Draw expand/collapse indicator - simplified text-based approach
        if (node.Children.Count > 0)
        {
            canvas.FontColor = Colors.White;
            canvas.FontSize = 14;
            canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 700, FontStyleType.Normal);

            var symbol = node.IsExpanded ? "−" : "+";
            canvas.DrawString(symbol, expanderRect, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        // Calculate text position accounting for expander
        var textStartX = node.Children.Count > 0 ? expanderX + expanderSize + 4 : nodeX + 4;

        // Draw node text with better formatting (variables already declared at top of method)
        if (isPropertyLine)
        {
            // Property lines: smaller, dimmed, minimal indentation
            canvas.FontColor = Color.FromArgb("#FFAAAAAA");
            canvas.FontSize = 10;
            canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 400, FontStyleType.Normal);

            var displayText = string.IsNullOrEmpty(details) ? name : $"{name}: {details}";
            if (displayText.Length > 80)
                displayText = displayText[..77] + "...";

            // Minimal indentation for property lines - just align with parent element text
            var propertyIndent = nodeX + 8; // Small offset instead of heavy indentation
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
                canvas.DrawString(displayName, new RectF(textStartX, y, width - textStartX - 4, nodeHeight), HorizontalAlignment.Left, VerticalAlignment.Center);
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
                canvas.DrawString(detailsText, new RectF(textStartX + nameWidth + 8, y, width - textStartX - nameWidth - 8, nodeHeight), HorizontalAlignment.Left, VerticalAlignment.Center);
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
            Debug.WriteLine("=== CLOSE BUTTON TAPPED: Calling parent overlay HidePanel() ===");
            // Notify parent overlay to hide panel properly
            _overlay.HidePanel();
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
            _isScrolling = false; // Reset scroll state
            _overlay.Invalidate();
            return true;
        }

        // Check if export button was tapped
        if (_exportButtonRect.Contains(point))
        {
            Debug.WriteLine("=== EXPORT BUTTON TAPPED ===");
            Debug.WriteLine($"=== EXPORT: Current tree data has {_currentTreeData?.Count ?? 0} root nodes ===");
            // Export the current tree data to file
            _ = Task.Run(async () => await ExportCurrentTreeToFile());
            return true;
        }

        // Check if scroll buttons were tapped
        if (_scrollUpButtonRect.Contains(point))
        {
            ApplyScroll(-LineHeight * 3); // Scroll up by 3 lines
            return true;
        }

        if (_scrollDownButtonRect.Contains(point))
        {
            ApplyScroll(LineHeight * 3); // Scroll down by 3 lines
            return true;
        }

        // Since WindowOverlay only provides tap events, implement scroll using simple position tracking
        var currentTime = DateTime.Now;
        var timeSinceLastTouch = (currentTime - _touchStartTime).TotalMilliseconds;

        // Reset scroll tracking if too much time has passed
        if (timeSinceLastTouch > 200)
        {
            _touchStartPoint = point;
            _touchStartTime = currentTime;
            _isScrolling = false;
        }
        else if (timeSinceLastTouch > 50) // Quick successive taps might indicate scrolling
        {
            var yDiff = point.Y - _touchStartPoint.Y;
            var distance = Math.Abs(yDiff);

            if (distance > MinScrollDistance)
            {
                // Calculate scroll amount - make it more responsive
                var scrollAmount = (float)(yDiff * ScrollSensitivity);

                Debug.WriteLine($"Scroll detected: yDiff={yDiff}, scrollAmount={scrollAmount}");
                ApplyScroll(scrollAmount);

                // Reset for next potential scroll
                _touchStartPoint = point;
                _touchStartTime = currentTime;
                return true;
            }
        }

        // Handle normal tree node tapping
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

    private void ApplyScroll(float scrollDelta)
    {
        if (_currentTreeData == null) return;

        var previousOffset = _scrollOffset;
        _scrollOffset += scrollDelta;

        // Calculate content bounds for scroll limiting
        var totalContentHeight = CalculateTreeContentHeight();
        var visibleHeight = GetTreeViewVisibleHeight();

        // Apply bounds checking
        var maxScrollOffset = Math.Max(0, totalContentHeight - visibleHeight);
        _scrollOffset = Math.Max(0, Math.Min(maxScrollOffset, _scrollOffset));

        // Only invalidate if scroll position actually changed
        if (Math.Abs(_scrollOffset - previousOffset) > 0.5f)
        {
            _overlay.Invalidate();
        }
    }

    private float CalculateTreeContentHeight()
    {
        if (_currentTreeData == null) return 0;

        var flattenedNodes = GetFlattenedNodes();
        return flattenedNodes.Count * LineHeight;
    }

    private float GetTreeViewVisibleHeight()
    {
        // Return the height of the scrollable tree view area
        // This should match the height used in DrawTreeView
        var windowHeight = _panelRect.Height;
        var (safeTop, safeBottom, _, _) = GetSafeAreaInsets(_panelRect);
        var contentHeight = windowHeight - safeTop - safeBottom - (ContentPadding * 2);
        var headerHeight = 50f; // Tree view header height

        return contentHeight - headerHeight - Padding;
    }

    public void Show()
    {
        _overlay.DisableUITouchEventPassthrough = true;
        _isVisible = true;
        _currentState = PanelState.MainMenu; // Reset to main menu when showing
        _currentTreeData = null;
        _scrollOffset = 0;
        _isScrolling = false; // Reset scroll state
        _overlay.Invalidate(); // Force redraw

        startMonitoringPerformances();
    }

    public void Hide()
    {
        _overlay.DisableUITouchEventPassthrough = false;
        _isVisible = false;
        _currentState = PanelState.MainMenu;
        _currentTreeData = null;
        _scrollOffset = 0;
        _isScrolling = false; // Reset scroll state
        _overlay.Invalidate(); // Force redraw

        stopMonitoringPerformances();
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
            Debug.WriteLine("=== SHELL HIERARCHY: Starting dump process ===");

            // Shell hierarchy dump must run on main thread to access UIKit components
            await MainThread.InvokeOnMainThreadAsync(() =>
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

                    Debug.WriteLine("=== SHELL HIERARCHY: Calling DumpShellHierarchy on main thread ===");
                    var dump = _dumpService.DumpShellHierarchy(options);

                    Debug.WriteLine($"=== SHELL HIERARCHY: Dump result length: {dump?.Length ?? 0} characters ===");
                    if (!string.IsNullOrEmpty(dump))
                    {
                        Debug.WriteLine($"=== SHELL HIERARCHY: First 200 chars: {dump.Substring(0, Math.Min(200, dump.Length))} ===");
                    }

                    Debug.WriteLine("=== SHELL HIERARCHY: Parsing dump data ===");
                    _currentTreeData = ParseVisualTreeDump(dump ?? string.Empty);

                    Debug.WriteLine($"=== SHELL HIERARCHY: Parsed {_currentTreeData?.Count ?? 0} root nodes ===");

                    Debug.WriteLine("=== SHELL HIERARCHY: Navigating to TreeView state ===");
                    _currentState = PanelState.TreeView;
                    _overlay.Invalidate();
                    Debug.WriteLine("=== SHELL HIERARCHY: Navigation complete ===");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"=== SHELL HIERARCHY ERROR (Main Thread): {ex.Message} ===");
                    Debug.WriteLine($"=== SHELL HIERARCHY STACK TRACE (Main Thread): {ex.StackTrace} ===");
                }
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== SHELL HIERARCHY ERROR (Async): {ex.Message} ===");
            Debug.WriteLine($"=== SHELL HIERARCHY STACK TRACE (Async): {ex.StackTrace} ===");
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

    private async Task ExportCurrentTreeToFile()
    {
        if (_currentTreeData == null)
        {
            Debug.WriteLine("=== EXPORT ERROR: No tree data available to export ===");
            return;
        }

        try
        {
            Debug.WriteLine("=== EXPORT: Starting export process ===");
            Debug.WriteLine($"=== EXPORT: Converting {_currentTreeData.Count} root nodes to export format ===");

            // Convert tree data back to readable format for export
            var exportContent = ConvertTreeToExportFormat(_currentTreeData);

            Debug.WriteLine($"=== EXPORT: Generated export content ({exportContent.Length} characters) ===");

            await WriteToDebugFile(exportContent, "visual-tree-export");

            Debug.WriteLine("=== EXPORT: Visual tree exported to file successfully ===");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== EXPORT ERROR: {ex.Message} ===");
            Debug.WriteLine($"=== EXPORT STACK TRACE: {ex.StackTrace} ===");
        }
    }

    private string ConvertTreeToExportFormat(List<TreeNode> treeData)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Visual Tree Export ===");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();

        foreach (var rootNode in treeData)
        {
            ConvertNodeToExportFormat(rootNode, sb, "");
        }

        return sb.ToString();
    }

    private void ConvertNodeToExportFormat(TreeNode node, System.Text.StringBuilder sb, string indent)
    {
        // Format similar to original dump format
        var nodeText = string.IsNullOrEmpty(node.Details)
            ? node.Name
            : $"{node.Name} {node.Details}";

        sb.AppendLine($"{indent}├─ {nodeText}");

        // Add children with increased indentation
        var childIndent = indent + "  ";
        foreach (var child in node.Children)
        {
            ConvertNodeToExportFormat(child, sb, childIndent);
        }
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

            Debug.WriteLine("==========================================");
            Debug.WriteLine($"=== FILE SAVED SUCCESSFULLY ===");
            Debug.WriteLine($"=== FILE PATH: {filePath} ===");
            Debug.WriteLine($"=== LATEST FILE: {latestPath} ===");
            Debug.WriteLine($"=== FILE SIZE: {content.Length} characters ===");
            Debug.WriteLine("==========================================");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"=== ERROR: Failed to write debug file: {ex.Message} ===");
        }
    }





    #region Performances

    #region variables
     
    private readonly FpsService _fpsService;

    private double _overallScore;
    private double _cpuUsage;
    private TimeSpan _prevCpuTime;
    private double _memoryUsage;
    private int _threadCount;
    private int _processorCount = Environment.ProcessorCount;


    private volatile bool _stopRequested = false;
    private Stopwatch _stopwatch;


    //FPS >EMA (Exponential Moving Average)
    private double _emaFrameTime = 0;
    private double _emaFps = 0;
    private const double _emaAlpha = 0.9;


    //hitch
    private const double HitchThresholdMs = 200;
    private double _emaHitch = 0;
    private double _emaHighestHitch = 0;
    private const double _emaHitchAlpha = 0.7; // more reactive than FPS/FrameTime


    //GC
    private int _gc0Prev = 0;
    private int _gc1Prev = 0;
    private int _gc2Prev = 0;

    private int _gc0Delta = 0;
    private int _gc1Delta = 0;
    private int _gc2Delta = 0;


    //Alloc/sec
    private long _lastTotalMemory = 0;
    private double _allocPerSec = 0;

    private readonly Process _currentProcess = Process.GetCurrentProcess();
    private long _lastAllocatedBytes = GC.GetTotalAllocatedBytes(false);

    //Networking
    long totalRequests = 0;
    long totalSent = 0;
    long totalReceived = 0;

    //double totalRequestsPerSecond = 0;
    //double totalSentPerSecond = 0;
    //double totalReceivedPerSecond = 0;

    double avgRequestTime = 0;

    //overall score
    private double _emaOverallScore = 0;
    private const double _emaOverallAlpha = 0.6; // 0–1, bigger = more reactiv

    private double _batteryMilliW = 0;
    private bool _batteryMilliWAvailable = true;
    #endregion

    #region methods
    private void startMonitoringPerformances()
    {
        _stopRequested = false;
        _fpsService?.Start();
        startMetrics();
    }

    private void startMetrics()
    {
        _stopwatch.Restart();
        _prevCpuTime = _currentProcess.TotalProcessorTime;

        Microsoft.Maui.Controls.Application.Current!.Dispatcher.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            if (_debugRibbonOptions.ShowAlloc_GC)
                updateGcAndAllocMetrics();

            updateExtraMetrics();

            if (_debugRibbonOptions.ShowCPU_Usage)
            {
                _memoryUsage = _currentProcess.WorkingSet64 / (1024 * 1024);
                _threadCount = _currentProcess.Threads.Count;

                var currentCpuTime = _currentProcess.TotalProcessorTime;
                double cpuDelta = (currentCpuTime - _prevCpuTime).TotalMilliseconds;

                double interval = _stopwatch.Elapsed.TotalMilliseconds; // ⬅️ real interval
                _cpuUsage = (cpuDelta / interval) * 100 / _processorCount;

                _prevCpuTime = currentCpuTime;
            }
            _stopwatch.Restart();

            _overallScore = calculateOverallScore();
            updateOverallScore(_overallScore);

            MainThread.BeginInvokeOnMainThread(invalidateOverlayToForceRedraw);

            return !_stopRequested;
        });
    }

    private void invalidateOverlayToForceRedraw()
    {
        _overlay.Invalidate(); 
    }

    private void stopMonitoringPerformances()
    {
        _fpsService?.Stop();
        _stopRequested = true;
    }

    private void _fpsService_OnFrameTimeCalculated(double frameTimeMs)
    {
        const double MinFrameTime = 0.1; // ms, pentru a evita diviziunea la zero
        frameTimeMs = Math.Max(frameTimeMs, MinFrameTime);

        // EMA FrameTime
        if (_emaFrameTime == 0)
            _emaFrameTime = frameTimeMs;
        else
            _emaFrameTime = (_emaAlpha * _emaFrameTime) + ((1 - _emaAlpha) * frameTimeMs);

        // EMA FPS
        double fps = 1000.0 / frameTimeMs;
        if (_emaFps == 0)
            _emaFps = fps;
        else
            _emaFps = (_emaAlpha * _emaFps) + ((1 - _emaAlpha) * fps);

        // Hitch EMA
        double hitchValue = frameTimeMs >= HitchThresholdMs ? frameTimeMs : 0;
        if (_emaHitch == 0)
            _emaHitch = hitchValue;
        else
            _emaHitch = (_emaHitchAlpha * _emaHitch) + ((1 - _emaHitchAlpha) * hitchValue);

        if (_emaHitch > _emaHighestHitch)
            _emaHighestHitch = _emaHitch;
    }




    private void updateGcAndAllocMetrics()
    {
        double elapsedSec = _stopwatch.Elapsed.TotalSeconds;
        if (elapsedSec <= 0) elapsedSec = 1; // fallback

        // Alloc/sec
        long currentAllocated = GC.GetTotalAllocatedBytes(false);
        long deltaAllocated = currentAllocated - _lastAllocatedBytes;
        _allocPerSec = (deltaAllocated / (1024.0 * 1024.0)) / elapsedSec; // MB/sec
        _lastAllocatedBytes = currentAllocated;

        // GC counts
        int gen0 = GC.CollectionCount(0);
        int gen1 = GC.CollectionCount(1);
        int gen2 = GC.CollectionCount(2);

        _gc0Delta = gen0 - _gc0Prev;
        _gc1Delta = gen1 - _gc1Prev;
        _gc2Delta = gen2 - _gc2Prev;

        _gc0Prev = gen0;
        _gc1Prev = gen1;
        _gc2Prev = gen2;

    }

    private void updateExtraMetrics()
    {
        if (_debugRibbonOptions.ShowBatteryUsage)
            updateBatteryUsage();

        //if (_debugRibbonOptions.ShowNetworkStats)
        //    updateNetworkStats();

    }

    //private void updateNetworkStats()
    //{
    //NETWORK FOR MOMENT IS STILL IN TEST AND NOT FULLY TESTED

    //var profiler = NetworkProfiler.Instance;

    //totalRequests = profiler.TotalRequests;
    //totalSent = profiler.TotalBytesSent;
    //totalReceived = profiler.TotalBytesReceived;
    //avgRequestTime = profiler.AverageRequestTimeMs;

    //totalReceivedPerSecond = profiler.BytesReceivedPerSecond;
    //totalSentPerSecond = profiler.BytesSentPerSecond;
    //totalRequestsPerSecond = profiler.RequestsPerSecond;
    //}

    private void updateBatteryUsage()
    {
#if ANDROID
        try
        {
            _batteryMilliW = BatteryService.GetBatteryMilliW();
            _batteryMilliWAvailable = true;
        }
        catch
        {
            _batteryMilliW = 0;
            _batteryMilliWAvailable = false;
        }
#else
            _batteryMilliW = 0;
            _batteryMilliWAvailable = false;
#endif
    }

    private double calculateOverallScore()
    {
        double score = 0;

        // FPS (max 3 puncte)
        if (_emaFps >= 50) score += 3;
        else if (_emaFps >= 30) score += 2;
        else score += 1;

        // CPU (max 3 puncte)
        if (_cpuUsage < 30) score += 3;
        else if (_cpuUsage < 60) score += 2;
        else score += 1;

        // Memory (max 2 puncte)
        if (_memoryUsage < 260) score += 2;
        else if (_memoryUsage < 400) score += 1;
        // >400 → 0

        // Threads (max 2 puncte)
        if (_threadCount < 50) score += 2;
        else if (_threadCount < 100) score += 1;
        // >100 → 0

        return score; // max 10
    }

    private void updateOverallScore(double rawScore)
    {

        if (_emaOverallScore == 0)
            _emaOverallScore = rawScore;
        else
            _emaOverallScore = (_emaOverallAlpha * _emaOverallScore) + ((1 - _emaOverallAlpha) * rawScore);
    }

    #endregion

    #endregion
}