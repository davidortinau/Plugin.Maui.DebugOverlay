using System.Diagnostics;

namespace Plugin.Maui.DebugOverlay;

public class DebugRibbonElement : IWindowOverlayElement
{
    private readonly Color _ribbonColor;
    public DebugRibbonElement(Color ribbonColor = null)
    {
        _ribbonColor = ribbonColor ?? Colors.MediumPurple;
    }
    public bool Contains(Point point)
    {
        return true;
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

            // Translate the canvas to the start point of the ribbon
            canvas.Translate(ribbonX, ribbonY);

            // Save the current state of the canvas
            canvas.SaveState();

            // Translate the canvas to the start point of the ribbon
            // canvas.Translate(ribbonWidth / 4, ribbonHeight);

            // Rotate the canvas 45 degrees
            canvas.Rotate(-45);

            // Draw the ribbon background
            canvas.FillColor = _ribbonColor;
            PathF ribbonPath = new PathF();
            ribbonPath.MoveTo(-ribbonWidth / 2, -ribbonHeight / 2);
            ribbonPath.LineTo(ribbonWidth / 2, -ribbonHeight / 2);
            ribbonPath.LineTo(ribbonWidth / 2, ribbonHeight / 2);
            ribbonPath.LineTo(-ribbonWidth / 2, ribbonHeight / 2);
            ribbonPath.Close();
            canvas.FillPath(ribbonPath);

            // Draw the text
            canvas.FontColor = Colors.White;
            canvas.FontSize = 12;
            canvas.Font = new Microsoft.Maui.Graphics.Font("ArialMT", 800, FontStyleType.Normal);
            canvas.DrawString("DEBUG", 
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
