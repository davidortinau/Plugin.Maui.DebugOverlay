namespace Plugin.Maui.DebugOverlay.Platforms
{
    public class GlobalPanGesture
    { 
        public enum GestureStatus { Started, Running, Completed }

        public class PanEventArgs : EventArgs
        {
            public GestureStatus Status { get; }
            public double TotalX { get; }
            public double X { get; }
            public double TotalY { get; }
            public double Y { get; }


            public PanEventArgs(GestureStatus status, double totalX, double totalY, double x, double y)
            {
                Status = status;
                TotalX = totalX;
                TotalY = totalY;

                X = x;
                Y = y;
            }
        }

        public event EventHandler<PanEventArgs>? PanUpdated;

        private double startX, startY;
        private bool isPanning = false;

        public void Attach(IWindow window)
        {
            var mauiWinUIWindow = window.Handler.PlatformView as Microsoft.UI.Xaml.Window;
            if (mauiWinUIWindow?.Content is Microsoft.UI.Xaml.FrameworkElement rootElement)
            {
                rootElement.PointerPressed += (s, e) =>
                {
                    var p = e.GetCurrentPoint(rootElement).Position;
                    isPanning = true;
                    startX = p.X;
                    startY = p.Y;
                    PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Started, 0, 0, p.X, p.Y));
                };
                rootElement.PointerMoved += (s, e) =>
                {
                    if (!isPanning) return;
                    var p = e.GetCurrentPoint(rootElement).Position;
                    PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Running, p.X - startX, p.Y - startY, p.X, p.Y));
                };
                rootElement.PointerReleased += (s, e) =>
                {
                    if (!isPanning) return;
                    isPanning = false;
                    var p = e.GetCurrentPoint(rootElement).Position;
                    PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Completed, p.X - startX, p.Y - startY, p.X, p.Y));
                };
            }
        }
    }
}