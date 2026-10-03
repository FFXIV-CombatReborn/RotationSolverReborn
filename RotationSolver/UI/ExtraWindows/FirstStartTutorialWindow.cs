using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using ECommons.Logging;
using RotationSolver.Basic.Configuration;
using RotationSolver.Commands;
using RotationSolver.Data;
using RotationSolver.Helpers;
using RotationSolver.UI.Material;

namespace RotationSolver.UI.ExtraWindows;

internal sealed class FirstStartTutorialWindow : Window
{
	private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoTitleBar
		| ImGuiWindowFlags.NoCollapse
		| ImGuiWindowFlags.NoScrollbar
		| ImGuiWindowFlags.NoScrollWithMouse
		| ImGuiWindowFlags.NoSavedSettings;

	private const string DiscordUrl = "https://discord.gg/r9V4RHYt6v";

	private const float StepperLabelBreakpoint = 720f;

	private const float PageTransitionDuration = 0.18f;

	private static readonly Vector2 PreferredSize = new(900f, 720f);

	private enum Chapter : byte
	{
		GettingStarted,
		Configure,
		WrapUp,
	}

	private readonly record struct PageLink(string Label, MainWindowTab Tab, Action? Then = null);

	private sealed record Step(
		string Title,
		FontAwesomeIcon Icon,
		Chapter Chapter,
		string Summary,
		Action Draw,
		PageLink[]? Pages = null);

	private readonly record struct Tile(FontAwesomeIcon Icon, string Title, string Text, Vector4 Accent);

	private readonly record struct AccentPreset(string Name, uint Rgb);

	private readonly record struct StarterMacro(string Title, string Description, FontAwesomeIcon Icon, string Text);

	private readonly record struct CommandHelp(string Command, string Description);

	private static readonly AccentPreset[] AccentPresets =
	[
		new("Crimson (default)", 0xB0201F),
		new("Amber", 0xC7881C),
		new("Emerald", 0x2E9E63),
		new("Teal", 0x1F8F99),
		new("Azure", 0x2F6FD6),
		new("Violet", 0x7A4FD0),
		new("Rose", 0xC8427F),
	];

	private static readonly StarterMacro[] StarterMacros =
	[
		new("Auto, full AoE",
			"Turns on Auto with every AoE allowed. The everyday setup for dungeons and trash packs.",
			FontAwesomeIcon.Play,
			"/rotation Settings AoEType Full\r\n/rotation Auto"),
		new("Manual, cleave only",
			"Attacks only what you target, and keeps AoE to cleaves. For bosses with adds you do not want to pull.",
			FontAwesomeIcon.HandPointer,
			"/rotation Settings AoEType Cleave\r\n/rotation Manual"),
		new("Stop",
			"Turns RSR off. Keep this one within easy reach.",
			FontAwesomeIcon.Stop,
			"/rotation Off"),
	];

	private static readonly CommandHelp[] Commands =
	[
		new("/rotation", "Open the settings window. /rsr works anywhere /rotation does."),
		new("/rotation Auto", "Turn on Auto. Run it again to step to the next hostile targeting rule."),
		new("/rotation Manual", "Turn on Manual: RSR attacks whatever you target."),
		new("/rotation TargetOnly", "RSR picks targets and leaves every button press to you."),
		new("/rotation Off", "Turn RSR off."),
		new("/rotation Control", "Show the Auto / Manual / Off state window."),
		new("/rotation Changelog", "Open the update notes."),
	];

	private readonly Step[] _steps;
	private readonly bool[] _seen;

	private readonly M3HeroHeader _hero = new()
	{
		Height = 200f,
		MaxImageHeight = 200f,
		ImageFocus = new Vector2(0.5f, 0.25f),
		ImageBlend = 0.14f,
	};

	private int _stepIndex;
	private double _stepShownAt;
	private bool _scrollToTop;
	private bool _revealStep;
	private bool _checkedFirstStart;

	private const bool PracticeToggleDefault = true;
	private const float PracticeSliderDefault = 30f;
	private static bool _practiceToggle = PracticeToggleDefault;
	private static float _practiceSlider = PracticeSliderDefault;

	private M3Style.Scope _theme;

	public FirstStartTutorialWindow()
		: base("Rotation Solver Reborn Tutorial###rsrFirstStartTutorial", BaseFlags)
	{
		SizeConstraints = new WindowSizeConstraints()
		{
			MinimumSize = new Vector2(560f, 540f),
			MaximumSize = new Vector2(1600f, 1400f),
		};
		RespectCloseHotkey = true;

		_steps =
		[
			new("Welcome", FontAwesomeIcon.Rocket, Chapter.GettingStarted,
				"Rotation Solver Reborn (RSR) reads the fight every frame, works out the best next action for your job, and either displays it or performs it. This tour covers the essentials in about five minutes. (Reading comprehension required)",
				DrawWelcome),

			new("UI Customization", FontAwesomeIcon.Palette, Chapter.GettingStarted,
				"Every RSR window takes its colours from a single accent colour. Pick one you like and set the text and element sizes; this tour updates as you go.",
				DrawAppearance,
				[new("Open the UI page", MainWindowTab.UI)]),

			new("Turning RSR on", FontAwesomeIcon.PowerOff, Chapter.GettingStarted,
				"RSR is always in one of a few modes. It does nothing until autorotation is activaed, and it can switch itself off again after combat.",
				DrawModes),

			new("Settings Window", FontAwesomeIcon.Cog, Chapter.GettingStarted,
				"Type /rotation (or /rsr) in chat to open the settings. Here is how the window is laid out.",
				DrawSettingsTour,
				[new("Open the settings", MainWindowTab.Main)]),

			new("Job Rotation", FontAwesomeIcon.UserShield, Chapter.Configure,
				"Each combat job has one or more rotations to choose from. Rotations called 'Reborn' or 'Evolved' (Starting with Evercold) are the default rotations, other rotations are designed by not-me and are not tested, your milage may vary.",
				DrawRotationStep,
				[new("Open the rotation page", MainWindowTab.Rotation)]),

			new("Actions", FontAwesomeIcon.Bolt, Chapter.Configure,
				"Every action your rotation can use, grouped by type. Switch any of them off, or change when they are allowed to fire.",
				DrawActionsStep,
				[new("Open the Actions page", MainWindowTab.Actions)]),

			new("Auto", FontAwesomeIcon.Robot, Chapter.Configure,
				"Behaviour shared by every job: how freely to use AoE, when RSR switches itself on and off, healing thresholds and more.",
				DrawAutoStep,
				[new("Open the Auto page", MainWindowTab.Auto)]),

			new("Targeting", FontAwesomeIcon.Crosshairs, Chapter.Configure,
				"Which enemies RSR will attack in Auto, and the order it picks them in.",
				DrawTargetStep,
				[new("Open the Target page", MainWindowTab.Target)]),

			new("Overlays", FontAwesomeIcon.Desktop, Chapter.Configure,
				"Optional windows that show what RSR is doing, and teaching mode, which lights up the next action on your hotbars.",
				DrawOverlaysStep,
				[new("Open the UI page", MainWindowTab.UI)]),

			new("Lists and duties", FontAwesomeIcon.ListUl, Chapter.Configure,
				"Curated lists that tell every job how to react to particular statuses and actions, plus switches for particular kinds of content.",
				DrawListsStep,
				[new("Open the List page", MainWindowTab.List), new("Open the Duty page", MainWindowTab.Duty)]),

			new("Fine-tuning", FontAwesomeIcon.Stopwatch, Chapter.Configure,
				"Timing, events and backups. The defaults suit most players, so come back here once you are comfortable.",
				DrawAdvancedStep,
				[new("Open the Basic page", MainWindowTab.Basic), new("Open the Extra page", MainWindowTab.Extra)]),

			new("Macros", FontAwesomeIcon.Terminal, Chapter.WrapUp,
				"Everything in RSR can be driven from chat, which means it can live on your hotbar. Here are three macros to start with.",
				DrawMacrosStep,
				[new("Open the full command list", MainWindowTab.Main, MainWindow.OpenMacroList)]),

			new("You're all set", FontAwesomeIcon.FlagCheckered, Chapter.WrapUp,
				"That's the essentials. RSR stays off until you turn it on, so nothing will happen until you are ready.",
				DrawFinishStep),
		];

		_seen = new bool[_steps.Length];
	}

