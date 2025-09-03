using System;

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
            //no implementation
        }
    }
}