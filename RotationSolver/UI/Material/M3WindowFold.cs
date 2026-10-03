using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace RotationSolver.UI.Material;

internal sealed class M3WindowFold
{
	private static readonly Vector2 MaximumSize = new(10000f, 10000f);

	private bool _minimized;
	private float _time;

	private bool _layout;
	private bool _settled;

	private Vector2 _restoreSize;
	private Vector2 _anchorOpen;
	private Vector2 _anchorFolded;

	private Vector2 _windowPos;
	private Vector2 _windowSize;
	private Vector2 _openPadding;
	private float _rounding;
	private bool _stylePushed;

	public bool IsMinimized => _minimized;

	public bool IsActive => _layout;

	public float Amount
	{
		get
		{
			var t = _time;
			return t < 0.5f ? 4f * t * t * t : 1f - (MathF.Pow((-2f * t) + 2f, 3f) * 0.5f);
		}
	}

	public float BarTop { get; set; }

	public Vector2 OpenPadding => _openPadding;

	public float Rounding => _rounding;

	private Vector2 AnchorInset => new(_openPadding.X, _openPadding.Y + BarTop);

	public void Restore()
	{
		if (!_minimized)
		{
			return;
		}

		if (_time >= 1f)
		{
			_anchorOpen = OpenAnchorNear(_anchorFolded);
		}

		_minimized = false;
	}

	public void Reset()
	{
		if (!_layout)
		{
			return;
		}

		Restore();
		_time = 0f;
	}

	public bool Prepare(Window window, int actionCount, in M3WindowBrand brand)
	{
		var style = ImGui.GetStyle();
		_openPadding = style.WindowPadding;
		_rounding = style.WindowRounding;

		var resting = _minimized && _time >= 1f;

		var target = _minimized ? 1f : 0f;
		if (_time != target)
		{
			var step = ImGui.GetIO().DeltaTime / M3Motion.EmphasisedDuration;
			_time = _minimized ? MathF.Min(1f, _time + step) : MathF.Max(0f, _time - step);
		}

		if (!_layout)
		{
			return false;
		}

		if (_settled && !_minimized && _time <= 0f)
		{
			_layout = false;
			_settled = false;
			return true;
		}

		var folded = Amount;
		var barSize = M3Widgets.WindowActionsSize(actionCount, brand, folded);
		var anchor = Vector2.Lerp(_anchorOpen, _anchorFolded, folded);
		var inset = AnchorInset * (1f - folded);
		var size = Vector2.Lerp(_restoreSize, barSize, folded);

		window.Position = resting ? null : new Vector2(anchor.X + inset.X - size.X, anchor.Y - inset.Y);
		window.PositionCondition = ImGuiCond.Always;
		window.Size = size / ImGuiHelpers.GlobalScale;
		window.SizeCondition = ImGuiCond.Always;
		window.SizeConstraints = new WindowSizeConstraints()
		{
			MinimumSize = Vector2.One,
			MaximumSize = MaximumSize,
		};

		// Don't save settings while folded, so the window reopens at its real size.
		window.Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings
			| (resting ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoMove);
		_settled = !_minimized && _time <= 0f;

		_rounding = float.Lerp(_rounding, barSize.Y * 0.5f, folded);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, _rounding);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Lerp(_openPadding, Vector2.Zero, folded));
		_stylePushed = true;
		return false;
	}

	public void PopStyle()
	{
		if (_stylePushed)
		{
			ImGui.PopStyleVar(2);
			_stylePushed = false;
		}
	}

	public void BeginDraw()
	{
		PopStyle();
		_windowPos = ImGui.GetWindowPos();
		_windowSize = ImGui.GetWindowSize();
	}

	public (Vector2 Pos, Vector2 Size) OpenRect()
	{
		if (!_layout)
		{
			return (_windowPos, _windowSize);
		}

		var inset = AnchorInset;
		return (new Vector2(_anchorOpen.X + inset.X - _restoreSize.X, _anchorOpen.Y - inset.Y), _restoreSize);
	}

	public int DrawBar(string id, ReadOnlySpan<M3WindowAction> actions, in M3WindowBrand brand, out bool closed, Vector4? fill = null, string closeTooltip = "Close")
	{
		var folded = Amount;

		Vector2 anchor;
		if (!_layout)
		{
			var inset = AnchorInset;
			anchor = new Vector2(_windowPos.X + _windowSize.X - inset.X, _windowPos.Y + inset.Y);
		}
		else
		{
			if (_minimized && _time >= 1f)
			{
				_anchorFolded = new Vector2(_windowPos.X + _windowSize.X, _windowPos.Y);
			}

			anchor = Vector2.Lerp(_anchorOpen, _anchorFolded, folded);
		}

		var barSize = M3Widgets.WindowActionsSize(actions.Length, brand, folded);
		ImGui.SetCursorScreenPos(new Vector2(anchor.X - barSize.X, anchor.Y));
		using var bar = ImRaii.Child(id, barSize, false,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);
		if (!bar)
		{
			closed = false;
			return -1;
		}

		var pressed = M3Widgets.WindowActions(id, anchor, actions, brand, folded, out var toggled, out closed, fill, closeTooltip);
		if (toggled)
		{
			Toggle(anchor);
		}

		return pressed;
	}

	private void Toggle(Vector2 anchor)
	{
		if (_minimized)
		{
			Restore();
			return;
		}

		if (!_layout)
		{
			_restoreSize = _windowSize;
			_anchorOpen = anchor;
			_anchorFolded = anchor;
			_layout = true;
		}

		_minimized = true;
	}

	private Vector2 OpenAnchorNear(Vector2 folded)
	{
		var inset = AnchorInset;
		var pos = new Vector2(folded.X + inset.X - _restoreSize.X, folded.Y - inset.Y);

		var viewport = ImGui.GetMainViewport();
		var min = viewport.WorkPos;
		var max = viewport.WorkPos + viewport.WorkSize;
		if (folded.X >= min.X && folded.X <= max.X && folded.Y >= min.Y && folded.Y <= max.Y)
		{
			pos.X = Math.Clamp(pos.X, min.X, MathF.Max(min.X, max.X - _restoreSize.X));
			pos.Y = Math.Clamp(pos.Y, min.Y, MathF.Max(min.Y, max.Y - _restoreSize.Y));
		}

		return new Vector2(pos.X + _restoreSize.X - inset.X, pos.Y + inset.Y);
	}
}
