using RotationSolver.UI.HighlightTeachingMode.ElementSpecial;
using RotationSolver.Updaters;
using System.Diagnostics;

namespace RotationSolver.UI.HighlightTeachingMode;

internal static class HotbarHighlightManager
{
	public static bool Enable { get; set; } = false;
	private static DrawingHighlightHotbar? _highLight;
	public static HashSet<HotbarID> HotbarIDs => _highLight?.HotbarIDs ?? [];

	public static Vector4 HighlightColor
	{
		get => _highLight?.Color ?? Vector4.One;
		set
		{
			if (_highLight == null)
			{
				return;
			}

			_highLight.Color = value;
		}
	}

	public static void Init()
	{
		_highLight = new DrawingHighlightHotbar(Service.Config.TeachingModeColor);
		UpdateSettings();
	}

	public static void UpdateSettings()
	{
		Enable = (Service.Config.TeachingMode || Service.Config.ReddenDisabledHotbarActions) && DataCenter.IsActivated() && MajorUpdater.IsValid;
		HighlightColor = Service.Config.TeachingModeColor;
	}

	public static void Dispose()
	{
		foreach (var item in new List<DrawingHighlightHotbarBase>(RotationSolverPlugin._drawingElements))
		{
			item.Dispose();
		}
		_highLight?.Dispose();
		_highLight = null;
		DrawingHighlightHotbar.DisposeTexture();
	}

	/// <summary>
	/// The drawings the overlay renders. Rebuilt on the framework thread by <see cref="UpdateElements"/>;
	/// the overlay only reads this reference, so it never touches game memory or <see cref="HotbarIDs"/>
	/// from the draw thread.
	/// </summary>
	internal static IDrawing2D[] Elements2D { get; private set; } = [];

	// Autorotation can pick an action and use it on the same frame, so a decision may be live for a single
	// frame. Each one is queued and held on screen briefly, otherwise fast oGCD choices are never visible.
	private const int MinDisplayMs = 150;

	// Bounds how far behind the live decision the display can drift when choices come in bursts.
	private const int MaxPending = 3;

	// Fallback redraw, so a bar that moved (dragged, page or cross-bar set switched) is tracked while the
	// highlighted action itself does not change.
	private const int RefreshIntervalMs = 33;

	private static readonly List<HotbarID[]> _pending = [];
	private static HotbarID[] _lastSeen = [];
	private static HotbarID[]? _displayed;
	private static readonly Stopwatch _displayTime = Stopwatch.StartNew();
	private static readonly Stopwatch _refresh = Stopwatch.StartNew();

	public static void ClearElements()
	{
		_pending.Clear();
		_lastSeen = [];
		_displayed = null;

		if (Elements2D.Length > 0)
		{
			Elements2D = [];
		}
	}

	/// <summary>
	/// Queues newly suggested actions and rebuilds the overlay drawings for the one being shown. Reads
	/// hotbar addons and the highlight set, so it must run on the framework thread.
	/// </summary>
	internal static void UpdateElements()
	{
		var live = HotbarIDs;

		// Capture every distinct suggestion, including ones that only last a frame.
		if (!SameIds(live, _lastSeen))
		{
			_lastSeen = [.. live];

			// Keep the most recent decisions rather than falling further behind.
			if (_pending.Count >= MaxPending)
			{
				_pending.RemoveAt(0);
			}

			_pending.Add(_lastSeen);
		}

		// Move to the next queued decision once the current one has had its time on screen.
		if (_pending.Count > 0 && (_displayed == null || _displayTime.ElapsedMilliseconds >= MinDisplayMs))
		{
			_displayed = _pending[0];
			_pending.RemoveAt(0);
			_displayTime.Restart();
		}
		else if (_displayed == null || _refresh.ElapsedMilliseconds < RefreshIntervalMs)
		{
			// Nothing new to show, and the periodic redraw is not due yet.
			return;
		}

		_refresh.Restart();
		_highLight?.SetDisplayIds(_displayed ?? []);

		List<IDrawing2D> result = [];
		foreach (var item in RotationSolverPlugin._drawingElements)
		{
			// Let each element update its per-frame state before drawing
			item.UpdateOnFrameMain();
			result.AddRange(item.To2DMain());
		}

		var elements = result.ToArray();
		Array.Sort(elements, static (a, b) => GetDrawingOrder(a).CompareTo(GetDrawingOrder(b)));
		Elements2D = elements;
	}

	private static bool SameIds(HashSet<HotbarID> live, HotbarID[] other)
	{
		if (live.Count != other.Length)
		{
			return false;
		}

		foreach (var id in other)
		{
			if (!live.Contains(id))
			{
				return false;
			}
		}

		return true;
	}

	private static int GetDrawingOrder(object drawing)
	{
		return drawing switch
		{
			PolylineDrawing poly => poly._thickness == 0 ? 0 : 1,
			ImageDrawing => 1,
			_ => 2,
		};
	}
}
