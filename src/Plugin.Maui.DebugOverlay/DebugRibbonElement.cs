using System.Diagnostics;
using System.Reflection;

namespace Plugin.Maui.DebugOverlay;

public class DebugRibbonElement : IWindowOverlayElement
{
    private readonly WindowOverlay _overlay;
    private readonly LoadTimeMetricsStore _loadTimeMetricsStore;
    private readonly Color _ribbonColor;
    private PathF _ribbonPath;
    private RectF _backgroundRect;
    private readonly string _labelText;
    private readonly int _slowThresholdMs;
    private string _attentionInfoLoadTime = "";

    public string LabelText => _labelText;

    internal DebugRibbonElement(WindowOverlay overlay, DebugRibbonOptions debugRibbonOptions, LoadTimeMetricsStore loadTimeMetricsStore, string labelText = "DEBUG")
    {
        _overlay = overlay;
        _loadTimeMetricsStore = loadTimeMetricsStore;
        _ribbonColor = debugRibbonOptions.RibbonColor ?? Colors.MediumPurple;
        _backgroundRect = new RectF();
        _labelText = labelText ?? "DEBUG";
        _slowThresholdMs = debugRibbonOptions.SlowThresholdMs;
        // Initialize the background rect based on the current window size
        // This ensures hit detection works even before the first Draw call
        if (overlay?.Window != null)
        {
            var windowRect = new RectF(0, 0, (float)overlay.Window.Width, (float)overlay.Window.Height);
            UpdateBackgroundRect(windowRect);
        }

        _loadTimeMetricsStore.CollectionChanged += (action, id, ms) =>
        {
            CheckIfLoadTimeExceededSlowThreshold();
            _overlay.Invalidate(); // Redraw ribbon to update warning state if needed
        };
    }

    private void UpdateBackgroundRect(RectF windowRect)
    {
        // Calculate the hit detection area for the ribbon (same logic as in Draw)
        _backgroundRect = new RectF(windowRect.Right - 100, windowRect.Bottom - 80, 100, 80);
    }

    public bool Contains(Point point)
    {
        var contains = _backgroundRect.Contains(point);
        Debug.WriteLine($"Ribbon Contains check - Point: ({point.X:F1}, {point.Y:F1}), " +
                       $"Rect: ({_backgroundRect.X:F1}, {_backgroundRect.Y:F1}, {_backgroundRect.Width:F1}, {_backgroundRect.Height:F1}), " +
                       $"Contains: {contains}");
        return contains;
    }

    private void CheckIfLoadTimeExceededSlowThreshold()
    {
        if (_loadTimeMetricsStore.GetAll().Values.Count(x => x >= _slowThresholdMs) > 0)
            _attentionInfoLoadTime = "⚠️ ";
        else _attentionInfoLoadTime = "";
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        try
        {
            // Define the dimensions of the ribbon
            const float ribbonWidth = 130;
            const float ribbonHeight = 25;

            // Calculate the position of the ribbon in the lower right corner
            float ribbonX = dirtyRect.Right - (ribbonWidth * 0.25f);
            float ribbonY = dirtyRect.Bottom - (ribbonHeight + (ribbonHeight * 0.05f));

            // Update the background rect for hit testing (larger area for easier tapping)
            UpdateBackgroundRect(dirtyRect);

            // Save the current canvas state
            canvas.SaveState();

            // Translate the canvas to the start point of the ribbon
            canvas.Translate(ribbonX, ribbonY);

            // Rotate the canvas -45 degrees for the ribbon effect
            canvas.Rotate(-45);

            // Draw the ribbon background
            canvas.FillColor = _ribbonColor;
            _ribbonPath = new PathF();
            _ribbonPath.MoveTo(-ribbonWidth / 2, -ribbonHeight / 2);
            _ribbonPath.LineTo(ribbonWidth / 2, -ribbonHeight / 2);
            _ribbonPath.LineTo(ribbonWidth / 2, ribbonHeight / 2);
            _ribbonPath.LineTo(-ribbonWidth / 2, ribbonHeight / 2);
            _ribbonPath.Close();
            canvas.FillPath(_ribbonPath);

            // Add a subtle border for better visibility
            canvas.StrokeColor = Color.FromArgb("#40000000"); // Semi-transparent black
            canvas.StrokeSize = 1;
            canvas.DrawPath(_ribbonPath);

            var fontSize = 12f;
            // Draw the text
            canvas.FontColor = Colors.White;
            canvas.FontSize = fontSize;
            canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 600, FontStyleType.Normal);

            var textRect = new RectF(
                -ribbonWidth / 2,
                (-ribbonHeight - fontSize) / 2 + 2,
                ribbonWidth,
                ribbonHeight);

            canvas.DrawString(_attentionInfoLoadTime + _labelText, textRect, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Restore the canvas state
            canvas.RestoreState();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error drawing debug ribbon: {ex.Message}");
        }
    }
}