	public void OpenIfFirstStart()
	{
		if (_checkedFirstStart)
		{
			return;
		}

		_checkedFirstStart = true;

		if (!Service.Config.TutorialDone)
		{
			IsOpen = true;
		}
	}

	public override bool DrawConditions()
	{
		return DataCenter.PlayerAvailable();
	}

	public override void OnOpen()
	{
		Array.Clear(_seen);
		ShowStep(0);
		base.OnOpen();
	}

	public override void OnClose()
	{
		Service.Config.TutorialDone = true;
		Service.Config.Save();

		_hero.Reset();
		base.OnClose();
	}

	public override void PreDraw()
	{
		_theme = M3Style.Push();

		var viewport = ImGui.GetMainViewport();
		ImGui.SetNextWindowSize(Vector2.Min(PreferredSize * M3.Scale, viewport.Size * 0.9f), ImGuiCond.FirstUseEver);
		ImGui.SetNextWindowPos(viewport.Pos + (viewport.Size * 0.5f), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
		base.PreDraw();
	}

	public override void PostDraw()
	{
		base.PostDraw();
		_theme.Dispose();
		_theme = default;
	}

	public override void Draw()
	{
		var scale = M3.Scale;
		var style = ImGui.GetStyle();

		DrawHeader();

		var origin = ImGui.GetCursorScreenPos();
		var available = ImGui.GetContentRegionAvail();
		var footerHeight = style.ItemSpacing.Y + M3.Space2 + M3Widgets.ButtonHeight;
		var bodyHeight = MathF.Max(80f * scale, available.Y - footerHeight);
		var compact = ImGui.GetWindowWidth() < StepperLabelBreakpoint * scale;
		var stepperWidth = compact ? 52f * scale : MathF.Min(220f * scale, available.X * 0.34f);
		var gap = style.ItemSpacing.X;

		DrawStepper(stepperWidth, bodyHeight, compact);

		var dividerX = origin.X + stepperWidth + (gap * 0.5f);
		ImGui.GetWindowDrawList().AddLine(new Vector2(dividerX, origin.Y), new Vector2(dividerX, origin.Y + bodyHeight),
			M3.U32(M3.Scheme.OutlineVariant, 0.45f), 1f * scale);

		ImGui.SameLine(0f, gap);
		var pageMin = ImGui.GetCursorScreenPos();
		DrawPage(bodyHeight);

		DrawFooter();

		M3Snackbar.Draw(pageMin, new Vector2(origin.X + available.X, origin.Y + bodyHeight));
	}

	private void ShowStep(int index)
	{
		_stepIndex = Math.Clamp(index, 0, _steps.Length - 1);
		_seen[_stepIndex] = true;
		_stepShownAt = ImGui.GetTime();
		_scrollToTop = true;
		_revealStep = true;
	}

	private bool Reached(int index)
	{
		return index == _stepIndex || _seen[index];
	}

	private static string ChapterName(Chapter chapter)
	{
		return chapter switch
		{
			Chapter.GettingStarted => "Getting started",
			Chapter.Configure => "Configure",
			_ => "Wrap up",
		};
	}

	#region Frame

	private void DrawHeader()
	{
		var (min, max) = _hero.Draw(UpdateNotesWindow.GetBanner());
		DrawCloseButton(max.X, min.Y);

		ImGui.SetCursorScreenPos(new Vector2(ImGui.GetWindowPos().X + ImGui.GetStyle().WindowPadding.X, max.Y + (4f * M3.Scale)));
		DrawIdentity();
		ImGui.Dummy(new Vector2(0f, M3.Space1));
	}

	private void DrawIdentity()
	{
		const string title = "Welcome to Rotation Solver Reborn";
		const string subtitle = "First start tutorial";

		var s = M3.Scheme;
		var scale = M3.Scale;
		var origin = ImGui.GetCursorScreenPos();
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);

		float titleHeight;
		float subtitleHeight;
		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			titleHeight = ImGui.GetTextLineHeight();
		}

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			subtitleHeight = ImGui.GetTextLineHeight();
		}

		var textHeight = titleHeight + (2f * scale) + subtitleHeight;
		var height = MathF.Max(40f * scale, textHeight);
		ImGui.Dummy(new Vector2(width, height));
		var drawList = ImGui.GetWindowDrawList();

		var logoMin = origin + new Vector2(2f * scale, 0f);
		var logoMax = logoMin + new Vector2(height, height);
		if (!M3ActionIcon.Image(drawList, MainWindow.GetLogoTexture(), logoMin, logoMax, M3.ShapeSmall))
		{
			drawList.AddRectFilled(logoMin, logoMax, M3.U32(s.PrimaryContainer), M3.ShapeSmall);
			M3Draw.IconCentered(drawList, FontAwesomeIcon.Fire, logoMin, logoMax, s.OnPrimaryContainer);
		}

		var textX = logoMax.X + (14f * scale);
		var textWidth = MathF.Max(24f * scale, origin.X + width - textX);
		var textY = origin.Y + ((height - textHeight) * 0.5f);
		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			drawList.AddText(new Vector2(textX, textY), M3.U32(s.OnSurface), M3Navigation.Truncate(title, textWidth));
		}

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			drawList.AddText(new Vector2(textX, textY + titleHeight + (2f * scale)), M3.U32(s.Primary), M3Navigation.Truncate(subtitle, textWidth));
		}
	}

	private void DrawCloseButton(float right, float top)
	{
		var inset = 14f * M3.Scale;
		_ = M3Widgets.WindowActions("##tutorial_actions", new Vector2(right - inset, top + inset), [], out var closed,
			closeTooltip: "Close the tutorial");
		if (closed)
		{
			IsOpen = false;
		}
	}

	private void DrawStepper(float width, float height, bool compact)
	{
		using var child = ImRaii.Child("##tutorial_stepper", new Vector2(width, height), false, ImGuiWindowFlags.NoScrollbar);
		if (!child)
		{
			return;
		}

		var s = M3.Scheme;
		var scale = M3.Scale;
		var rowWidth = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var rowHeight = M3.FitText(38f, 7f);
		var drawList = ImGui.GetWindowDrawList();

		float numberHeight;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			numberHeight = ImGui.GetTextLineHeight();
		}

		var diameter = MathF.Min(MathF.Max(24f * scale, numberHeight + (8f * scale)), rowHeight - (6f * scale));
		var radius = diameter * 0.5f;
		var connectorGap = 3f * scale;

		using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
		for (var i = 0; i < _steps.Length; i++)
		{
			var step = _steps[i];
			var current = i == _stepIndex;
			var done = _seen[i] && !current;

			if (current && _revealStep)
			{
				ImGui.SetScrollHereY(0.5f);
				_revealStep = false;
			}

			if (ImGui.InvisibleButton($"##tutorial_step_{i}", new Vector2(rowWidth, rowHeight)) && !current)
			{
				ShowStep(i);
			}

			var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var held = ImGui.IsItemActive();
			var min = ImGui.GetItemRectMin();
			var max = ImGui.GetItemRectMax();
			var rounding = rowHeight * 0.5f;
			var selection = M3Motion.Approach($"##tutorial_step_selection_{i}", current ? 1f : 0f, M3Motion.EmphasisedDuration);

			if (selection > 0.01f)
			{
				drawList.AddRectFilled(min, max, M3.U32(s.SecondaryContainer, 0.95f * selection), rounding);
			}

			if (hovered || held)
			{
				drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), rounding);
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			var center = new Vector2(
				compact ? min.X + (rowWidth * 0.5f) : min.X + (10f * scale) + radius,
				min.Y + (rowHeight * 0.5f));

			if (i > 0)
			{
				DrawConnector(drawList, center.X, min.Y, center.Y - radius - connectorGap, Reached(i));
			}

			if (i < _steps.Length - 1)
			{
				DrawConnector(drawList, center.X, center.Y + radius + connectorGap, max.Y, Reached(i + 1));
			}

			var circleMin = center - new Vector2(radius, radius);
			var circleMax = center + new Vector2(radius, radius);
			if (current)
			{
				drawList.AddCircleFilled(center, radius, M3.U32(s.Primary), 32);
				DrawStepNumber(drawList, center, i + 1, s.OnPrimary);
			}
			else if (done)
			{
				drawList.AddCircleFilled(center, radius, M3.U32(s.PrimaryContainer), 32);
				M3Draw.IconCentered(drawList, FontAwesomeIcon.Check, circleMin, circleMax, s.OnPrimaryContainer);
			}
			else
			{
				drawList.AddCircleFilled(center, radius, M3.U32(s.SurfaceContainerHighest), 32);
				drawList.AddCircle(center, radius - (0.75f * scale), M3.U32(s.Outline, 0.8f), 32, 1.5f * scale);
				DrawStepNumber(drawList, center, i + 1, s.OnSurfaceVariant);
			}

			if (!compact)
			{
				var textX = center.X + radius + (12f * scale);
				var label = M3Navigation.Truncate(step.Title, MathF.Max(8f * scale, max.X - textX - (10f * scale)));
				var labelSize = ImGui.CalcTextSize(label);
				var color = current ? s.OnSecondaryContainer
					: done ? M3.Alpha(s.OnSurface, 0.9f)
					: M3.Alpha(s.OnSurfaceVariant, 0.85f);
				drawList.AddText(new Vector2(textX, center.Y - (labelSize.Y * 0.5f)), M3.U32(color), label);
			}
			else if (hovered)
			{
				ImguiTooltips.ShowTooltip($"{i + 1}. {step.Title}");
			}
		}
	}

	private static void DrawConnector(ImDrawListPtr drawList, float x, float top, float bottom, bool reached)
	{
		if (bottom - top < 0.5f)
		{
			return;
		}

		var s = M3.Scheme;
		var color = reached ? M3.Alpha(s.Primary, 0.7f) : M3.Alpha(s.OutlineVariant, 0.9f);
		drawList.AddLine(new Vector2(x, top), new Vector2(x, bottom), M3.U32(color), 2f * M3.Scale);
	}

	private static void DrawStepNumber(ImDrawListPtr drawList, Vector2 center, int number, Vector4 color)
	{
		using var font = ImRaii.PushFont(M3.LabelSmall);
		var text = number.ToString();
		drawList.AddText(center - (ImGui.CalcTextSize(text) * 0.5f), M3.U32(color), text);
	}

	private void DrawPage(float height)
	{
		var scale = M3.Scale;

		var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(M3.Space1, 0f));
		using var child = ImRaii.Child("##tutorial_page", new Vector2(0f, height), false, ImGuiWindowFlags.AlwaysUseWindowPadding);
		padding.Dispose();

		if (!child)
		{
			return;
		}

		if (_scrollToTop)
		{
			ImGui.SetScrollY(0f);
			_scrollToTop = false;
		}

		var progress = Math.Clamp((float)(ImGui.GetTime() - _stepShownAt) / PageTransitionDuration, 0f, 1f);
		progress = progress * progress * (3f - (2f * progress));
		using var fade = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.2f + (0.8f * progress));
		ImGui.Dummy(new Vector2(0f, M3.Space1 + ((1f - progress) * 10f * scale)));

		var step = _steps[_stepIndex];
		DrawStepHeader(step);

		using (ImRaii.PushId($"tutorial_step_{_stepIndex}"))
		{
			step.Draw();
		}

		ImGui.Dummy(new Vector2(0f, M3.Space3));
	}

	private static void DrawStepHeader(Step step)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var origin = ImGui.GetCursorScreenPos();
		var diameter = 48f * scale;
		var textX = origin.X + diameter + (16f * scale);
		var textWidth = MathF.Max(32f * scale, origin.X + width - textX);
		var overline = $"{ChapterName(step.Chapter)}".ToUpperInvariant();

		float overlineHeight;
		float titleHeight;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			overlineHeight = ImGui.GetTextLineHeight();
		}

		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			titleHeight = ImGui.CalcTextSize(step.Title, false, textWidth).Y;
		}

		var textHeight = overlineHeight + (4f * scale) + titleHeight;
		var height = MathF.Max(diameter, textHeight);
		var drawList = ImGui.GetWindowDrawList();

		var circleMin = new Vector2(origin.X, origin.Y + ((height - diameter) * 0.5f));
		var circleMax = circleMin + new Vector2(diameter, diameter);
		drawList.AddCircleFilled((circleMin + circleMax) * 0.5f, diameter * 0.5f, M3.U32(s.PrimaryContainer), 48);
		M3Draw.IconCentered(drawList, step.Icon, circleMin, circleMax, s.OnPrimaryContainer);

		var textTop = origin.Y + ((height - textHeight) * 0.5f);
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			drawList.AddText(new Vector2(textX, textTop), M3.U32(s.Primary), M3Navigation.Truncate(overline, textWidth));
		}

		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			_ = M3Draw.WrappedText(step.Title, new Vector2(textX, textTop + overlineHeight + (4f * scale)), textWidth, s.OnSurface);
		}

		ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + height));
		ImGui.Dummy(new Vector2(width, M3.Space1));

		using (ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(s.OnSurfaceVariant, 0.95f)))
		{
			ImGui.TextWrapped(step.Summary);
		}

		if (step.Pages is { Length: > 0 } pages)
		{
			ImGui.Dummy(new Vector2(0f, M3.Space1));
			var right = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X;
			for (var i = 0; i < pages.Length; i++)
			{
				var page = pages[i];
				if (i > 0)
				{
					FlowSameLine(M3Widgets.ButtonWidth(FontAwesomeIcon.ExternalLinkAlt, page.Label), M3.Space2, right);
				}

				if (M3Widgets.Button($"##tutorial_open_page_{i}", page.Label, M3ButtonStyle.Tonal, FontAwesomeIcon.ExternalLinkAlt,
					tooltip: "Opens the settings window on this page. The tutorial stays open."))
				{
					RotationSolverPlugin.ShowConfigWindow(page.Tab);
					page.Then?.Invoke();
				}
			}
		}

		ImGui.Dummy(new Vector2(0f, M3.Space3));
	}

	private void DrawFooter()
	{
		const string skipLabel = "Skip tutorial";
		const string backLabel = "Back";

		var s = M3.Scheme;
		var scale = M3.Scale;
		var last = _stepIndex == _steps.Length - 1;
		var origin = ImGui.GetCursorScreenPos();
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var right = origin.X + width;
		var gap = M3.Space2;
		var y = origin.Y + M3.Space2;

		ImGui.GetWindowDrawList().AddLine(origin, new Vector2(right, origin.Y), M3.U32(s.OutlineVariant, 0.5f), 1f * scale);

		var nextWidth = MathF.Max(M3Widgets.ButtonWidth(FontAwesomeIcon.ArrowRight, "Next"), M3Widgets.ButtonWidth(FontAwesomeIcon.Check, "Finish"));
		var backWidth = M3Widgets.ButtonWidth(FontAwesomeIcon.ArrowLeft, backLabel);

		ImGui.SetCursorScreenPos(new Vector2(right - nextWidth, y));
		if (M3Widgets.Button("##tutorial_next", last ? "Finish" : "Next", M3ButtonStyle.Filled,
			last ? FontAwesomeIcon.Check : FontAwesomeIcon.ArrowRight, nextWidth))
		{
			if (last)
			{
				IsOpen = false;
			}
			else
			{
				ShowStep(_stepIndex + 1);
			}
		}

		ImGui.SetCursorScreenPos(new Vector2(right - nextWidth - gap - backWidth, y));
		if (M3Widgets.Button("##tutorial_back", backLabel, M3ButtonStyle.Outlined, FontAwesomeIcon.ArrowLeft, backWidth, _stepIndex > 0))
		{
			ShowStep(_stepIndex - 1);
		}

		var progressLeft = origin.X;
		if (!last)
		{
			ImGui.SetCursorScreenPos(new Vector2(origin.X, y));
			if (M3Widgets.Button("##tutorial_skip", skipLabel, M3ButtonStyle.Text,
				tooltip: "Close the tutorial. Reopen it any time from the About page."))
			{
				IsOpen = false;
			}

			progressLeft += M3Widgets.ButtonWidth(FontAwesomeIcon.None, skipLabel);
		}

		progressLeft += gap * 2f;
		var progressRight = right - nextWidth - gap - backWidth - (gap * 2f);
		if (progressRight - progressLeft >= 96f * scale)
		{
			DrawProgress(progressLeft, progressRight, y, M3Widgets.ButtonHeight);
		}

		ImGui.SetCursorScreenPos(new Vector2(origin.X, y + M3Widgets.ButtonHeight));
		ImGui.Dummy(new Vector2(width, 0f));
	}

	private void DrawProgress(float left, float right, float top, float height)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var label = $"Step {_stepIndex + 1} of {_steps.Length}";
		var width = MathF.Min(right - left, 320f * scale);
		left += ((right - left) - width) * 0.5f;

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var labelSize = ImGui.CalcTextSize(label);
		var barHeight = 4f * scale;
		var labelGap = 6f * scale;
		var blockTop = top + ((height - (labelSize.Y + labelGap + barHeight)) * 0.5f);

		ImGui.GetWindowDrawList().AddText(new Vector2(left + ((width - labelSize.X) * 0.5f), blockTop), M3.U32(s.OnSurfaceVariant, 0.9f), label);

		var fraction = M3Motion.Approach("##tutorial_progress", (_stepIndex + 1) / (float)_steps.Length, M3Motion.EmphasisedDuration);
		ImGui.SetCursorScreenPos(new Vector2(left, blockTop + labelSize.Y + labelGap));
		M3Widgets.LinearProgress(new Vector2(width, barHeight), fraction);
	}

	#endregion

	#region Steps

	private static void DrawWelcome()
	{
		var s = M3.Scheme;

		M3Widgets.SectionLabel("How it works");
		DrawTiles(
		[
			new(FontAwesomeIcon.Eye, "Reads the fight",
				"Your gauge, buffs, cooldowns and party HP, plus every enemy nearby, checked many times a second.", s.Primary),
			new(FontAwesomeIcon.Lightbulb, "Picks the next action",
				"Your job's rotation decides what comes next: GCDs and weaves, heals, mitigation, interrupts and more.", s.Tertiary),
			new(FontAwesomeIcon.MousePointer, "Uses it for you",
				"Switched on, RSR presses the action itself. Teaching mode lights it up on your hotbar too, so you can follow along.", s.Secondary),
		]);

		Gap(12f);
		_ = M3Widgets.Banner("##tutorial_warning", UiString.ConfigWindow_About_Warning.GetDescription(),
			M3Severity.Warning, FontAwesomeIcon.ExclamationTriangle);
		Gap(12f);

		DrawInfoCard("tutorial_welcome_tour", "In this tour", FontAwesomeIcon.Map,
			"Turning RSR on and off, and how Auto, Manual and Target Only differ.",
			"Finding your way around the settings window.",
			"Checking your rotation, and the handful of settings worth a look on day one.",
			"Overlays, teaching mode, and macros for your hotbar.");

		DrawNote("Settings shown in this tour can be changed right here, and apply straight away. Jump between steps with the list on the left, and close the tour whenever you like; the About page can reopen it.");
	}

	private static void DrawAppearance()
	{
		using (M3Card.Begin("tutorial_accent", "Accent colour", FontAwesomeIcon.Palette,
			subtitle: "Every other colour in the interface is derived from this one: surfaces, buttons, highlights and the overlays."))
		{
			DrawAccentPicker();
		}

		using (M3Card.Begin("tutorial_text", "Text and density", FontAwesomeIcon.Font))
		{
			DrawTextScaleRow();
			DrawElementScaleRow();
			DrawSwitch("tutorial_inline_descriptions", "Show setting descriptions inline",
				"Prints each setting's explanation under its name. Turn it off for a denser list; the explanation stays in the tooltip.",
				Service.Config.UiInlineDescriptions);
			DrawSwitch("tutorial_hints", "Show tips in the settings window",
				"A banner above each settings page that cycles through usage tips.",
				Service.Config.ShowHints);
		}

		if (M3Widgets.Button("##tutorial_appearance_reset", "Reset to defaults", M3ButtonStyle.Text, FontAwesomeIcon.UndoAlt,
			tooltip: "Puts the accent colour, text size and element size back to how they started."))
		{
			Service.Config.UiAccentColor = M3.DefaultSeed;
			Service.Config.UiTextScale = 1f;
			Service.Config.UiElementScale = 1f;
		}

		Gap(8f);
		DrawNote("All of these are on the UI page too, next to the colours for teaching mode and the hotbar highlights.");
	}

	private static void DrawModes()
	{
		var s = M3.Scheme;

		var (state, accent) = CurrentState();
		_ = M3Widgets.Pill("##tutorial_state", $"Right now: {state}", accent, tooltip: "Updates as the state changes.");
		Gap(12f);

		DrawTiles(
		[
			new(FontAwesomeIcon.Robot, "Auto",
				"Auto activates the autorotation algorithm and allows RSR to set targets for you based on the next calculated action. Run /rotation Auto again to step to the next targeting rule.", s.Primary),
			new(FontAwesomeIcon.HandPointer, "Manual",
				"Manual activate the autorotation algorithm but does not allow RSR to set targets and locks the hostile target list to only the selected target. Party healing is still possible on Manual mode as RSR uses soft targeting for that.", s.Tertiary),
			new(FontAwesomeIcon.Crosshairs, "Target Only",
				"RSR chooses targets as in Auto but never uses an action. Every button press is yours.", s.Secondary),
			new(FontAwesomeIcon.PowerOff, "Off",
				"Nothing is targeted or used. Turn RSR off whenever you are not using it.", s.OnSurfaceVariant),
		], 170f);

		Gap(12f);
		using (M3Card.Begin("tutorial_modes_switching", "Different ways to control RSR autorotation states", FontAwesomeIcon.ToggleOn))
		{
			DrawBullets(
				"From chat or a macro: /rotation Auto, /rotation Manual, /rotation TargetOnly and /rotation Off. /rsr works anywhere /rotation does.",
				"The state window: /rotation Control opens a small Auto / Manual / Off switch you can leave on screen.",
				"The server info bar: the RSR entry at the top of the screen shows the current state, and clicking it cycles through states.",
				"The Control window (UI page) puts the state, the rotation's status and the special commands on screen during combat.");

			Gap(4f);
			if (M3Widgets.Button("##tutorial_open_state_window", "Open the state window", M3ButtonStyle.Tonal, FontAwesomeIcon.ToggleOn))
			{
				RotationSolverPlugin.OpenStateControlWindow();
			}
		}

		DrawNote("RSR can switch itself on when a countdown starts, and off again after combat, when you die or when a duty ends. That is set up in the Auto step.");
	}

	private static void DrawSettingsTour()
	{
		var s = M3.Scheme;

		DrawTiles(
		[
			new(FontAwesomeIcon.Bars, "Page list",
				"Every settings page is listed down the left in a scrollable list.", s.Primary),
			new(FontAwesomeIcon.Search, "Search",
				"The search field above the page list finds any setting by name, and each result links to the page it lives on.", s.Primary),
			new(FontAwesomeIcon.Sync, "Rotation card",
				"Your current job (as a deep dungeon pixelart icon) and the loaded rotation. Click it to configure the rotation; right-click it to switch to another.", s.Tertiary),
			new(FontAwesomeIcon.Lightbulb, "Tips and warnings",
				"Banners across the top of each page flag problems, such as a conflicting plugin, and cycle through tips.", s.Tertiary),
			new(FontAwesomeIcon.Terminal, "Right-click for macros",
				"Right-click any setting, toggle or action to see the chat command that changes it, ready to copy.", s.Secondary),
			new(FontAwesomeIcon.Cube, "Copy diagnostics",
				"The chip at the bottom of the page list copies a report for support. It pulses red while a plugin known to crash RSR is enabled.", s.Secondary),
		]);

		Gap(12f);
		DrawInfoCard("tutorial_setting_shortcuts", "Shortcuts on any setting", FontAwesomeIcon.MousePointer,
			"Right-click a setting for its chat command, which you can run on the spot or copy into a macro. For example, right-clicking \"Auto turn off RSR when combat is over for more than\" on the Auto page gives /rotation Settings AutoOffAfterCombat True.",
			"Ctrl+click a slider to type an exact value instead of dragging, then press Enter. For example, Ctrl+click the slider under that same setting and type 45 to have RSR switch off 45 seconds after combat ends.");

		DrawPracticeSettings();

		_ = M3Widgets.Banner("##tutorial_reset_warning",
			"The skull button at the top right of the settings window resets every RSR setting. It asks first, but there is no undo, so back up from the Extra page before trying it.",
			M3Severity.Error, FontAwesomeIcon.Skull);
	}

	private static void DrawRotationStep()
	{
		DrawCurrentRotation();

		DrawInfoCard("tutorial_rotation_tuning", "Where to tune it", FontAwesomeIcon.SlidersH,
			"The rotation page, opened from the rotation card or the entry named after your job, has the rotation's description, its status and its own options, which vary from rotation to rotation.",
			"On Dancer, Sage and Astrologian the same page holds the dance partner, Kardia and card priority lists.",
			"Your choice of rotation is remembered for each job, separately for PvE and PvP.",
			"If one job feels off, look at its rotation options before changing anything that applies to every job.");
	}

	private static void DrawCurrentRotation()
	{
		const string pickerId = "##tutorial_rotation_picker";

		var s = M3.Scheme;
		var scale = M3.Scale;
		var rotation = DataCenter.CurrentRotation;

		if (rotation == null)
		{
			var nonCombat = Player.Job is Job.CRP or Job.BSM or Job.ARM or Job.GSM
				or Job.LTW or Job.WVR or Job.ALC or Job.CUL
				or Job.MIN or Job.FSH or Job.BTN;

			_ = nonCombat
				? M3Widgets.Banner("##tutorial_no_rotation", "You are on a crafting or gathering job. Rotation Solver only drives combat jobs, so switch to one to see its rotation here.",
					M3Severity.Info, FontAwesomeIcon.Hammer)
				: M3Widgets.Banner("##tutorial_no_rotation", UiString.ConfigWindow_NoRotation.GetDescription(),
					M3Severity.Error, FontAwesomeIcon.ExclamationTriangle);
			Gap(12f);
			return;
		}

		var attributes = rotation.GetAttributes();
		var name = attributes?.Name ?? rotation.Name;
		var accent = rotation.IsExtra() ? s.Tertiary : s.Primary;
		var subtitle = $"{Player.Job} - {(DataCenter.IsPvP ? "PvP" : "PvE")}";
		if (!string.IsNullOrEmpty(attributes?.GameVersion))
		{
			subtitle += $" - Patch {attributes.GameVersion}";
		}

		using var card = M3Card.Begin("tutorial_rotation_current", null, accent: accent, style: M3CardStyle.Elevated);

		const string switchLabel = "Switch rotation";
		var width = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X - M3Card.RightInset);
		var origin = ImGui.GetCursorScreenPos();
		var iconSize = 48f * scale;
		var buttonWidth = M3Widgets.ButtonWidth(FontAwesomeIcon.Sync, switchLabel);
		var textX = origin.X + iconSize + (14f * scale);
		var textWidth = MathF.Max(24f * scale, origin.X + width - buttonWidth - (12f * scale) - textX);

		float titleHeight;
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			titleHeight = ImGui.GetTextLineHeight();
		}

		var subtitleHeight = ImGui.GetTextLineHeight();
		var textHeight = titleHeight + (2f * scale) + subtitleHeight;
		var height = MathF.Max(MathF.Max(iconSize, textHeight), M3Widgets.ButtonHeight);
		var drawList = ImGui.GetWindowDrawList();

		var iconMin = new Vector2(origin.X, origin.Y + ((height - iconSize) * 0.5f));
		var iconMax = iconMin + new Vector2(iconSize, iconSize);
		if (!M3ActionIcon.Image(drawList, IconSet.GetJobPixelArtIcon(), iconMin, iconMax, M3.ShapeSmall))
		{
			M3ActionIcon.EmptySlot(drawList, iconMin, iconMax, M3.ShapeSmall);
		}

		var textTop = origin.Y + ((height - textHeight) * 0.5f);
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			drawList.AddText(new Vector2(textX, textTop), M3.U32(accent), M3Navigation.Truncate(name, textWidth));
		}

		drawList.AddText(new Vector2(textX, textTop + titleHeight + (2f * scale)), M3.U32(s.OnSurfaceVariant, 0.9f),
			M3Navigation.Truncate(subtitle, textWidth));

		ImGui.SetCursorScreenPos(new Vector2(origin.X + width - buttonWidth, origin.Y + ((height - M3Widgets.ButtonHeight) * 0.5f)));
		if (M3Widgets.Button("##tutorial_switch_rotation", switchLabel, M3ButtonStyle.Tonal, FontAwesomeIcon.Sync,
			tooltip: "Pick another rotation for this job."))
		{
			ImGui.OpenPopup(pickerId);
		}

		MainWindow.DrawRotationPicker(pickerId, rotation);

		ImGui.SetCursorScreenPos(new Vector2(origin.X, origin.Y + height));
		ImGui.Dummy(new Vector2(width, M3.Space1));

		var description = FirstParagraph(rotation.Description ?? attributes?.Description);
		if (description.Length > 0)
		{
			using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(s.OnSurfaceVariant, 0.95f));
			ImGui.TextWrapped(description);
		}
	}

	private static void DrawActionsStep()
	{
		var s = M3.Scheme;

		DrawTiles(
		[
			new(FontAwesomeIcon.MousePointer, "Pick an action",
				"Click any icon to open its settings alongside. The small tick or cross on each icon shows whether it is enabled.", s.Primary),
			new(FontAwesomeIcon.PowerOff, "Enable or disable",
				"RSR never uses a disabled action. You can still press it yourself.", s.Tertiary),
			new(FontAwesomeIcon.SlidersH, "Tune when it fires",
				"Depending on the action: a time-to-kill threshold, an HP threshold for heals, how many targets an AoE needs, and whether it shows in the cooldown window.", s.Secondary),
		]);

		Gap(12f);
		DrawInfoCard("tutorial_actions_more", "Also worth knowing", FontAwesomeIcon.InfoCircle,
			"Intercept, on by default in PvE: press an action yourself and RSR queues it, then uses it at the next opportunity instead of fighting you for the GCD. It is set up on the Auto page.",
			"Right-click an action for the command that enables or disables it, so a macro can toggle it.",
			"The list follows your current job and rotation.");
	}

	private static void DrawAutoStep()
	{
		DrawAoECard();

		using (M3Card.Begin("tutorial_auto_switch", "Auto switch", FontAwesomeIcon.PowerOff,
			subtitle: "When RSR switches itself on and off."))
		{
			DrawSwitch("tutorial_start_on_countdown", "Turn on Auto when a countdown starts",
				"Switches on as soon as a pull timer begins, so the rotation can time its pre-pull actions.",
				Service.Config.StartOnCountdown);
			DrawSwitch("tutorial_off_after_combat", "Turn off after combat",
				$"Switches off once you have been out of combat for {Service.Config.AutoOffAfterCombatTime:0} seconds. The delay is on the Auto page.",
				Service.Config.AutoOffAfterCombat);
			DrawSwitch("tutorial_off_when_dead", "Turn off when you die",
				"Stops RSR the moment you are knocked out.",
				Service.Config.AutoOffWhenDead);
			DrawSwitch("tutorial_off_when_duty_completed", "Turn off when a duty is completed",
				"Stops RSR as the duty's completion screen comes up.",
				Service.Config.AutoOffWhenDutyCompleted);
		}

		DrawInfoCard("tutorial_auto_more", "Also on the Auto page", FontAwesomeIcon.InfoCircle,
			"Action usage: allow or block abilities, interrupts, tinctures and True North, and set up intercept.",
			"Healing: the HP thresholds for single-target and AoE heals, and whether tanks and DPS use their own heals.",
			"Raising: who gets raised, and whether Swiftcast is spent on it.");
	}

	private static void DrawAoECard()
	{
		ReadOnlySpan<M3Segment> segments =
		[
			new("Off", FontAwesomeIcon.Crosshairs, "Single-target actions only."),
			new("Cleave", FontAwesomeIcon.CodeBranch, "AoE only where one target is enough, such as cleaves."),
			new("Full", FontAwesomeIcon.Bullseye, "Every AoE action."),
		];

		using var card = M3Card.Begin("tutorial_auto_aoe", "AoE usage", FontAwesomeIcon.Bullseye,
			subtitle: "How freely RSR uses area attacks. A macro can change it too, for example /rotation Settings AoEType Cleave.");

		var current = Service.Config.AoEType switch
		{
			ConfigTypes.AoEType.Off => 0,
			ConfigTypes.AoEType.Cleave => 1,
			_ => 2,
		};

		var available = MathF.Max(64f * M3.Scale, ImGui.GetContentRegionAvail().X - M3Card.RightInset);
		var width = MathF.Min(available, MathF.Max(M3Widgets.SegmentedWidth(segments), 360f * M3.Scale));
		var picked = M3Widgets.SegmentedButtons("##tutorial_aoe", segments, current, width);
		if (picked >= 0)
		{
			current = picked;
			Service.Config.AoEType = picked switch
			{
				0 => ConfigTypes.AoEType.Off,
				1 => ConfigTypes.AoEType.Cleave,
				_ => ConfigTypes.AoEType.Full,
			};
		}

		Gap(4f);
		using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(M3.Scheme.OnSurfaceVariant, 0.95f));
		ImGui.TextWrapped(current switch
		{
			0 => "Off: single-target actions only. The safest choice near enemies you must not pull.",
			1 => "Cleave: AoE actions are used only when they need no more than your one target, such as cleaves. Nothing that relies on hitting a pack.",
			_ => "Full: every AoE action, whenever enough enemies are in range. The usual choice for dungeons and trash packs.",
		});
	}

	private static void DrawTargetStep()
	{
		using (M3Card.Begin("tutorial_targeting_rules", "Hostile targeting", FontAwesomeIcon.Crosshairs,
			subtitle: "In Auto, RSR picks targets by the highlighted rule, and each /rotation Auto moves along the list. Click a rule to make it the active one."))
		{
			DrawTargetingRules();
		}

		DrawInfoCard("tutorial_target_more", "Also on the Target page", FontAwesomeIcon.InfoCircle,
			"How far RSR looks for enemies, and whether it engages ones that have not attacked you yet.",
			"How it treats striking dummies, FATE mobs, quest targets and target markers.",
			"Reorder the hostile targeting list, or add and remove rules.",
			"If RSR pulls things you did not mean to, tighten the engage settings here before touching the rotation.");
	}

	private static void DrawTargetingRules()
	{
		var s = M3.Scheme;
		var types = Service.Config.TargetingTypes;
		if (types.Count == 0)
		{
			using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(s.OnSurfaceVariant, 0.95f));
			ImGui.TextWrapped("The list is empty. RSR fills in its defaults the first time it needs a rule.");
			return;
		}

		var active = Service.Config.TargetingIndex % types.Count;
		var right = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X - M3Card.RightInset;
		for (var i = 0; i < types.Count; i++)
		{
			var label = $"{i + 1}. {types[i]}";
			if (i > 0)
			{
				FlowSameLine(M3Widgets.PillSize(label).X, M3.Space2, right);
			}

			var isActive = i == active;
			if (M3Widgets.Pill($"##tutorial_targeting_{i}", label, isActive ? s.Primary : s.OnSurfaceVariant,
				tooltip: isActive ? "The active rule." : "Make this the active rule.", interactive: true) && !isActive)
			{
				RSCommands.SetTargetingIndex(i);
			}
		}
	}

	private static void DrawOverlaysStep()
	{
		using (M3Card.Begin("tutorial_overlay_windows", "Windows", FontAwesomeIcon.WindowRestore,
			subtitle: "Each appears as soon as it is switched on, once a rotation is loaded."))
		{
			DrawSwitch("tutorial_control_window", "Control window",
				"The autorotation state, the rotation's live status and one-click special commands.",
				Service.Config.ShowControlWindow);
			DrawSwitch("tutorial_next_action_window", "Next action",
				"The action RSR will use next, with the GCD as a progress bar. It can show the hotbar keybind as well.",
				Service.Config.ShowNextActionWindow);
			DrawSwitch("tutorial_timeline_window", "Action timeline",
				"A scrolling record of what was used, and when: GCDs with their casts, oGCDs with their animation lock.",
				Service.Config.ShowActionTimelineWindow);
			DrawSwitch("tutorial_windows_hostile_only", "Only in duties or near enemies",
				"Hides all of the above while there is nothing to fight.",
				Service.Config.OnlyShowWithHostileOrInDuty);
		}

		using (M3Card.Begin("tutorial_overlay_teaching", "Teaching mode", FontAwesomeIcon.GraduationCap))
		{
			DrawSwitch("tutorial_teaching_mode", "Highlight the next action on your hotbars",
				"Lights up the button RSR wants next. Pair it with Target Only mode, where RSR picks targets and you press the buttons, to learn a rotation hands-on.",
				Service.Config.TeachingMode);
		}

		DrawNote("Sizes, colours and locks for all of these are on the UI page, along with the server info bar entry and toast notifications.");
	}

	private static void DrawListsStep()
	{
		DrawInfoCard("tutorial_lists", "List", FontAwesomeIcon.ListUl,
			"Statuses: which debuffs to cleanse, which buffs make an enemy invincible or a priority, when not to start a cast, and more.",
			"Actions: known raidwides, tankbusters and knockbacks that RSR prepares mitigation for.",
			"Territories: settings for particular areas.",
			"The lists come curated and are updated with the plugin. Add your own entries with the + buttons.");

		DrawInfoCard("tutorial_duty", "Duty", FontAwesomeIcon.Shield,
			"One section per kind of content, from Ultimate and Savage to deep dungeons, PvP and the Masked Carnivale, each with fight-specific switches. Most can stay as they are.",
			"In Occult Crescent, Variant dungeons, Bozja and a few other duties, a page for that content's own actions appears in the page list.",
			"The AutoDuty page has the settings and status for running RSR alongside AutoDuty.");
	}

	private static void DrawAdvancedStep()
	{
		DrawInfoCard("tutorial_basic", "Basic", FontAwesomeIcon.Stopwatch,
			"Action Ahead: how early in the GCD RSR queues the next action. The default suits almost everyone.",
			"Minimum updating time: how often RSR re-evaluates the fight. Raise it a little if you see frame drops; lower is more responsive.",
			"Other shared settings, such as making /rotation Auto a simple on/off toggle.");

		DrawInfoCard("tutorial_extra", "Extra", FontAwesomeIcon.PuzzlePiece,
			"Events: run your own macros when a duty starts or ends, or when a particular action is used.",
			"Internal: back up and restore your RSR settings.",
			"Other: animation-lock and cooldown-delay tweaks, for players not already using Boss Mod Reborn for them.");

		DrawInfoCard("tutorial_debug", "Debug", FontAwesomeIcon.Bug,
			"Live data for rotation writers and bug reports. You will not need it day to day.");
	}

	private static void DrawMacrosStep()
	{
		for (var i = 0; i < StarterMacros.Length; i++)
		{
			DrawMacro(i, StarterMacros[i]);
		}

		M3Widgets.SectionLabel("Handy commands");
		for (var i = 0; i < Commands.Length; i++)
		{
			var command = Commands[i];
			if (M3SettingRow.NavigationRow($"##tutorial_command_{i}", command.Command, command.Description,
				FontAwesomeIcon.Terminal, FontAwesomeIcon.Copy))
			{
				CopyToClipboard(command.Command, $"Copied {command.Command}");
			}
		}

		Gap(8f);
		DrawNote("To make a macro, open User Macros (/macros), paste into an empty slot and drag its icon to a hotbar. In the settings window, right-click anything to copy the command that changes it.");
	}

	private static void DrawFinishStep()
	{
		_ = M3Widgets.Banner("##tutorial_try_it",
			"Try it on a striking dummy: target one, run /rotation Manual and watch. Run /rotation Off when you are done.",
			M3Severity.Success, FontAwesomeIcon.Dumbbell);

		Gap(12f);
		M3Widgets.SectionLabel("Where next");

		if (M3SettingRow.NavigationRow("##tutorial_finish_settings", "Open the settings",
			"Everything in this tour, and the rest.", FontAwesomeIcon.Cog, FontAwesomeIcon.ChevronRight))
		{
			RotationSolverPlugin.ShowConfigWindow(MainWindowTab.Main);
		}

		if (M3SettingRow.NavigationRow("##tutorial_finish_state", "Open the state window",
			"A small Auto / Manual / Off switch to keep on screen.", FontAwesomeIcon.ToggleOn, FontAwesomeIcon.ChevronRight))
		{
			RotationSolverPlugin.OpenStateControlWindow();
		}

		if (M3SettingRow.NavigationRow("##tutorial_finish_changelog", "Read what's new",
			"The update notes for this version.", FontAwesomeIcon.Newspaper, FontAwesomeIcon.ChevronRight))
		{
			RotationSolverPlugin.OpenChangelog();
		}

		if (M3SettingRow.NavigationRow("##tutorial_finish_discord", "Get help on Discord",
			"Questions, bug reports and rotation feedback.", FontAwesomeIcon.Comments, FontAwesomeIcon.ExternalLinkAlt))
		{
			OpenLink(DiscordUrl);
		}

		Gap(8f);
		DrawNote("Reopen this tour any time from the About page. Press Finish to close it.");
	}

	#endregion

	#region Building blocks

	private static void Gap(float dp)
	{
		ImGui.Dummy(new Vector2(0f, dp * M3.Scale));
	}

	private static void FlowSameLine(float width, float gap, float right)
	{
		if (ImGui.GetItemRectMax().X + gap + width <= right)
		{
			ImGui.SameLine(0f, gap);
		}
	}

	private static void DrawTiles(ReadOnlySpan<Tile> tiles, float minWidth = 200f)
	{
		if (tiles.IsEmpty)
		{
			return;
		}

		var scale = M3.Scale;
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var gap = M3.Space2;

		var columns = Math.Clamp((int)((width + gap) / ((minWidth * scale) + gap)), 1, tiles.Length);
		var rows = (tiles.Length + columns - 1) / columns;
		columns = (tiles.Length + rows - 1) / rows;

		var tileWidth = (width - (gap * (columns - 1))) / columns;
		var padding = 14f * scale;
		var textWidth = MathF.Max(16f * scale, tileWidth - (padding * 2f));
		var origin = ImGui.GetCursorScreenPos();
		var y = origin.Y;

		for (var row = 0; row < rows; row++)
		{
			var first = row * columns;
			var count = Math.Min(columns, tiles.Length - first);

			var height = 0f;
			for (var c = 0; c < count; c++)
			{
				height = MathF.Max(height, TileHeight(tiles[first + c], textWidth, padding));
			}

			for (var c = 0; c < count; c++)
			{
				DrawTile(tiles[first + c], new Vector2(origin.X + ((tileWidth + gap) * c), y), new Vector2(tileWidth, height), padding, textWidth);
			}

			y += height + gap;
		}

		ImGui.SetCursorScreenPos(new Vector2(origin.X, y - gap));
		ImGui.Dummy(new Vector2(width, 0f));
	}

	private static float TileIconDiameter => 36f * M3.Scale;

	private static float TileHeight(in Tile tile, float textWidth, float padding)
	{
		var scale = M3.Scale;
		float titleHeight;
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			titleHeight = ImGui.CalcTextSize(tile.Title, false, textWidth).Y;
		}

		var bodyHeight = ImGui.CalcTextSize(tile.Text, false, textWidth).Y;
		return padding + TileIconDiameter + (10f * scale) + titleHeight + (4f * scale) + bodyHeight + padding;
	}

	private static void DrawTile(in Tile tile, Vector2 min, Vector2 size, float padding, float textWidth)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var max = min + size;
		var drawList = ImGui.GetWindowDrawList();

		drawList.AddRectFilled(min, max, M3.U32(s.SurfaceContainerLow, 0.92f), M3.ShapeMedium);
		drawList.AddRect(min, max, M3.U32(s.OutlineVariant, 0.55f), M3.ShapeMedium, ImDrawFlags.None, 1f * scale);

		var diameter = TileIconDiameter;
		var iconMin = min + new Vector2(padding, padding);
		var iconMax = iconMin + new Vector2(diameter, diameter);
		drawList.AddCircleFilled((iconMin + iconMax) * 0.5f, diameter * 0.5f, M3.U32(tile.Accent, 0.16f), 32);
		M3Draw.IconCentered(drawList, tile.Icon, iconMin, iconMax, tile.Accent);

		float titleBottom;
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			titleBottom = M3Draw.WrappedText(tile.Title, new Vector2(min.X + padding, iconMax.Y + (10f * scale)), textWidth, s.OnSurface);
		}

		_ = M3Draw.WrappedText(tile.Text, new Vector2(min.X + padding, titleBottom + (4f * scale)), textWidth, M3.Alpha(s.OnSurfaceVariant, 0.92f));
	}

	private static void DrawInfoCard(string id, string title, FontAwesomeIcon icon, params ReadOnlySpan<string> bullets)
	{
		using var card = M3Card.Begin(id, title, icon);
		DrawBullets(bullets);
	}

	private static void DrawBullets(params ReadOnlySpan<string> items)
	{
		using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 6f) * M3.Scale);
		foreach (var item in items)
		{
			DrawBullet(item);
		}
	}

	private static void DrawBullet(string text)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var indent = 22f * scale;
		var origin = ImGui.GetCursorScreenPos();

		ImGui.GetWindowDrawList().AddCircleFilled(
			new Vector2(origin.X + (9f * scale), origin.Y + (ImGui.GetTextLineHeight() * 0.5f)),
			2.5f * scale, M3.U32(s.Primary, 0.9f), 12);

		ImGui.Indent(indent);
		using (ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(s.OnSurface, 0.9f)))
		{
			ImGui.TextWrapped(text);
		}

		ImGui.Unindent(indent);
	}

	private static void DrawNote(string text)
	{
		_ = M3Widgets.Banner($"##tutorial_note_{text.GetHashCode()}", text, M3Severity.Info, FontAwesomeIcon.Lightbulb);
		Gap(8f);
	}

	private static void DrawSwitch(string id, string label, string supporting, ConditionBoolean setting)
	{
		var value = setting.Value;
		if (M3Widgets.RowSwitch($"{label}###{id}", ref value, supporting))
		{
			setting.Value = value;
		}
	}

	private static void DrawAccentPicker()
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var seed = Service.Config.UiAccentColor;
		var diameter = 28f * scale;
		var gap = 12f * scale;
		var right = ImGui.GetCursorScreenPos().X + ImGui.GetContentRegionAvail().X - M3Card.RightInset;
		var drawList = ImGui.GetWindowDrawList();
		var matched = false;

		ImGui.Dummy(new Vector2(0f, 4f * scale));
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (6f * scale));

		for (var i = 0; i < AccentPresets.Length; i++)
		{
			var preset = AccentPresets[i];
			var color = M3ColorMath.FromRgb(preset.Rgb);
			var selected = SameColor(seed, color);
			matched |= selected;

			if (i > 0)
			{
				FlowSameLine(diameter, gap, right);
			}

			if (ImGui.InvisibleButton($"##tutorial_accent_{i}", new Vector2(diameter, diameter)))
			{
				Service.Config.UiAccentColor = color;
			}

			var hovered = ImGui.IsItemHovered();
			var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
			if (hovered)
			{
				drawList.AddCircleFilled(center, (diameter * 0.5f) + (6f * scale), M3.U32(s.OnSurface, M3.StateHover), 32);
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
				ImGui.SetTooltip(preset.Name);
			}

			drawList.AddCircleFilled(center, diameter * 0.5f, M3.U32(color), 32);
			if (selected)
			{
				DrawSelectionRing(drawList, center, diameter);
				M3Draw.IconCentered(drawList, FontAwesomeIcon.Check, center - new Vector2(diameter * 0.5f), center + new Vector2(diameter * 0.5f), M3.ContentOn(color));
			}
		}

		FlowSameLine(diameter, gap * 2f, right);
		var custom = seed;
		if (M3Widgets.ColorSwatch("##tutorial_accent_custom", ref custom))
		{
			Service.Config.UiAccentColor = custom with { W = 1f };
		}

		if (ImGui.IsItemHovered())
		{
			ImGui.SetTooltip(matched ? "Pick any colour" : "Your own colour. Click to change it.");
		}

		if (!matched)
		{
			DrawSelectionRing(drawList, (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f, diameter);
		}

		ImGui.Dummy(new Vector2(0f, 4f * scale));
	}

	private static void DrawSelectionRing(ImDrawListPtr drawList, Vector2 center, float diameter)
	{
		var scale = M3.Scale;
		drawList.AddCircle(center, (diameter * 0.5f) + (3.5f * scale), M3.U32(M3.Scheme.OnSurface), 32, 2f * scale);
	}

	private static bool SameColor(Vector4 a, Vector4 b)
	{
		const float tolerance = 0.003f;
		return MathF.Abs(a.X - b.X) < tolerance && MathF.Abs(a.Y - b.Y) < tolerance && MathF.Abs(a.Z - b.Z) < tolerance;
	}

	private static void DrawTextScaleRow()
	{
		var value = Service.Config.UiTextScale;
		if (DrawPercentRow("##tutorial_text_scale", "Text size",
			"Scales the text in every Rotation Solver window, on top of Dalamud's own font settings.",
			ref value, 0.75f, 1.75f))
		{
			Service.Config.UiTextScale = MathF.Round(value, 2);
		}
	}

	private static void DrawElementScaleRow()
	{
		var value = Service.Config.UiElementScale;
		if (DrawPercentRow("##tutorial_element_scale", "Element size",
			"Scales the padding, spacing and controls in every Rotation Solver window. Turn it down for a more compact layout.",
			ref value, 0.75f, 1.75f))
		{
			Service.Config.UiElementScale = MathF.Round(value, 2);
		}
	}

	private static bool DrawPercentRow(string id, string label, string supporting, ref float value, float min, float max)
	{
		var trackWidth = 160f * M3.Scale;
		var controlSize = new Vector2(trackWidth + M3Widgets.SliderValueGutter($"{max * 100f:0}%"), M3Widgets.ButtonHeight);

		var row = M3SettingRow.Begin(label, supporting, controlSize);
		var changed = M3Widgets.Slider(id, ref value, min, max, $"{value * 100f:0}%", trackWidth);
		M3SettingRow.End(row);
		return changed;
	}

	private static void DrawPracticeSettings()
	{
		using var card = M3Card.Begin("tutorial_practice", "Try it here", FontAwesomeIcon.Flask,
			subtitle: "Practice settings. They behave like the real ones, but change nothing in RSR.");

		var toggle = M3SettingRow.Begin("Practice toggle", "Right-click this row to see its chat command.", M3Widgets.SwitchSize());
		_ = M3Widgets.Switch("##tutorial_practice_toggle", ref _practiceToggle);
		DrawPracticePopup(toggle, "PracticeToggle", PracticeToggleDefault.ToString(), () => _practiceToggle = PracticeToggleDefault);
		M3SettingRow.End(toggle);

		var trackWidth = 160f * M3.Scale;
		var slider = M3SettingRow.Begin("Practice slider", "Ctrl+click the slider, type a number and press Enter.",
			new Vector2(trackWidth + M3Widgets.SliderValueGutter("600.00 s"), M3Widgets.ButtonHeight));
		_ = M3Widgets.Slider("##tutorial_practice_slider", ref _practiceSlider, 0f, 600f, $"{_practiceSlider:F2} s", trackWidth);
		DrawPracticePopup(slider, "PracticeSlider", PracticeSliderDefault.ToString(), () => _practiceSlider = PracticeSliderDefault);
		M3SettingRow.End(slider);
	}

	private static void DrawPracticePopup(in M3RowInfo row, string name, string defaultValue, Action reset)
	{
		var key = $"tutorial_practice_popup_{name}";
		var command = $"{Service.COMMAND} {OtherCommandType.Settings} {name} {defaultValue}";
		void Copy() => CopyToClipboard(command, $"Copied {command} (a practice command, it does nothing in chat).");

		ImGuiHelper.DrawHotKeysPopup(key, string.Empty,
			("Reset to Default Value.", reset, ImGuiHelper.BackspaceHint),
			($"Execute \"{command}\"", reset, ["Alt"]),
			($"Copy \"{command}\"", Copy, ["Ctrl"]));

		ImGuiHelper.ExecuteHotKeysPopupAt(row.Hovered, key, string.Empty, string.Empty, false,
			(reset, [VirtualKey.BACK]),
			(reset, [VirtualKey.MENU]),
			(Copy, [VirtualKey.CONTROL]));
	}

	private static void DrawMacro(int index, StarterMacro macro)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		using var card = M3Card.Begin($"tutorial_macro_{index}", macro.Title, macro.Icon, style: M3CardStyle.Outlined, subtitle: macro.Description);

		const string copyLabel = "Copy";
		var width = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X - M3Card.RightInset);
		var copyWidth = M3Widgets.ButtonWidth(FontAwesomeIcon.Copy, copyLabel);
		var padding = new Vector2(12f, 10f) * scale;
		var lines = macro.Text.Split("\r\n");
		var lineHeight = ImGui.GetTextLineHeight();
		var lineGap = 2f * scale;
		var textHeight = (lines.Length * lineHeight) + ((lines.Length - 1) * lineGap);
		var height = MathF.Max(textHeight, M3Widgets.ButtonHeight) + (padding.Y * 2f);

		var origin = ImGui.GetCursorScreenPos();
		ImGui.Dummy(new Vector2(width, height));
		var max = origin + new Vector2(width, height);
		var drawList = ImGui.GetWindowDrawList();

		drawList.AddRectFilled(origin, max, M3.U32(s.SurfaceContainerHighest, 0.7f), M3.ShapeSmall);

		var textWidth = MathF.Max(16f * scale, width - (padding.X * 3f) - copyWidth);
		var y = origin.Y + ((height - textHeight) * 0.5f);
		foreach (var line in lines)
		{
			drawList.AddText(new Vector2(origin.X + padding.X, y), M3.U32(s.OnSurface, 0.95f), M3Navigation.Truncate(line, textWidth));
			y += lineHeight + lineGap;
		}

		ImGui.SetCursorScreenPos(new Vector2(max.X - padding.X - copyWidth, origin.Y + ((height - M3Widgets.ButtonHeight) * 0.5f)));
		if (M3Widgets.Button($"##tutorial_copy_macro_{index}", copyLabel, M3ButtonStyle.Tonal, FontAwesomeIcon.Copy))
		{
			CopyToClipboard(macro.Text, "Macro copied. Paste it into a new macro in User Macros (/macros).");
		}

		ImGui.SetCursorScreenPos(new Vector2(origin.X, max.Y));
		ImGui.Dummy(new Vector2(width, 0f));
	}

	private static (string Label, Vector4 Accent) CurrentState()
	{
		var s = M3.Scheme;
		if (!DataCenter.State)
		{
			return ("Off", s.OnSurfaceVariant);
		}

		if (DataCenter.IsManual)
		{
			return ("Manual", s.Tertiary);
		}

		if (DataCenter.IsTargetOnly)
		{
			return ($"Target Only, {DataCenter.TargetingType}", s.Secondary);
		}

		if (DataCenter.IsAutoDuty)
		{
			return ("AutoDuty", s.Primary);
		}

		if (DataCenter.IsHenched)
		{
			return ("Henched", s.Primary);
		}

		return DataCenter.IsPvPStateEnabled
			? ("PvP", s.Primary)
			: ($"Auto, {DataCenter.TargetingType}", s.Primary);
	}

	private static string FirstParagraph(string? text, int maxLength = 280)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return string.Empty;
		}

		text = text.Trim();
		var end = text.IndexOf('\n');
		if (end > 0)
		{
			text = text[..end].TrimEnd();
		}

		return text.Length <= maxLength
			? text
			: string.Concat(text.AsSpan(0, maxLength).TrimEnd(), "…");
	}

	private static void CopyToClipboard(string text, string confirmation)
	{
		try
		{
			ImGui.SetClipboardText(text);
			M3Snackbar.Show(confirmation);
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to copy to the clipboard: {ex.Message}");
		}
	}

	private static void OpenLink(string url)
	{
		try
		{
			Util.OpenLink(url);
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to open {url}: {ex.Message}");
		}
	}

	#endregion
}
