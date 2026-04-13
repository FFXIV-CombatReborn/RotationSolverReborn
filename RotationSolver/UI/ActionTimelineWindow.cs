using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ECommons.GameHelpers;
using RotationSolver.ActionTimeline;

namespace RotationSolver.UI;

/// <summary>
/// Action Timeline window showing action execution history
/// </summary>
internal class ActionTimelineWindow : Window
{
	private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoScrollbar
										| ImGuiWindowFlags.NoCollapse
										| ImGuiWindowFlags.NoTitleBar
										| ImGuiWindowFlags.NoNav
										| ImGuiWindowFlags.NoScrollWithMouse;

	// Timeline display settings
	private readonly float SizePerSecond = 60f;
	private readonly float TimeOffset = 2f;
	private readonly bool IsHorizontal = true;
	private readonly int GCDIconSize = 40;
	private readonly int OGCDIconSize = 30;
	private readonly float GCDHeightLow = 0.5f;
	private readonly float GCDHeightHigh = 0.8f;

	// Reusable buffers to reduce per-frame allocations
	private static readonly List<TimelineItem> _filterBuffer = new(128);

	public ActionTimelineWindow() : base(nameof(ActionTimelineWindow), BaseFlags)
	{
		Size = new Vector2(560, 100);
		SizeCondition = ImGuiCond.FirstUseEver;
		Position = new Vector2(200, 200);
		PositionCondition = ImGuiCond.FirstUseEver;
	}

