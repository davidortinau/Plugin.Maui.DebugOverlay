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
        internal bool ShowAlloc_GC { get; private set; } = false;

        /// <summary>
        /// Show CPU usage (total, app) and threads.
        /// </summary>
        internal bool ShowCPU_Usage { get; private set; } = false;

        /// <summary>
        /// Show memory usage (total, app).
        /// </summary>
        internal bool ShowMemory { get; private set; } = false;

        /// <summary>
        /// Show frames per second (FPS) and frame time (ms).
        /// </summary>
        internal bool ShowFrame { get; private set; } = false;

        /// <summary>
        /// Show load time per ms
        /// </summary>
        internal bool ShowLoadTime { get; private set; } = false;

        /// <summary>
        /// Threshold above which load time is considered slow (in ms).
        /// </summary>
        internal int SlowThresholdMs { get; private set; } = 0;

        /// <summary>
        /// Threshold above which load time is considered critical (in ms).
        /// </summary>
        internal int CriticalThresholdMs { get; private set; } = 0;


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
        public DebugRibbonOptions EnableBatteryUsage()
        {
            ShowBatteryUsage = true;
            return this;
        }

        /// <summary>
        /// Enable or disable GC and allocations display.
        /// </summary>
        public DebugRibbonOptions EnableGC()
        {
            ShowAlloc_GC = true;
            return this;
        }

        /// <summary>
        /// Enable or disable CPU usage display.
        /// </summary>
        public DebugRibbonOptions EnableCPU()
        {
            ShowCPU_Usage = true;
            return this;
        }

        /// <summary>
        /// Enable or disable memory usage display.
        /// </summary>
        public DebugRibbonOptions EnableMemory()
        {
            ShowMemory = true;
            return this;
        }

        /// <summary>
        /// Enable or disable FPS display.
        /// </summary>
        public DebugRibbonOptions EnableFrame()
        {
            ShowFrame = true;
            return this;
        }

        /// <summary>
        /// Enable and configure load time per component thresholds.
        /// </summary>
        /// <param name="slowThresholdMs">Threshold above which load time is considered slow.</param>
        /// <param name="criticalThresholdMs">Threshold above which load time is considered critical.</param>
        /// <returns>The updated <see cref="DebugRibbonOptions"/>.</returns>
        public DebugRibbonOptions EnableLoadTimePerComponents(int slowThresholdMs = 1000, int criticalThresholdMs = 1250)
        {
            ShowLoadTime = true;
            SlowThresholdMs = slowThresholdMs;
            CriticalThresholdMs = criticalThresholdMs;
            return this;
        }
    }
}