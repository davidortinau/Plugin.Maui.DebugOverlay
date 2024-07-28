using System.Diagnostics;
using System.Reflection;

namespace Plugin.Maui.DebugOverlay;

public class DebugRibbonElement : IWindowOverlayElement
{
    readonly WindowOverlay _overlay;
    private readonly Color _ribbonColor;
    private PathF _ribbonPath;
    private RectF _backgroundRect;
    private string _mauiVersion;
    private string _labelText;

    public string LabelText{
        get { return _labelText; }
    }

    public DebugRibbonElement(WindowOverlay overlay, string labelText = "DEBUG", Color ribbonColor = null)
    {
        _overlay = overlay;
        _ribbonColor = ribbonColor ?? Colors.MediumPurple;
        _backgroundRect = new RectF();
        _labelText = labelText;

        
    }
    public bool Contains(Point point) 
    {
        Debug.WriteLine($"Point: {point.X}, {point.Y} and BackgroundRect: {_backgroundRect.X}, {_backgroundRect.Y}, {_backgroundRect.Width}, {_backgroundRect.Height}");
        return _backgroundRect.Contains(point);
    }

    
    

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        Debug.WriteLine("Drawing DebugOverlay");
        // Debug.WriteLine($"Width: {dirtyRect.Width}, Height: {dirtyRect.Height}");
        // Define the dimensions of the ribbon
            float ribbonWidth = 130;
            float ribbonHeight = 25;
            // Calculate the position of the ribbon in the lower right corner
            float ribbonX = dirtyRect.Right - (ribbonWidth * 0.25f);
            float ribbonY = dirtyRect.Bottom - (ribbonHeight + (ribbonHeight * 0.05f));
            // float ribbonWidth = dirtyRect.Right - (ribbonX*2);

            _backgroundRect = new RectF((dirtyRect.Right - 100), (dirtyRect.Bottom - 80), 100, 80);
            // canvas.FillColor = Colors.Black;
            // canvas.FillRectangle(_backgroundRect);

            // Translate the canvas to the start point of the ribbon
            canvas.Translate(ribbonX, ribbonY);

            Debug.WriteLine($"RibbonX: {ribbonX}, RibbonY: {ribbonY}");           
            
            // Save the current state of the canvas
            canvas.SaveState();
            // Rotate the canvas 45 degrees
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

            

            
            

            // Draw the text
            canvas.FontColor = Colors.White;
            canvas.FontSize = 12;
            canvas.Font = new Microsoft.Maui.Graphics.Font("ArialMT", 800, FontStyleType.Normal);
            canvas.DrawString(_labelText, 
                new RectF(
                    (-ribbonWidth / 2), 
                    (-ribbonHeight / 2) + 2, 
                    ribbonWidth, 
                    ribbonHeight), 
                HorizontalAlignment.Center, VerticalAlignment.Center);


            // Restore the canvas state
            canvas.RestoreState();
    }

}
