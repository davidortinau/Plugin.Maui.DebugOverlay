using System.Diagnostics;
using System.Reflection;

namespace Plugin.Maui.DebugOverlay;

public class DebugRibbonElement : IWindowOverlayElement
{
    private readonly WindowOverlay _overlay;
    private readonly Color _ribbonColor;
    private PathF _ribbonPath;
    private RectF _backgroundRect;
    private readonly string _labelText;

    public string LabelText => _labelText;

    public DebugRibbonElement(WindowOverlay overlay, string labelText = "DEBUG", Color ribbonColor = null)
    {
        _overlay = overlay;
        _ribbonColor = ribbonColor ?? Colors.MediumPurple;
        _backgroundRect = new RectF();
        _labelText = labelText ?? "DEBUG";
        
        // Initialize the background rect based on the current window size
        // This ensures hit detection works even before the first Draw call
        if (overlay?.Window != null)
        {
            var windowRect = new RectF(0, 0, (float)overlay.Window.Width, (float)overlay.Window.Height);
            UpdateBackgroundRect(windowRect);
        }
    }
    
    private void UpdateBackgroundRect(RectF windowRect)
    {
        // Get bottom safe area inset to account for Android navigation bar
        var bottomInset = GetBottomSafeAreaInset();
        
        // Calculate the hit detection area for the ribbon (same logic as in Draw)
        _backgroundRect = new RectF(windowRect.Right - 100, windowRect.Bottom - 80 - bottomInset, 100, 80);
    }
    
    private float GetBottomSafeAreaInset()
    {
        float bottom = 0f; // Default to no inset
        
#if ANDROID
        try
        {
            // Detect Android navigation bar height
            var context = Platform.CurrentActivity ?? Android.App.Application.Context;
            if (context?.Resources != null)
            {
                // Check if navigation bar is present
                var resourceId = context.Resources.GetIdentifier("navigation_bar_height", "dimen", "android");
                if (resourceId > 0)
                {
                    var navigationBarHeight = context.Resources.GetDimensionPixelSize(resourceId);
                    bottom = navigationBarHeight / context.Resources.DisplayMetrics.Density;
                    
                    // Additional check to see if navigation bar is actually shown
                    // This helps distinguish between devices with and without visible navigation buttons
                    var hasNavigationBar = HasNavigationBar(context);
                    if (!hasNavigationBar)
                    {
                        bottom = 0f;
                    }
                }
            }
        }
        catch
        {
            // Fall back to default (no inset)
        }
#endif
        
        return bottom;
    }
    
#if ANDROID
    private bool HasNavigationBar(Android.Content.Context context)
    {
        try
        {
            // Check if device has navigation bar by looking at configuration
            var resources = context.Resources;
            var id = resources.GetIdentifier("config_showNavigationBar", "bool", "android");
            if (id > 0)
            {
                return resources.GetBoolean(id);
            }
            
            // Fallback: check if navigation bar height > 0 and display has software keys
            var hasMenuKey = Android.Views.ViewConfiguration.Get(context).HasPermanentMenuKey;
            var hasBackKey = Android.Views.KeyCharacterMap.DeviceHasKey(Android.Views.Keycode.Back);
            var hasHomeKey = Android.Views.KeyCharacterMap.DeviceHasKey(Android.Views.Keycode.Home);
            
            return !hasMenuKey && !hasBackKey && !hasHomeKey;
        }
        catch
        {
            // If we can't determine, assume navigation bar exists for safety
            return true;
        }
    }
#endif
    
    public bool Contains(Point point) 
    {
        var contains = _backgroundRect.Contains(point);
        Debug.WriteLine($"Ribbon Contains check - Point: ({point.X:F1}, {point.Y:F1}), " +
                       $"Rect: ({_backgroundRect.X:F1}, {_backgroundRect.Y:F1}, {_backgroundRect.Width:F1}, {_backgroundRect.Height:F1}), " +
                       $"Contains: {contains}");
        return contains;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        try
        {
            // Define the dimensions of the ribbon
            const float ribbonWidth = 130;
            const float ribbonHeight = 25;
            
            // Get bottom safe area inset to account for Android navigation bar
            var bottomInset = GetBottomSafeAreaInset();
            
            // Calculate the position of the ribbon in the lower right corner
            float ribbonX = dirtyRect.Right - (ribbonWidth * 0.25f);
            float ribbonY = dirtyRect.Bottom - (ribbonHeight + (ribbonHeight * 0.05f)) - bottomInset;

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

            // Draw the text
            canvas.FontColor = Colors.White;
            canvas.FontSize = 12;
            canvas.Font = new Microsoft.Maui.Graphics.Font("Arial", 600, FontStyleType.Normal);
            
            var textRect = new RectF(
                -ribbonWidth / 2, 
                (-ribbonHeight / 2) + 2, 
                ribbonWidth, 
                ribbonHeight);
            
            canvas.DrawString(_labelText, textRect, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Restore the canvas state
            canvas.RestoreState();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error drawing debug ribbon: {ex.Message}");
        }
    }
}