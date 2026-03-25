using Plugin.Maui.DebugOverlay.Platforms;
using System.Diagnostics;
namespace Plugin.Maui.DebugOverlay;

public class DebugOverlay : WindowOverlay
{
    private DebugRibbonElement _debugRibbonElement;
    private DebugOverlayPanel _debugPanel;
    private LoadTimeMetricsStore _loadTimeMetricsStore;

    private bool _isPanelVisible;
    private DateTime _lastTapTime = DateTime.MinValue;
    private const int TapDebounceMs = 300; // 300ms debounce
    private float _topInset;

    internal DebugOverlay(IWindow window, DebugRibbonOptions debugRibbonOptions, LoadTimeMetricsStore loadTimeMetricsStore) : base(window)
    {
        // Create ribbon element (always shows "DEBUG")
        _debugRibbonElement = new DebugRibbonElement(this, debugRibbonOptions, loadTimeMetricsStore, labelText: "DEBUG");
        this.AddWindowElement(_debugRibbonElement);

        // Create panel element (initially hidden)
        _debugPanel = new DebugOverlayPanel(this, debugRibbonOptions, loadTimeMetricsStore, panelBackgroundColor: Color.FromArgb("#E0000000"));
        this.AddWindowElement(_debugPanel);

        _isPanelVisible = false;

        Debug.WriteLine("DebugOverlay created with ribbon and panel elements");
        this.Tapped += DebugOverlay_Tapped;

        var pan = new GlobalPanGesture();
        pan.PanUpdated += (s, e) =>
        {
            if (_isPanelVisible)
            {
                _debugPanel.HandlePanUpdate(s, e);
                return;
            }
        };
        pan.Attach(Window);


    }

    private void DebugOverlay_Tapped(object? sender, WindowOverlayTappedEventArgs e)
    {
        var pointWithTopInset = e.Point;
        pointWithTopInset.Y -= _topInset;


        var currentTime = DateTime.Now;
        var timeSinceLastTap = (currentTime - _lastTapTime).TotalMilliseconds;

        Debug.WriteLine($"Overlay tapped at point: {e.Point.X}, {e.Point.Y} (time since last tap: {timeSinceLastTap}ms)");

        // Debounce rapid taps to prevent double-tap issues
        if (timeSinceLastTap < TapDebounceMs)
        {
            Debug.WriteLine($"Tap ignored due to debouncing (< {TapDebounceMs}ms)");
            return;
        }

        _lastTapTime = currentTime;

        try
        {
            // If panel is visible, consume ALL taps to prevent pass-through
            if (_isPanelVisible)
            {
                Debug.WriteLine("Panel is visible - consuming tap to prevent pass-through");
                _debugPanel.HandleTap(pointWithTopInset);
                return; // Always return early when panel is visible
            }

            // Panel is not visible - check if ribbon was tapped
            if (_debugRibbonElement.Contains(pointWithTopInset))
            {
                Debug.WriteLine($"=== RIBBON TAPPED: _isPanelVisible = {_isPanelVisible}, showing panel ===");
                TogglePanel();
                return;
            }

            // Panel not visible and ribbon not tapped - let tap pass through
            Debug.WriteLine("No overlay interaction - allowing tap to pass through");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error handling tap: {ex.Message}");
        }
    }

    public void ShowPanel()
    {
        if (!_isPanelVisible)
        {
            _isPanelVisible = true;
            _debugPanel.Show();
            Debug.WriteLine("Debug panel shown");
        }
    }

    public void HidePanel()
    {
        if (_isPanelVisible)
        {
            _isPanelVisible = false;
            _debugPanel.Hide();
            Debug.WriteLine("=== DEBUG OVERLAY: Panel hidden, _isPanelVisible = false ===");
        }
    }

    public void TogglePanel()
    {
        if (_isPanelVisible)
            HidePanel();
        else
            ShowPanel();
    }

    public override void HandleUIChange()
    {
        base.HandleUIChange();

        // Invalidate to force redraw of all elements
        this.Invalidate();


        _topInset = SafeAreaService.GetTopSafeAreaInset();
    }
}