	public override void PreDraw()
	{
		Basic.Configuration.Configs config = Service.Config;
		Vector4 bgColor = config.IsControlWindowLock
			? config.ControlWindowLockBg
			: config.ControlWindowUnlockBg;
		ImGui.PushStyleColor(ImGuiCol.WindowBg, bgColor);

		Flags = BaseFlags;
		if (config.IsControlWindowLock)
		{
			Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
		}

		ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0, 0));

		base.PreDraw();
	}

	public override void PostDraw()
	{
		base.PostDraw();
		ImGui.PopStyleColor();
		ImGui.PopStyleVar(2);
	}

	public override void Draw()
	{
		if (!Player.Available)
		{
			ImGui.Text("Player not available");
			return;
		}

		using ImRaii.Style selectableAlign = ImRaii.PushStyle(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
		using var framePad = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(4, 3));
		using var childWinPad = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(12, 12));
		using var frameCellPadding = ImRaii.PushStyle(ImGuiStyleVar.CellPadding, new Vector2(4, 2));
		using var frameItemSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4));
		using var frameItemInnerSpacing = ImRaii.PushStyle(ImGuiStyleVar.ItemInnerSpacing, new Vector2(4, 4));
		using var frameIndentSpacing = ImRaii.PushStyle(ImGuiStyleVar.IndentSpacing, 21f);
		using var frameScrollbarSize = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarSize, 16f);
		using var frameGrabMinSize = ImRaii.PushStyle(ImGuiStyleVar.GrabMinSize, 13f);
		using var frameWindowRounding = ImRaii.PushStyle(ImGuiStyleVar.WindowRounding, 11f);
		using var frameChildRounding = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 11f);
		using var frameFrameRounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 11f);
		using var framePopupRounding = ImRaii.PushStyle(ImGuiStyleVar.PopupRounding, 11f);
		using var frameScrollbarRounding = ImRaii.PushStyle(ImGuiStyleVar.ScrollbarRounding, 11f);
		using var frameGrabRounding = ImRaii.PushStyle(ImGuiStyleVar.GrabRounding, 11f);
		using var frameTabRounding = ImRaii.PushStyle(ImGuiStyleVar.TabRounding, 11f);
		var pos = ImGui.GetWindowPos();
		var size = ImGui.GetWindowSize();
		var now = DateTime.Now;
		var endTime = now - TimeSpan.FromSeconds(size.X / SizePerSecond - TimeOffset);

		// Get timeline items from our timeline manager
		var items = ActionTimelineManager.Instance.GetItems(endTime, out var lastEndTime);

		// Filter items based on configuration
		var filteredItems = FilterItems(items);

		DrawTimeline(pos, size, now, filteredItems);

		// Draw time grid
		DrawGrid(pos, size);
	}

	private void DrawTimeline(Vector2 pos, Vector2 size, DateTime now, List<TimelineItem> items)
	{
		var drawList = ImGui.GetWindowDrawList();
		var timelineLength = IsHorizontal ? size.X : size.Y;
		var heightLength = IsHorizontal ? size.Y : size.X;

		foreach (var item in items)
		{
			DrawTimelineItem(drawList, pos, now, item, timelineLength, heightLength);
		}
	}

	private void DrawTimelineItem(ImDrawListPtr drawList, Vector2 pos, DateTime now, TimelineItem item, float timelineLength, float heightLength)
	{
		// Calculate position on timeline
		var timeSinceStart = (float)(now - item.StartTime).TotalSeconds;
		var itemDuration = (float)(item.EndTime - item.StartTime).TotalSeconds;

		var startX = pos.X + timelineLength - (timeSinceStart + TimeOffset) * SizePerSecond;
		var endX = startX + itemDuration * SizePerSecond;

		// Skip if completely outside visible area
		if (endX < pos.X || startX > pos.X + timelineLength) return;

		var iconSize = item.Type == TimelineItemType.GCD ? GCDIconSize : OGCDIconSize;
		var yOffset = item.Type == TimelineItemType.GCD ? heightLength * GCDHeightLow : heightLength * (GCDHeightLow + 0.1f);
		var itemHeight = item.Type == TimelineItemType.GCD ? heightLength * (GCDHeightHigh - GCDHeightLow) : iconSize;

		// Draw background bar for GCD actions
		if (item.Type == TimelineItemType.GCD)
		{
			var barColor = item.State switch
			{
				TimelineItemState.Casting => ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.8f, 0.2f, 0.8f)),
				TimelineItemState.Finished => ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 0.8f)),
				TimelineItemState.Canceled => ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.2f, 0.2f, 0.8f)),
				_ => ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 0.8f))
			};

			drawList.AddRectFilled(
				new Vector2(Math.Max(startX, pos.X), pos.Y + yOffset),
				new Vector2(Math.Min(endX, pos.X + timelineLength), pos.Y + yOffset + itemHeight),
				barColor,
				2f);
		}

		// Draw action icon
		if (IconSet.GetTexture(item.Icon, out IDalamudTextureWrap? texture) && texture != null)
		{
			var iconPos = new Vector2(startX, pos.Y + yOffset + (itemHeight - iconSize) / 2);

			// Ensure icon is within window bounds
			iconPos.X = Math.Max(iconPos.X, pos.X);
			iconPos.X = Math.Min(iconPos.X, pos.X + timelineLength - iconSize);

			drawList.AddImage(
				texture.Handle,
				iconPos,
				iconPos + new Vector2(iconSize, iconSize));
		}
		else
		{
			// Fallback: draw colored rectangle
			var fallbackColor = item.Type switch
			{
				TimelineItemType.GCD => ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.3f, 0.3f, 1f)),
				TimelineItemType.OGCD => ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.8f, 1f)),
				TimelineItemType.AutoAttack => ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, 1f)),
				_ => ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1f))
			};

			var iconPos = new Vector2(Math.Max(startX, pos.X), pos.Y + yOffset + (itemHeight - iconSize) / 2);
			iconPos.X = Math.Min(iconPos.X, pos.X + timelineLength - iconSize);

			drawList.AddRectFilled(
				iconPos,
				iconPos + new Vector2(iconSize, iconSize),
				fallbackColor);
		}
	}

	private void DrawGrid(Vector2 pos, Vector2 size)
	{
		var drawList = ImGui.GetWindowDrawList();
		var timelineLength = IsHorizontal ? size.X : size.Y;
		var gridColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 0.3f));

		// Determine step so we don't draw too many lines/labels per frame.
		float secondsVisible = MathF.Max(1f, timelineLength / SizePerSecond);
		int maxLines = 24;
		int stepSeconds = Math.Max(1, (int)MathF.Ceiling(secondsVisible / maxLines));

		for (int i = 0; i <= (int)secondsVisible; i += stepSeconds)
		{
			var x = pos.X + timelineLength - (i * SizePerSecond);
			if (x >= pos.X && x <= pos.X + timelineLength)
			{
				drawList.AddLine(
					new Vector2(x, pos.Y),
					new Vector2(x, pos.Y + size.Y),
					gridColor);

				var timeText = $"{i}s";
				drawList.AddText(
					new Vector2(x + 2, pos.Y + 2),
					gridColor,
					timeText);
			}
		}

		// Draw current time line
		var currentTimeX = pos.X + timelineLength - TimeOffset * SizePerSecond;
		var currentTimeColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.8f, 0.2f, 1f));
		drawList.AddLine(
			new Vector2(currentTimeX, pos.Y),
			new Vector2(currentTimeX, pos.Y + size.Y),
			currentTimeColor,
			3f);
	}

	/// <summary>
	/// Filter timeline items based on configuration settings
	/// </summary>
	private static List<TimelineItem> FilterItems(List<TimelineItem> items)
	{
		var config = Service.Config;
		_filterBuffer.Clear();

		foreach (var item in items)
		{
			// Filter oGCD actions based on config
			if (item.Type == TimelineItemType.OGCD && !config.ActionTimelineShowOgcd)
			{
				continue;
			}

			// Filter auto-attacks based on config
			if (item.Type == TimelineItemType.AutoAttack && !config.ActionTimelineShowAutoAttack)
			{
				continue;
			}

			_filterBuffer.Add(item);
		}

		return _filterBuffer;
	}

}
