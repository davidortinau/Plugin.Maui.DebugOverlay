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
            MainThread.BeginInvokeOnMainThread(() =>
            {
                var view = window.Handler.PlatformView as UIKit.UIView; // iOS and Mac
                if (view == null) return;

                var pan = new UIKit.UIPanGestureRecognizer(recognizer =>
                {
                    var translation = recognizer.TranslationInView(view);
                    switch (recognizer.State)
                    {
                        case UIKit.UIGestureRecognizerState.Began:
                            isPanning = true;
                            startX = translation.X;
                            startY = translation.Y;
                            PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Started, 0, 0, translation.X, translation.Y));
                            break;
                        case UIKit.UIGestureRecognizerState.Changed:
                            if (!isPanning) break;
                            PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Running,
                                translation.X - startX, translation.Y - startY, translation.X, translation.Y));
                            break;
                        case UIKit.UIGestureRecognizerState.Ended:
                        case UIKit.UIGestureRecognizerState.Cancelled:
                            if (!isPanning) break;
                            isPanning = false;
                            PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Completed,
                                translation.X - startX, translation.Y - startY, translation.X, translation.Y));
                            break;
                    }
                });

                view.AddGestureRecognizer(pan);
            });
        }
    }
}