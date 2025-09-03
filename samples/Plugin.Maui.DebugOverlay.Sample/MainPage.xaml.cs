
namespace Plugin.Maui.DebugOverlay.Sample;

public partial class MainPage : ContentPage
{
	// Memory churn state
	readonly List<byte[]> _buffers = new();
	CancellationTokenSource _memCts;
	Task _memTask;
	long _allocatedBytes;

	// CPU load state
	readonly List<Task> _cpuTasks = new();
	CancellationTokenSource _cpuCts;

	// Animation load state
	readonly List<BoxView> _boxes = new();
	CancellationTokenSource _animCts;
	readonly Dictionary<BoxView, (double vx, double vy)> _boxVels = new();

	bool _isShuttingDown;

	public MainPage()
	{
		InitializeComponent();
	}

	protected override void OnDisappearing()
	{
		base.OnDisappearing();
		_isShuttingDown = true;
		StopAll();
	}

	void StopAll()
	{
		OnStopMemoryClicked(this, EventArgs.Empty);
		OnStopCpuClicked(this, EventArgs.Empty);
		OnStopAnimClicked(this, EventArgs.Empty);
	}

	// Memory
	void OnStartMemoryClicked(object sender, EventArgs e)
	{
		if (_memCts != null || (_memTask != null && !_memTask.IsCompleted))
			return;

		var cts = new CancellationTokenSource();
		_memCts = cts;
		StartMemoryBtn.IsEnabled = false;
		StopMemoryBtn.IsEnabled = true;

		var chunkBytes = (int)(MemChunkSlider.Value * 1024 * 1024);

		_memTask = Task.Run(async () =>
		{
			try
			{
				var rnd = new Random();
				while (!cts.IsCancellationRequested)
				{
					// allocate
					var buf = new byte[chunkBytes];
					// touch memory so it actually commits
					for (int i = 0; i < buf.Length; i += 4096)
						buf[i] = (byte)rnd.Next(256);
					lock (_buffers) _buffers.Add(buf);
					Interlocked.Add(ref _allocatedBytes, buf.Length);

					// update label on UI
					var allocatedMb = _allocatedBytes / (1024.0 * 1024.0);
					Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
					{
						try { AllocatedLabel.Text = $"{allocatedMb:F0} MB"; }
						catch { /* UI torn down or not available */ }
					});

					// randomly free some buffers to create churn
					if (_buffers.Count > 8)
					{
						lock (_buffers)
						{
							int remove = Math.Min(4, _buffers.Count / 3);
							for (int i = 0; i < remove; i++)
							{
								var last = _buffers[^1];
								_buffers.RemoveAt(_buffers.Count - 1);
								Interlocked.Add(ref _allocatedBytes, -last.Length);
							}
						}
					}

					await Task.Delay(200, cts.Token).ConfigureAwait(false);
				}
			}
			catch (OperationCanceledException)
			{
				// expected on stop
			}
		}, cts.Token);
	}

	async void OnStopMemoryClicked(object sender, EventArgs e)
	{
		var cts = _memCts;
		if (cts != null)
		{
			_memCts = null;
			// Toggle UI state immediately; guard in case the page is tearing down
			if (!_isShuttingDown)
			{
				Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
				{
					try
					{
						StartMemoryBtn.IsEnabled = true;
						StopMemoryBtn.IsEnabled = false;
					}
					catch { /* UI torn down or not available */ }
				});
			}
			cts.Cancel();
			try
			{
				if (_memTask != null)
				{
					await _memTask;
				}
			}
			catch (OperationCanceledException) { }
			finally
			{
				cts.Dispose();
				_memTask = null;
			}
		}
		else
		{
			// Ensure correct UI state even if nothing to stop
			if (!_isShuttingDown)
			{
				Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(() =>
				{
					try
					{
						StartMemoryBtn.IsEnabled = true;
						StopMemoryBtn.IsEnabled = false;
					}
					catch { /* UI torn down or not available */ }
				});
			}
		}
	}

	void OnFreeMemoryClicked(object sender, EventArgs e)
	{
		lock (_buffers) _buffers.Clear();
		Interlocked.Exchange(ref _allocatedBytes, 0);
		AllocatedLabel.Text = "0 MB";
	}

	void OnGcCollectClicked(object sender, EventArgs e)
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	// CPU
	void OnStartCpuClicked(object sender, EventArgs e)
	{
		if (_cpuCts != null)
			return;

		_cpuCts = new CancellationTokenSource();
		_cpuTasks.Clear();

		StartCpuBtn.IsEnabled = false;
		StopCpuBtn.IsEnabled = true;

		int workers = (int)CpuWorkersSlider.Value;
		for (int w = 0; w < workers; w++)
		{
			_cpuTasks.Add(Task.Run(() => CpuWorker(_cpuCts.Token)));
		}
	}

	void CpuWorker(CancellationToken token)
	{
		// busy loop with periodic short sleeps to keep UI responsive
		var sw = System.Diagnostics.Stopwatch.StartNew();
		double x = 0;
		var rnd = new Random(Environment.TickCount);
		while (!token.IsCancellationRequested)
		{
			// do some floating point math
			for (int i = 0; i < 1_000_00; i++)
			{
				x += Math.Sin(i) * Math.Cos(x + i) * Math.Sqrt(i + 1);
				if (double.IsInfinity(x) || double.IsNaN(x)) x = rnd.NextDouble();
			}
			if (sw.ElapsedMilliseconds > 50)
			{
				Thread.SpinWait(10_000);
				sw.Restart();
			}
		}
		_ = x; // keep from being optimized away
	}

	void OnStopCpuClicked(object sender, EventArgs e)
	{
		_cpuCts?.Cancel();
		try { Task.WhenAll(_cpuTasks).Wait(250); } catch { }
		_cpuTasks.Clear();
		_cpuCts?.Dispose();
		_cpuCts = null;

		if (!_isShuttingDown)
		{
			StartCpuBtn.IsEnabled = true;
			StopCpuBtn.IsEnabled = false;
		}
	}

	// Animations
	async void OnStartAnimClicked(object sender, EventArgs e)
	{
		if (_animCts != null)
			return;

		_animCts = new CancellationTokenSource();
		StartAnimBtn.IsEnabled = false;
		StopAnimBtn.IsEnabled = true;

		int count = (int)BoxesCountSlider.Value;
		BoxesHost.Children.Clear();
		_boxes.Clear();
		_boxVels.Clear();

		// Ensure layout has non-zero size before placing boxes
		try
		{
			for (int i = 0; i < 120 && (BoxesHost.Width <= 0 || BoxesHost.Height <= 0); i++)
				await Task.Delay(16, _animCts.Token);
		}
		catch (OperationCanceledException)
		{
			return;
		}

		var width = Math.Max(0, BoxesHost.Width);
		var height = Math.Max(0, BoxesHost.Height);

		var rand = new Random();
		for (int i = 0; i < count; i++)
		{
			var size = rand.Next(12, 28); // make them larger for visibility
			var box = new BoxView
			{
				// bright colors for better contrast on dark background
				Color = Microsoft.Maui.Graphics.Color.FromRgb(rand.Next(128, 255), rand.Next(128, 255), rand.Next(128, 255)),
				Opacity = 0.9
			};
			var startX = width > 0 ? rand.NextDouble() * Math.Max(1, width - size) : rand.NextDouble() * 300;
			var startY = height > 0 ? rand.NextDouble() * Math.Max(1, height - size) : rand.NextDouble() * 200;
			AbsoluteLayout.SetLayoutBounds(box, new Microsoft.Maui.Graphics.Rect(startX, startY, size, size));
			AbsoluteLayout.SetLayoutFlags(box, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.None);
			_boxes.Add(box);
			BoxesHost.Children.Add(box);

			// Assign a persistent velocity per box (pixels per frame)
			double speed() => rand.NextDouble() * 3.0 + 1.0; // 1..4 px/frame
			var vx = speed() * (rand.Next(2) == 0 ? -1 : 1);
			var vy = speed() * (rand.Next(2) == 0 ? -1 : 1);
			_boxVels[box] = (vx, vy);
		}

		_ = AnimateBoxesAsync(_animCts.Token);
	}

	async Task AnimateBoxesAsync(CancellationToken token)
	{
		var rand = new Random();
		while (!token.IsCancellationRequested)
		{
			var width = BoxesHost.Width;
			var height = BoxesHost.Height;
			if (width <= 0 || height <= 0)
			{
				await Task.Delay(16, token);
				continue;
			}
			foreach (var box in _boxes)
			{
				var bounds = AbsoluteLayout.GetLayoutBounds(box);
				var (vx, vy) = _boxVels.TryGetValue(box, out var v) ? v : (1.0, 1.0);

				var nx = bounds.X + vx;
				var ny = bounds.Y + vy;

				// Bounce off edges
				if (nx < 0)
				{
					nx = 0;
					vx = Math.Abs(vx);
				}
				else if (nx + bounds.Width > width)
				{
					nx = Math.Max(0, width - bounds.Width);
					vx = -Math.Abs(vx);
				}

				if (ny < 0)
				{
					ny = 0;
					vy = Math.Abs(vy);
				}
				else if (ny + bounds.Height > height)
				{
					ny = Math.Max(0, height - bounds.Height);
					vy = -Math.Abs(vy);
				}

				_boxVels[box] = (vx, vy);
				AbsoluteLayout.SetLayoutBounds(box, new Microsoft.Maui.Graphics.Rect(nx, ny, bounds.Width, bounds.Height));
			}
			await Task.Delay(16, token); // ~60 FPS updates
		}
	}

	void OnStopAnimClicked(object sender, EventArgs e)
	{
		_animCts?.Cancel();
		_animCts?.Dispose();
		_animCts = null;
		if (!_isShuttingDown)
		{
			StartAnimBtn.IsEnabled = true;
			StopAnimBtn.IsEnabled = false;
		}
	}
}
