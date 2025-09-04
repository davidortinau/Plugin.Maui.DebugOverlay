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
                var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
                if (activity?.Window?.DecorView != null)
                {
                    activity.Window.DecorView.Touch += (s, e) =>
                    {
                        var motion = e.Event;
                        switch (motion.Action)
                        {
                            case Android.Views.MotionEventActions.Down:
                                isPanning = true;
                                startX = motion.RawX;
                                startY = motion.RawY;
                                PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Started, 0, 0, motion.RawX, motion.RawY));
                                break;

                            case Android.Views.MotionEventActions.Move:
                                if (!isPanning) break;
                                PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Running,
                                    motion.RawX - startX, motion.RawY - startY, motion.RawX, motion.RawY));
                                break;

                            case Android.Views.MotionEventActions.Up:
                            case Android.Views.MotionEventActions.Cancel:
                                if (!isPanning) break;
                                isPanning = false;
                                PanUpdated?.Invoke(this, new PanEventArgs(GestureStatus.Completed,
                                    motion.RawX - startX, motion.RawY - startY, motion.RawX, motion.RawY));
                                break;
                        }
                        e.Handled = false;
                    };
                }
            });
        }
    }
}