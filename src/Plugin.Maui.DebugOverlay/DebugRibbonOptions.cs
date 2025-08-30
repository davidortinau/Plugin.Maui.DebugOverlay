namespace Plugin.Maui.DebugOverlay
{
    public class DebugRibbonOptions
    {
        /// <summary>
        /// Ribbon color.
        /// </summary>
        internal Color RibbonColor { get; private set; } = Colors.MediumPurple;

        /// <summary>
        /// Only on Android: show battery usage in mW.
        /// </summary>
        internal bool ShowBatteryUsage { get; private set; } = false;

        /// <summary>
        /// Show GC and allocations (collections count, total memory).
        /// </summary>
        internal bool ShowAlloc_GC { get; private set; } = true;

        /// <summary>
        /// Show CPU usage (total, app) and threads.
        /// </summary>
        internal bool ShowCPU_Usage { get; private set; } = true;

        /// <summary>
        /// Show memory usage (total, app).
        /// </summary>
        internal bool ShowMemory { get; private set; } = false;

        /// <summary>
        /// Show frames per second (FPS) and frame time (ms).
        /// </summary>
        internal bool ShowFrame { get; private set; } = true;



        //#########################################################
        //################### Fluent methods ###################### 

        /// <summary>
        /// Set the ribbon color.
        /// </summary>
        public DebugRibbonOptions SetRibbonColor(Color color)
        {
            RibbonColor = color;
            return this;
        }

        /// <summary>
        /// Enable or disable battery usage display (Android only).
        /// </summary>
        public DebugRibbonOptions EnableBatteryUsage(bool enable = true)
        {
            ShowBatteryUsage = enable;
            return this;
        }

        /// <summary>
        /// Enable or disable GC and allocations display.
        /// </summary>
        public DebugRibbonOptions EnableGC(bool enable = true)
        {
            ShowAlloc_GC = enable;
            return this;
        }

        /// <summary>
        /// Enable or disable CPU usage display.
        /// </summary>
        public DebugRibbonOptions EnableCPU(bool enable = true)
        {
            ShowCPU_Usage = enable;
            return this;
        }

        /// <summary>
        /// Enable or disable memory usage display.
        /// </summary>
        public DebugRibbonOptions EnableMemory(bool enable = true)
        {
            ShowMemory = enable;
            return this;
        }

        /// <summary>
        /// Enable or disable FPS display.
        /// </summary>
        public DebugRibbonOptions EnableFrame(bool enable = true)
        {
            ShowFrame = enable;
            return this;
        }
    }



}
