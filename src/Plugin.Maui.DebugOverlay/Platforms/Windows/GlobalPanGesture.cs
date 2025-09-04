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
            var handler = window?.Handler;
            if (handler == null) return;
            var platformView = handler.PlatformView as Microsoft.UI.Xaml.Window;
            if (platformView?.Content is Microsoft.UI.Xaml.FrameworkElement rootElement)
            {
                // Use AddHandler with handledEventsToo = true so we receive events even if others mark them handled
                Microsoft.UI.Xaml.UIElement uie = rootElement;

                // PointerPressed
                uie.AddHandler(Microsoft.UI.Xaml.UIElement.PointerPressedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((s, e) =>
                {
                    var p = e.GetCurrentPoint(rootElement).Position;
                    isPanning = true;
                    startX = p.X;
                    startY = p.Y;
                    // Capture pointer so we reliably get Released even if pointer moves outside
                    uie.CapturePointer(e.Pointer);
                    PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Started, 0, 0, p.X, p.Y));
                }), true);

                // PointerMoved
                uie.AddHandler(Microsoft.UI.Xaml.UIElement.PointerMovedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((s, e) =>
                {
                    if (!isPanning) return;
                    var p = e.GetCurrentPoint(rootElement).Position;
                    PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Running, p.X - startX, p.Y - startY, p.X, p.Y));
                }), true);

                // PointerReleased
                uie.AddHandler(Microsoft.UI.Xaml.UIElement.PointerReleasedEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((s, e) =>
                {
                    if (!isPanning) return;
                    isPanning = false;
                    var p = e.GetCurrentPoint(rootElement).Position;
                    // Release capture
                    try { uie.ReleasePointerCapture(e.Pointer); } catch { }
                    PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Completed, p.X - startX, p.Y - startY, p.X, p.Y));
                }), true);

                // Safety: treat canceled and capture-lost as Completed
                uie.AddHandler(Microsoft.UI.Xaml.UIElement.PointerCanceledEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((s, e) =>
                {
                    if (!isPanning) return;
                    isPanning = false;
                    try { uie.ReleasePointerCaptures(); } catch { }
                    var p = e.GetCurrentPoint(rootElement).Position;
                    PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Completed, p.X - startX, p.Y - startY, p.X, p.Y));
                }), true);

                uie.AddHandler(Microsoft.UI.Xaml.UIElement.PointerCaptureLostEvent, new Microsoft.UI.Xaml.Input.PointerEventHandler((s, e) =>
                {
                    if (!isPanning) return;
                    isPanning = false;
                    try { uie.ReleasePointerCaptures(); } catch { }
                    var p = e.GetCurrentPoint(rootElement).Position;
                    PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Completed, p.X - startX, p.Y - startY, p.X, p.Y));
                }), true);
            }
        }
    }
}