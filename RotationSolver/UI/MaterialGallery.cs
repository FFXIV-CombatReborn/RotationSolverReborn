using Dalamud.Interface.Utility.Raii;
using RotationSolver.UI.Material;

namespace RotationSolver.UI;

// Debug-tab showcase of the Material components. TODO: maybe split into a library for other Reborn plugins.
internal static class MaterialGallery
{
	private const int DefaultScaleIndex = 1;

	private static readonly M3Tab[] _pages =
	[
		new("Foundations", FontAwesomeIcon.Palette),
		new("Buttons", FontAwesomeIcon.HandPointer),
		new("Selection", FontAwesomeIcon.CheckSquare),
		new("Inputs", FontAwesomeIcon.Keyboard),
		new("Feedback", FontAwesomeIcon.Bell),
		new("Layout", FontAwesomeIcon.ThLarge),
		new("Navigation", FontAwesomeIcon.Compass),
	];

	private static readonly M3Segment[] _scaleSegments = [new("75%"), new("100%"), new("125%"), new("150%")];
	private static readonly float[] _scales = [0.75f, 1f, 1.25f, 1.5f];

	private static readonly M3ButtonStyle[] _buttonStyles =
		[M3ButtonStyle.Filled, M3ButtonStyle.Tonal, M3ButtonStyle.Outlined, M3ButtonStyle.Text, M3ButtonStyle.Danger];

	private static readonly FontAwesomeIcon[] _buttonIcons =
		[FontAwesomeIcon.Check, FontAwesomeIcon.Pen, FontAwesomeIcon.Copy, FontAwesomeIcon.Undo, FontAwesomeIcon.TrashAlt];

	private static readonly (string Name, Func<M3Scheme, Vector4> Pick)[] _roles =
	[
		("Primary", s => s.Primary),
		("On primary", s => s.OnPrimary),
		("Primary container", s => s.PrimaryContainer),
		("On primary container", s => s.OnPrimaryContainer),
		("Primary fixed dim", s => s.PrimaryFixedDim),
		("Inverse primary", s => s.InversePrimary),
		("Secondary", s => s.Secondary),
		("Secondary container", s => s.SecondaryContainer),
		("On secondary container", s => s.OnSecondaryContainer),
		("Tertiary", s => s.Tertiary),
		("Tertiary container", s => s.TertiaryContainer),
		("On tertiary container", s => s.OnTertiaryContainer),
		("Error", s => s.Error),
		("On error", s => s.OnError),
		("Error container", s => s.ErrorContainer),
		("On error container", s => s.OnErrorContainer),
		("Surface", s => s.Surface),
		("Container lowest", s => s.SurfaceContainerLowest),
		("Container low", s => s.SurfaceContainerLow),
		("Container", s => s.SurfaceContainer),
		("Container high", s => s.SurfaceContainerHigh),
		("Container highest", s => s.SurfaceContainerHighest),
		("On surface", s => s.OnSurface),
		("On surface variant", s => s.OnSurfaceVariant),
		("Inverse surface", s => s.InverseSurface),
		("Inverse on surface", s => s.InverseOnSurface),
		("Outline", s => s.Outline),
		("Outline variant", s => s.OutlineVariant),
		("Scrim", s => s.Scrim),
		("Warning", s => s.Warning),
		("Warning container", s => s.WarningContainer),
		("Success", s => s.Success),
		("Success container", s => s.SuccessContainer),
		("Info", s => s.Info),
	];

	private static readonly M3Segment[] _textSegments = [new("Day"), new("Week"), new("Month")];
	private static readonly M3Segment[] _iconSegments =
	[
		new("Fire", FontAwesomeIcon.Fire, "Fire aspect"),
		new("Ice", FontAwesomeIcon.Snowflake, "Ice aspect"),
		new("Wind", FontAwesomeIcon.Wind, "Wind aspect"),
	];

	private static readonly M3WindowAction[] _windowActions =
	[
		new("##gallery_share", FontAwesomeIcon.ShareAlt, "Share"),
		new("##gallery_help", FontAwesomeIcon.QuestionCircle, "Help"),
	];

	private static readonly string[] _radioOptions = ["Lowest HP", "Highest HP", "Nearest"];
	private static readonly string[] _sizeOptions = ["Small", "Medium", "Large"];
	private static readonly string[] _filterNames = ["Tanks", "Healers", "Melee", "Ranged"];
	private static readonly string[] _menuOptions = ["Any target", "Hostile only", "Party only"];
	private static readonly string[] _comboItems = ["Big", "Small", "Moving", "Stationary", "A much longer option that gets truncated"];

	private static readonly M3Tab[] _demoIconTabs =
	[
		new("Rotation", FontAwesomeIcon.Sync),
		new("Actions", FontAwesomeIcon.Bolt),
		new("Targets", FontAwesomeIcon.Crosshairs),
	];

	private static readonly M3Tab[] _demoTextTabs =
	[
		new("Overview"),
		new("Alerts", Badge: "4", Tooltip: "Tabs can carry a badge"),
		new("History"),
	];

	private static readonly M3Tab[] _demoSecondaryTabs =
	[
		new("PvE", FontAwesomeIcon.Dragon),
		new("PvP", FontAwesomeIcon.FistRaised),
	];

	private static readonly float[] _actionIconSizes = [24f, 32f, 48f, 64f];

	private static int _page;
	private static int _scaleIndex = DefaultScaleIndex;

	private static bool _motionOn;
	private static readonly bool[] _toggles = [false, true, false, true, false];
	private static bool _windowFolded;
	private static int _segmentText;
	private static int _segmentIcon;
	private static int _segmentWide = 1;

	private static bool _switchOn = true;
	private static bool _switchOff;
	private static bool _rowSwitch = true;
	private static bool _checkA = true;
	private static bool _checkB;
	private static int _radioIndex;
	private static int _radioHorizontal = 1;
	private static bool _standaloneRadio;
	private static readonly bool[] _filters = [true, false, true, false];
	private static int _menuIndex;
	private static readonly List<string> _tags = ["Bleeding", "Doom", "Vulnerability Up"];
	private static int _nextTag = 1;
	private static float _slider = 0.4f;
	private static int _sliderInt = 3;
	private static float _rowFloat = 1.5f;
	private static int _rowInt = 12;
	private static Vector4 _swatch = new(0.2f, 0.6f, 0.9f, 1f);

	private static string _search = string.Empty;
	private static string _name = string.Empty;
	private static string _target = "Striking Dummy";
	private static string _statusId = "abc";
	private static int _comboIndex = 1;
	private static int _comboEmpty = -1;

	private static float _progress = 0.35f;
	private static bool _dialogSwitch;

	private static bool _expanded = true;
	private static bool _parentSetting = true;
	private static bool _childSetting;
	private static int _childValue = 40;
	private static bool _rowWithIcon = true;

	private static string _navSelected = "home";
	private static int _tabIcons;
	private static int _tabText;
	private static int _tabSecondary;

	public static void Draw()
	{
		DrawControls();

		using var scale = M3.PushWindowScale(_scales[_scaleIndex]);
		var previewing = _scaleIndex != DefaultScaleIndex;
		if (previewing)
		{
			ImGui.PushFont(M3.Body);
		}

		using var font = new M3.FontScope(previewing);

		var picked = M3Navigation.Tabs("##gallery_pages", _pages, _page, width: RowWidth);
		if (picked >= 0)
		{
			_page = picked;
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));

		switch (_page)
		{
			case 0:
				DrawFoundations();
				break;
			case 1:
				DrawButtons();
				break;
			case 2:
				DrawSelection();
				break;
			case 3:
				DrawInputs();
				break;
			case 4:
				DrawFeedback();
				break;
			case 5:
				DrawLayout();
				break;
			default:
				DrawNavigation();
				break;
		}
	}

	private static void DrawControls()
	{
		var scale = M3.Scale;

		using var card = M3Card.Begin("gallery_controls", "Gallery controls", FontAwesomeIcon.SlidersH, style: M3CardStyle.Outlined,
			subtitle: "Headings in this colour mark components added ahead of any caller in the plugin.");

		var segmentsWidth = M3Widgets.SegmentedWidth(_scaleSegments);
		var scaleRow = M3SettingRow.Begin("Preview scale", "Scales every metric and font below, without touching the text size setting.",
			new Vector2(segmentsWidth, M3Widgets.SegmentedHeight));
		var chosen = M3Widgets.SegmentedButtons("##gallery_scale", _scaleSegments, _scaleIndex, segmentsWidth);
		if (chosen >= 0)
		{
			_scaleIndex = chosen;
		}

		M3SettingRow.End(scaleRow);

		var swatch = 28f * scale;
		var resetWidth = M3Widgets.ButtonWidth(FontAwesomeIcon.None, "Reset");
		var seedRow = M3SettingRow.Begin("Accent seed", "The saved interface accent colour. The whole scheme is derived from it.",
			new Vector2(swatch + M3.Space2 + resetWidth, M3Widgets.ButtonHeight));
		ImGui.SetCursorScreenPos(seedRow.ControlPosition + new Vector2(0f, (M3Widgets.ButtonHeight - swatch) * 0.5f));
		var seed = Service.Config.UiAccentColor;
		if (M3Widgets.ColorSwatch("##gallery_seed", ref seed))
		{
			Service.Config.UiAccentColor = seed;
		}

		ImGui.SetCursorScreenPos(seedRow.ControlPosition + new Vector2(swatch + M3.Space2, 0f));
		if (M3Widgets.Button("##gallery_seed_reset", "Reset", M3ButtonStyle.Text))
		{
			Service.Config.UiAccentColor = M3.DefaultSeed;
		}

		M3SettingRow.End(seedRow);

		if (M3Widgets.Button("##gallery_reset_motion", "Reset animation state", M3ButtonStyle.Tonal, FontAwesomeIcon.Undo,
			tooltip: "Clears every eased value and cached card height, as closing the window does."))
		{
			M3Motion.Reset();
			M3CardHost.Reset();
		}

		ImGui.SameLine(0f, M3.Space2);
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((M3Widgets.ButtonHeight - M3Widgets.PillSize("New component", FontAwesomeIcon.Star).Y) * 0.5f));
		_ = M3Widgets.Pill("##gallery_new_legend", "New component", M3.Scheme.Tertiary, FontAwesomeIcon.Star);
	}

	#region Foundations

	private static void DrawFoundations()
	{
		Heading("Colour roles");
		Caption("Click a swatch to copy its hex value.");
		DrawColorRoles();

		Heading("Typography");
		DrawTypography();

		Heading("Shape");
		DrawShapes();

		Heading("Elevation");
		DrawElevation();

		Heading("State layers");
		DrawStateLayers();

		Heading("Severity");
		DrawSeverities();

		Heading("Motion");
		DrawMotion();

		Heading("Drawing primitives");
		DrawPrimitives();
	}

	private static void DrawColorRoles()
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = new Vector2(150f, 48f) * scale;
		var flow = new Flow(M3.Space1);

		foreach (var (name, pick) in _roles)
		{
			var color = pick(s);
			var hex = Hex(color);

			flow.Next(size.X);
			if (ImGui.InvisibleButton($"##role_{name}", size))
			{
				ImGui.SetClipboardText(hex);
				M3Snackbar.Show($"Copied {name} ({hex})");
			}

			var hovered = ImGui.IsItemHovered();
			var min = ImGui.GetItemRectMin();
			var max = ImGui.GetItemRectMax();
			var drawList = ImGui.GetWindowDrawList();
			var content = M3.ContentOn(color);

			drawList.AddRectFilled(min, max, M3.U32(color), M3.ShapeSmall);
			drawList.AddRect(min, max, M3.U32(s.OutlineVariant, hovered ? 1f : 0.5f), M3.ShapeSmall, ImDrawFlags.None, 1f * scale);

			using (ImRaii.PushFont(M3.LabelSmall))
			{
				var line = ImGui.GetTextLineHeight();
				drawList.AddText(min + (new Vector2(8f, 6f) * scale), M3.U32(content), name);
				drawList.AddText(new Vector2(min.X + (8f * scale), max.Y - line - (6f * scale)), M3.U32(content, 0.75f), hex);
			}

			if (hovered)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}
		}
	}

	private static void DrawTypography()
	{
		(string Name, ImFontPtr Font)[] styles =
		[
			("Headline small", M3.HeadlineSmall),
			("Title large", M3.TitleLarge),
			("Title medium", M3.TitleMedium),
			("Body", ImGui.GetFont()),
			("Label small", M3.LabelSmall),
		];

		foreach (var (name, font) in styles)
		{
			float size;
			using (ImRaii.PushFont(font))
			{
				ImGui.TextUnformatted($"{name} — The quick brown fox");
				size = ImGui.GetFontSize();
			}

			ImGui.SameLine(0f, M3.Space2);
			Caption($"{size:0}px");
		}
	}

	private static void DrawShapes()
	{
		(string Name, float Radius)[] shapes =
		[
			("Extra small", M3.ShapeExtraSmall),
			("Small", M3.ShapeSmall),
			("Medium", M3.ShapeMedium),
			("Large", M3.ShapeLarge),
			("Extra large", M3.ShapeExtraLarge),
			("Full", M3.ShapeFull),
		];

		var s = M3.Scheme;
		var box = new Vector2(96f, 56f) * M3.Scale;
		var flow = new Flow(M3.Space3);
		foreach (var (name, radius) in shapes)
		{
			flow.Next(box.X);
			using (ImRaii.Group())
			{
				ImGui.Dummy(box);
				ImGui.GetWindowDrawList().AddRectFilled(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), M3.U32(s.PrimaryContainer), radius);
				Caption(name);
			}
		}
	}

	private static void DrawElevation()
	{
		var s = M3.Scheme;
		var box = new Vector2(96f, 56f) * M3.Scale;
		var flow = new Flow(M3.Space3 * 2f);
		for (var level = 0; level <= 3; level++)
		{
			flow.Next(box.X);
			using (ImRaii.Group())
			{
				ImGui.Dummy(box);
				var min = ImGui.GetItemRectMin();
				var max = ImGui.GetItemRectMax();
				var drawList = ImGui.GetWindowDrawList();
				M3Draw.Elevation(drawList, min, max, M3.ShapeMedium, level);
				drawList.AddRectFilled(min, max, M3.U32(s.SurfaceContainerLow), M3.ShapeMedium);
				Caption($"Level {level}");
			}
		}
	}

	private static void DrawStateLayers()
	{
		var s = M3.Scheme;
		var box = new Vector2(110f, 44f) * M3.Scale;
		(string Name, Vector4 Container, Vector4 Content)[] states =
		[
			("Enabled", s.SecondaryContainer, s.OnSecondaryContainer),
			("Hovered", M3.StateLayer(s.SecondaryContainer, s.OnSecondaryContainer, true, false), s.OnSecondaryContainer),
			("Pressed", M3.StateLayer(s.SecondaryContainer, s.OnSecondaryContainer, true, true), s.OnSecondaryContainer),
			("Disabled", M3.Alpha(s.OnSurface, M3.DisabledContainer), M3.Alpha(s.OnSurface, M3.DisabledContent)),
		];

		var flow = new Flow(M3.Space2);
		foreach (var (name, container, content) in states)
		{
			flow.Next(box.X);
			ImGui.Dummy(box);
			var min = ImGui.GetItemRectMin();
			var max = ImGui.GetItemRectMax();
			var drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilled(min, max, M3.U32(container), box.Y * 0.5f);
			var textSize = ImGui.CalcTextSize(name);
			drawList.AddText(min + ((box - textSize) * 0.5f), M3.U32(content), name);
		}
	}

	private static void DrawSeverities()
	{
		var box = new Vector2(120f, 44f) * M3.Scale;
		var flow = new Flow(M3.Space2);
		foreach (var severity in Enum.GetValues<M3Severity>())
		{
			flow.Next(box.X);
			ImGui.Dummy(box);
			var min = ImGui.GetItemRectMin();
			var max = ImGui.GetItemRectMax();
			var drawList = ImGui.GetWindowDrawList();
			var accent = M3.Severity(severity);
			drawList.AddRectFilled(min, max, M3.U32(M3.SeverityContainer(severity), 0.8f), M3.ShapeSmall);

			var name = severity.ToString();
			var textSize = ImGui.CalcTextSize(name);
			var radius = 4f * M3.Scale;
			var left = min.X + ((box.X - textSize.X - (radius * 2f) - M3.Space2) * 0.5f);
			drawList.AddCircleFilled(new Vector2(left + radius, min.Y + (box.Y * 0.5f)), radius, M3.U32(accent), 12);
			drawList.AddText(new Vector2(left + (radius * 2f) + M3.Space2, min.Y + ((box.Y - textSize.Y) * 0.5f)), M3.U32(accent), name);
		}
	}

	private static void DrawMotion()
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		_ = M3Widgets.Switch("##gallery_motion", ref _motionOn);
		Beside("Flip to send both dots across.");

		(string Name, float Duration)[] tracks =
		[
			($"Fast - {M3Motion.FastDuration * 1000f:0}ms", M3Motion.FastDuration),
			($"Emphasised - {M3Motion.EmphasisedDuration * 1000f:0}ms", M3Motion.EmphasisedDuration),
		];

		var width = MathF.Min(ImGui.GetContentRegionAvail().X, 320f * scale);
		foreach (var (name, duration) in tracks)
		{
			Caption(name);
			ImGui.Dummy(new Vector2(width, 20f * scale));
			var min = ImGui.GetItemRectMin();
			var max = ImGui.GetItemRectMax();
			var drawList = ImGui.GetWindowDrawList();
			var radius = 8f * scale;
			var y = (min.Y + max.Y) * 0.5f;
			var t = M3Motion.Approach($"gallery_motion_{duration}", _motionOn ? 1f : 0f, duration);

			drawList.AddLine(new Vector2(min.X + radius, y), new Vector2(max.X - radius, y), M3.U32(s.SurfaceContainerHighest), 4f * scale);
			drawList.AddCircleFilled(new Vector2(float.Lerp(min.X + radius, max.X - radius, t), y), radius, M3.U32(s.Primary), 24);
		}
	}

	private static void DrawPrimitives()
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var box = new Vector2(110f, 72f) * scale;
		var flow = new Flow(M3.Space3);

		flow.Next(box.X);
		using (ImRaii.Group())
		{
			ImGui.Dummy(box);
			M3Draw.Container(ImGui.GetWindowDrawList(), ImGui.GetItemRectMin(), ImGui.GetItemRectMax(),
				s.SurfaceContainerHigh, M3.ShapeMedium, s.Outline, 1f);
			Caption("Container");
		}

		flow.Next(box.X);
		using (ImRaii.Group())
		{
			ImGui.Dummy(box);
			M3Draw.VerticalGradient(ImGui.GetWindowDrawList(), ImGui.GetItemRectMin(), ImGui.GetItemRectMax(),
				s.PrimaryContainer, M3.Alpha(s.Surface, 0f));
			Caption("Vertical gradient");
		}

		flow.Next(box.X);
		using (ImRaii.Group())
		{
			ImGui.Dummy(box);
			var min = ImGui.GetItemRectMin();
			var max = ImGui.GetItemRectMax();
			var drawList = ImGui.GetWindowDrawList();
			drawList.AddRectFilled(min, max, M3.U32(s.SurfaceContainerLow), M3.ShapeMedium);
			M3Draw.AccentRail(drawList, min.X + (2f * scale), 3f * scale, min.Y + (2f * scale), max.Y - (2f * scale),
				s.Primary, M3Draw.ResolveFade(80f, 0.35f, box.Y));
			Caption("Accent rail");
		}

		flow.Next(box.X);
		using (ImRaii.Group())
		{
			ImGui.Dummy(box);
			var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
			var drawList = ImGui.GetWindowDrawList();
			M3Draw.Arc(drawList, center, 26f * scale, 0f, 1f, s.SurfaceContainerHighest, 4f * scale);
			M3Draw.Arc(drawList, center, 26f * scale, 0f, 0.7f, s.Primary, 4f * scale);
			M3Draw.Arc(drawList, center, 16f * scale, 0.25f, 0.6f, s.Tertiary, 3f * scale);
			Caption("Arc");
		}

		flow.Next(box.X);
		using (ImRaii.Group())
		{
			ImGui.Dummy(box);
			M3Draw.IconCentered(ImGui.GetWindowDrawList(), FontAwesomeIcon.Magic, ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), s.Primary);
			Caption("Icon centred");
		}
	}

	#endregion

	#region Buttons

	private static void DrawButtons()
	{
		Heading("Common buttons");
		var flow = new Flow(M3.Space2);
		foreach (var style in _buttonStyles)
		{
			var label = style.ToString();
			flow.Next(M3Widgets.ButtonWidth(FontAwesomeIcon.None, label));
			if (M3Widgets.Button($"##button_{style}", label, style))
			{
				Pressed(label);
			}
		}

		flow = new Flow(M3.Space2);
		for (var i = 0; i < _buttonStyles.Length; i++)
		{
			var label = $"{_buttonStyles[i]} + icon";
			flow.Next(M3Widgets.ButtonWidth(_buttonIcons[i], label));
			if (M3Widgets.Button($"##button_icon_{i}", label, _buttonStyles[i], _buttonIcons[i], tooltip: "Buttons take a tooltip"))
			{
				Pressed(label);
			}
		}

		flow = new Flow(M3.Space2);
		foreach (var style in _buttonStyles)
		{
			var label = $"{style} (disabled)";
			flow.Next(M3Widgets.ButtonWidth(FontAwesomeIcon.None, label));
			_ = M3Widgets.Button($"##button_disabled_{style}", label, style, enabled: false);
		}

		if (M3Widgets.Button("##button_wide", "Fixed width: fills the row", M3ButtonStyle.Filled, FontAwesomeIcon.ArrowsAltH,
			width: RowWidth))
		{
			Pressed("Full width");
		}

		Heading("Icon buttons");
		flow = new Flow(M3.Space1);
		for (var i = 0; i < _buttonStyles.Length; i++)
		{
			flow.Next(M3Widgets.IconButtonSize);
			if (M3Widgets.IconButton($"##icon_{i}", _buttonIcons[i], $"{_buttonStyles[i]} icon button", _buttonStyles[i]))
			{
				Pressed($"{_buttonStyles[i]} icon button");
			}
		}

		flow.Next(M3Widgets.IconButtonSize);
		_ = M3Widgets.IconButton("##icon_tint", FontAwesomeIcon.Heart, "Text style, tinted", tint: M3.Scheme.Tertiary);
		flow.Next(28f * M3.Scale);
		_ = M3Widgets.IconButton("##icon_small", FontAwesomeIcon.Cog, "28dp", M3ButtonStyle.Tonal, diameter: 28f * M3.Scale);
		flow.Next(48f * M3.Scale);
		_ = M3Widgets.IconButton("##icon_large", FontAwesomeIcon.Cog, "48dp", M3ButtonStyle.Tonal, diameter: 48f * M3.Scale);

		Heading("Toggle icon buttons", isNew: true);
		Caption("Standard, filled, tonal, outlined and danger styles; the first two swap glyphs as well.");
		(FontAwesomeIcon Icon, FontAwesomeIcon Selected, M3ButtonStyle Style, string Tip)[] toggles =
		[
			(FontAwesomeIcon.Eye, FontAwesomeIcon.EyeSlash, M3ButtonStyle.Text, "Hide"),
			(FontAwesomeIcon.Bell, FontAwesomeIcon.BellSlash, M3ButtonStyle.Filled, "Mute"),
			(FontAwesomeIcon.Star, FontAwesomeIcon.None, M3ButtonStyle.Tonal, "Favourite"),
			(FontAwesomeIcon.Thumbtack, FontAwesomeIcon.None, M3ButtonStyle.Outlined, "Pin"),
			(FontAwesomeIcon.Lock, FontAwesomeIcon.LockOpen, M3ButtonStyle.Danger, "Unlock"),
		];

		flow = new Flow(M3.Space1);
		for (var i = 0; i < toggles.Length; i++)
		{
			var (icon, selected, style, tip) = toggles[i];
			flow.Next(M3Widgets.IconButtonSize);
			_ = M3Widgets.IconToggle($"##toggle_{i}", icon, ref _toggles[i], $"{tip}: {(_toggles[i] ? "on" : "off")}", style, selected);
		}

		Heading("Window actions", isNew: true);
		Caption("The controls of a window without a title bar. Over a plain surface, pass a container fill.");
		var barSize = M3Widgets.WindowActionsSize(_windowActions.Length);
		var barOrigin = ImGui.GetCursorScreenPos();
		var action = M3Widgets.WindowActions("##gallery_window_actions", barOrigin + new Vector2(barSize.X, 0f), _windowActions,
			out var closed, M3.Scheme.SurfaceContainerHigh);
		if (action >= 0)
		{
			Pressed(_windowActions[action].Tooltip);
		}

		if (closed)
		{
			Pressed("Close");
		}

		ImGui.SetCursorScreenPos(barOrigin);
		ImGui.Dummy(barSize);

		Caption("With a minimize button, the bar folds into a brand, as the settings window does.");
		var brand = new M3WindowBrand(null, "RSR");
		var folded = M3Motion.Approach("##gallery_window_fold_motion", _windowFolded ? 1f : 0f, M3Motion.EmphasisedDuration);
		var openBarSize = M3Widgets.WindowActionsSize(_windowActions.Length, brand, 0f);
		barOrigin = ImGui.GetCursorScreenPos();
		_ = M3Widgets.WindowActions("##gallery_window_fold", barOrigin + new Vector2(openBarSize.X, 0f), _windowActions, brand, folded,
			out var toggled, out _, M3.Scheme.SurfaceContainerHigh);
		if (toggled)
		{
			_windowFolded = !_windowFolded;
		}

		ImGui.SetCursorScreenPos(barOrigin);
		ImGui.Dummy(openBarSize);

		Heading("Segmented buttons");
		var picked = M3Widgets.SegmentedButtons("##segmented_text", _textSegments, _segmentText);
		if (picked >= 0)
		{
			_segmentText = picked;
		}

		picked = M3Widgets.SegmentedButtons("##segmented_icon", _iconSegments, _segmentIcon);
		if (picked >= 0)
		{
			_segmentIcon = picked;
		}

		picked = M3Widgets.SegmentedButtons("##segmented_wide", _textSegments, _segmentWide, RowWidth);
		if (picked >= 0)
		{
			_segmentWide = picked;
		}
	}

	#endregion

	#region Selection

	private static void DrawSelection()
	{
		var scale = M3.Scale;

		Heading("Switches");
		_ = M3Widgets.Switch("##switch_on", ref _switchOn);
		ImGui.SameLine(0f, M3.Space2);
		_ = M3Widgets.Switch("##switch_off", ref _switchOff);
		ImGui.SameLine(0f, M3.Space2);
		var disabledOn = true;
		_ = M3Widgets.Switch("##switch_disabled_on", ref disabledOn, enabled: false);
		ImGui.SameLine(0f, M3.Space2);
		var disabledOff = false;
		_ = M3Widgets.Switch("##switch_disabled_off", ref disabledOff, enabled: false);
		_ = M3Widgets.RowSwitch("Row switch###gallery_row_switch", ref _rowSwitch, "Supporting text says what the setting does.");

		Heading("Checkboxes");
		_ = M3Widgets.Checkbox("##check_a", ref _checkA);
		Beside(_checkA ? "Checked" : "Unchecked", muted: false);
		ImGui.SameLine(0f, M3.Space3);
		_ = M3Widgets.Checkbox("##check_b", ref _checkB);
		Beside(_checkB ? "Checked" : "Unchecked", muted: false);

		Heading("Radio buttons", isNew: true);
		var picked = M3Widgets.RadioGroup("##radio_vertical", _radioOptions, _radioIndex);
		if (picked >= 0)
		{
			_radioIndex = picked;
		}

		picked = M3Widgets.RadioGroup("##radio_horizontal", _sizeOptions, _radioHorizontal, horizontal: true);
		if (picked >= 0)
		{
			_radioHorizontal = picked;
		}

		_ = M3Widgets.RadioGroup("##radio_disabled", _sizeOptions, 0, horizontal: true, enabled: false);

		if (M3Widgets.RadioButton("##radio_single", _standaloneRadio))
		{
			_standaloneRadio = !_standaloneRadio;
		}

		Beside("A bare radio button; the caller owns its state.");

		Heading("Filter and menu chips");
		var flow = new Flow(M3.Space1);
		for (var i = 0; i < _filterNames.Length; i++)
		{
			var icon = _filters[i] ? FontAwesomeIcon.Check : FontAwesomeIcon.None;
			flow.Next(M3Widgets.ChipWidth(_filterNames[i], icon));
			if (M3Widgets.Chip($"##filter_{i}", _filterNames[i], _filters[i], icon))
			{
				_filters[i] = !_filters[i];
			}
		}

		flow.Next(M3Widgets.ChipWidth("Tertiary", FontAwesomeIcon.Leaf));
		_ = M3Widgets.Chip("##chip_accent", "Tertiary", true, FontAwesomeIcon.Leaf, "Chips take an accent", M3.Scheme.Tertiary);

		var menuLabel = _menuOptions[_menuIndex];
		flow.Next(M3Widgets.ChipWidth(menuLabel, FontAwesomeIcon.Filter, FontAwesomeIcon.CaretDown));
		if (M3Widgets.Chip("##chip_menu", menuLabel, false, FontAwesomeIcon.Filter, "Opens a menu", trailingIcon: FontAwesomeIcon.CaretDown))
		{
			ImGui.OpenPopup("##gallery_chip_menu");
		}

		using (var popup = ImRaii.Popup("##gallery_chip_menu"))
		{
			if (popup)
			{
				for (var i = 0; i < _menuOptions.Length; i++)
				{
					if (M3Widgets.MenuItem($"##chip_menu_{i}", _menuOptions[i], i == _menuIndex, FontAwesomeIcon.Crosshairs))
					{
						_menuIndex = i;
						ImGui.CloseCurrentPopup();
					}
				}
			}
		}

		Heading("Input chips", isNew: true);
		flow = new Flow(M3.Space1);
		for (var i = 0; i < _tags.Count; i++)
		{
			flow.Next(M3Widgets.InputChipWidth(_tags[i], FontAwesomeIcon.Tag));
			if (M3Widgets.InputChip($"##tag_{i}", _tags[i], out var removed, FontAwesomeIcon.Tag, "Click the body, or the cross to remove"))
			{
				M3Snackbar.Show($"Opened {_tags[i]}");
			}

			if (removed)
			{
				var tag = _tags[i];
				_tags.RemoveAt(i--);
				M3Snackbar.Show($"Removed {tag}", "Undo", () => _tags.Add(tag));
			}
		}

		flow.Next(M3Widgets.ButtonWidth(FontAwesomeIcon.Plus, "Add"));
		if (M3Widgets.Button("##tag_add", "Add", M3ButtonStyle.Text, FontAwesomeIcon.Plus))
		{
			_tags.Add($"Status {_nextTag++}");
		}

		Heading("Pills");
		flow = new Flow(M3.Space1);
		foreach (var severity in Enum.GetValues<M3Severity>())
		{
			var label = severity.ToString();
			flow.Next(M3Widgets.PillSize(label).X);
			_ = M3Widgets.Pill($"##pill_{severity}", label, M3.Severity(severity));
		}

		flow.Next(M3Widgets.PillSize("With icon", FontAwesomeIcon.Bolt).X);
		_ = M3Widgets.Pill("##pill_icon", "With icon", M3.Scheme.Tertiary, FontAwesomeIcon.Bolt);
		flow.Next(M3Widgets.PillSize("Interactive", FontAwesomeIcon.MousePointer).X);
		if (M3Widgets.Pill("##pill_interactive", "Interactive", M3.Scheme.Primary, FontAwesomeIcon.MousePointer, "Pills can be pressable", interactive: true))
		{
			Pressed("Interactive pill");
		}

		Heading("Sliders");
		var track = 200f * scale;
		_ = M3Widgets.Slider("##slider_float", ref _slider, 0f, 1f, $"{_slider:P0}", track);
		_ = M3Widgets.SliderInt("##slider_int", ref _sliderInt, 0, 10, _sliderInt.ToString(), track);
		_ = M3Widgets.RowDragFloat("Row slider (float)###gallery_row_float", ref _rowFloat, 0f, 5f, "%.1f s");
		_ = M3Widgets.RowDragInt("Row slider (int)###gallery_row_int", ref _rowInt, 0, 30);

		Heading("Colour swatch");
		_ = M3Widgets.ColorSwatch("##gallery_swatch", ref _swatch);
		Beside($"Opens a picker. Now {Hex(_swatch)}.");
	}

	#endregion

	#region Inputs

	private static void DrawInputs()
	{
		var width = MathF.Min(RowWidth, 380f * M3.Scale);

		Heading("Search field");
		_ = M3Widgets.SearchField("##gallery_search", "Search settings", ref _search, width);

		Heading("Text fields", isNew: true);
		_ = M3Widgets.TextField("##gallery_name", "Name", ref _name, width);
		ImGui.Dummy(new Vector2(0f, M3.Space2));

		_ = M3Widgets.TextField("##gallery_target", "Target name", ref _target, width,
			supporting: "Matches any enemy whose name contains this text.", leadingIcon: FontAwesomeIcon.Crosshairs);
		ImGui.Dummy(new Vector2(0f, M3.Space2));

		var valid = uint.TryParse(_statusId, out _);
		_ = M3Widgets.TextField("##gallery_status", "Status ID", ref _statusId, width,
			supporting: valid ? "Validates as you type." : "Status IDs are whole numbers.", error: !valid, leadingIcon: FontAwesomeIcon.Hashtag);

		Heading("Combo and menu items");
		_ = M3Widgets.Combo("##gallery_combo", ref _comboIndex, _comboItems, width);
		ImGui.Dummy(new Vector2(0f, M3.Space1));
		_ = M3Widgets.Combo("##gallery_combo_empty", ref _comboEmpty, _comboItems, width, "Nothing picked yet");
	}

	#endregion

	#region Feedback

	private static void DrawFeedback()
	{
		var scale = M3.Scale;
		var width = RowWidth;

		Heading("Banners");
		(M3Severity Severity, FontAwesomeIcon Icon, string Message, string? Action)[] banners =
		[
			(M3Severity.Neutral, FontAwesomeIcon.InfoCircle, "A neutral banner, in the accent colour.", null),
			(M3Severity.Info, FontAwesomeIcon.Lightbulb, "An info banner, like the rotating usage tip.", "Copy"),
			(M3Severity.Success, FontAwesomeIcon.CheckCircle, "A success banner.", null),
			(M3Severity.Warning, FontAwesomeIcon.ExclamationTriangle, "A warning banner with an action. Long messages wrap onto further lines instead of running under the action button.", "Details"),
			(M3Severity.Error, FontAwesomeIcon.TimesCircle, "An error banner.", "Fix"),
		];

		foreach (var (severity, icon, message, action) in banners)
		{
			if (M3Widgets.Banner($"##banner_{severity}", message, severity, icon, action, "Banners take a tooltip"))
			{
				Pressed($"{severity} banner action");
			}

			ImGui.Dummy(new Vector2(0f, M3.Space1));
		}

		Heading("Linear progress");
		M3Widgets.LinearProgress(new Vector2(width, 6f * scale), _progress, 0.75f);
		ImGui.Dummy(new Vector2(0f, M3.Space1));
		_ = M3Widgets.Slider("##gallery_progress", ref _progress, 0f, 1f, $"{_progress:P0}", 200f * scale);
		Caption("The tick marks a threshold, here 75%.");

		Heading("Indeterminate linear progress", isNew: true);
		M3Widgets.LinearProgressIndeterminate(new Vector2(width, 4f * scale));

		Heading("Circular progress", isNew: true);
		var flow = new Flow(M3.Space3);
		flow.Next(48f * scale);
		M3Widgets.CircularProgress(48f * scale, _progress);
		flow.Next(64f * scale);
		M3Widgets.CircularProgress(64f * scale, _progress, 6f);
		flow.Next(48f * scale);
		M3Widgets.CircularProgress(48f * scale);
		flow.Next(24f * scale);
		M3Widgets.CircularProgress(24f * scale, null, 3f);
		Caption("Determinate ones follow the slider above.");

		Heading("Badges", isNew: true);
		flow = new Flow(M3.Space2);
		(FontAwesomeIcon Icon, string? Text, string Tip)[] badges =
		[
			(FontAwesomeIcon.Bell, null, "A dot"),
			(FontAwesomeIcon.Envelope, "3", "A count"),
			(FontAwesomeIcon.Inbox, M3Widgets.BadgeCount(1200), "Counts cap at 999+"),
		];

		for (var i = 0; i < badges.Length; i++)
		{
			var (icon, text, tip) = badges[i];
			flow.Next(M3Widgets.IconButtonSize);
			_ = M3Widgets.IconButton($"##badge_{i}", icon, tip, M3ButtonStyle.Tonal);
			M3Widgets.Badge(text);
		}

		flow.Next(M3Widgets.ButtonWidth(FontAwesomeIcon.None, "Updates"));
		_ = M3Widgets.Button("##badge_button", "Updates", M3ButtonStyle.Outlined);
		M3Widgets.Badge("New", M3.Scheme.Tertiary);

		Heading("Snackbars", isNew: true);
		Caption("Shown along the bottom of this window, one at a time. Hovering one holds it on screen.");
		flow = new Flow(M3.Space2);
		flow.Next(M3Widgets.ButtonWidth(FontAwesomeIcon.None, "Message"));
		if (M3Widgets.Button("##snack_plain", "Message", M3ButtonStyle.Tonal))
		{
			M3Snackbar.Show("Copied to clipboard");
		}

		flow.Next(M3Widgets.ButtonWidth(FontAwesomeIcon.None, "With action"));
		if (M3Widgets.Button("##snack_action", "With action", M3ButtonStyle.Tonal))
		{
			M3Snackbar.Show("Rotation settings reset", "Undo", () => M3Snackbar.Show("Undone"));
		}

		flow.Next(M3Widgets.ButtonWidth(FontAwesomeIcon.None, "Long text"));
		if (M3Widgets.Button("##snack_long", "Long text", M3ButtonStyle.Tonal))
		{
			M3Snackbar.Show("A longer snackbar wraps onto a second line when it will not fit on one, and stays up for the long duration.",
				"Got it", duration: M3Snackbar.LongDuration);
		}

		flow.Next(M3Widgets.ButtonWidth(FontAwesomeIcon.None, "Queue three"));
		if (M3Widgets.Button("##snack_queue", "Queue three", M3ButtonStyle.Tonal))
		{
			M3Snackbar.Show("First of three", duration: 2f);
			M3Snackbar.Show("Second of three", duration: 2f);
			M3Snackbar.Show("Third of three", duration: 2f);
		}

		Heading("Dialogs", isNew: true);
		flow = new Flow(M3.Space2);
		flow.Next(M3Widgets.ButtonWidth(FontAwesomeIcon.Undo, "With hero icon"));
		if (M3Widgets.Button("##dialog_icon_open", "With hero icon", M3ButtonStyle.Tonal, FontAwesomeIcon.Undo))
		{
			M3Dialog.Open("##gallery_dialog_icon");
		}

		flow.Next(M3Widgets.ButtonWidth(FontAwesomeIcon.None, "Plain"));
		if (M3Widgets.Button("##dialog_plain_open", "Plain", M3ButtonStyle.Tonal))
		{
			M3Dialog.Open("##gallery_dialog_plain");
		}

		DrawDialogs();

		Heading("Empty state", isNew: true);
		using (var card = M3Card.Begin("gallery_empty_state", null, style: M3CardStyle.Outlined))
		{
			if (M3Widgets.EmptyState("##gallery_empty", "No rotations loaded",
				"Download the rotations for your job, or point Rotation Solver at a local folder.", FontAwesomeIcon.Inbox, "Download"))
			{
				Pressed("Empty state action");
			}
		}

		Heading("Tooltips");
		_ = M3Widgets.Button("##tooltip_button", "Hover for a tooltip", M3ButtonStyle.Outlined, FontAwesomeIcon.InfoCircle);
		ImguiTooltips.HoveredTooltip("Tooltips drawn through ImguiTooltips honour the Show tooltips setting.");
	}

	private static void DrawDialogs()
	{
		using (var dialog = M3Dialog.Begin("##gallery_dialog_icon", "Reset rotation settings?",
			"Every option of the current rotation goes back to its default. This cannot be undone.", FontAwesomeIcon.Undo))
		{
			if (dialog.IsOpen)
			{
				_ = M3Widgets.RowSwitch("Also reset action settings###gallery_dialog_switch", ref _dialogSwitch);
				if (M3Dialog.Actions([new("Cancel"), new("Reset", M3ButtonStyle.Danger, FontAwesomeIcon.Undo)]) == 1)
				{
					M3Snackbar.Show(_dialogSwitch ? "Pretended to reset everything" : "Pretended to reset the rotation");
				}
			}
		}

		using (var dialog = M3Dialog.Begin("##gallery_dialog_plain", "Discard changes?",
			"A dialog without a hero icon keeps its headline leading-aligned. Escape dismisses it."))
		{
			if (dialog.IsOpen && M3Dialog.Actions([new("Keep editing"), new("Discard", M3ButtonStyle.Filled)]) == 1)
			{
				Pressed("Discard");
			}
		}
	}

	#endregion

	#region Layout

	private static void DrawLayout()
	{
		var s = M3.Scheme;

		Heading("Cards");
		foreach (var style in Enum.GetValues<M3CardStyle>())
		{
			using var card = M3Card.Begin($"gallery_card_{style}", $"{style} card", FontAwesomeIcon.Square, style: style,
				subtitle: "A subtitle wraps under the title.");
			ImGui.TextWrapped("Card content is inset from the accent rail, and wrapped text stops at the card's padding.");
		}

		using (var card = M3Card.Begin("gallery_card_accent", "Accented card", FontAwesomeIcon.Leaf, s.Tertiary))
		{
			_ = M3Widgets.RowSwitch("A setting inside a card###gallery_card_switch", ref _rowSwitch);
		}

		Heading("Expandable card");
		using (var card = M3ExpandableCard.Begin("gallery_expandable", "Expandable card", ref _expanded, FontAwesomeIcon.LayerGroup,
			badge: "2 settings", badgeAccent: s.Tertiary))
		{
			if (card.Expanded)
			{
				_ = M3Widgets.RowSwitch("First setting###gallery_expand_a", ref _parentSetting);
				_ = M3Widgets.RowDragInt("Second setting###gallery_expand_b", ref _childValue, 0, 100);
			}
		}

		Heading("Setting rows");
		var plain = M3SettingRow.Begin("A row with no control", null, Vector2.Zero);
		M3SettingRow.End(plain);

		var withIcon = M3SettingRow.Begin("Leading icon and supporting text", "Supporting text wraps beneath the headline.",
			M3Widgets.SwitchSize(), leadingIcon: FontAwesomeIcon.Bolt);
		_ = M3Widgets.Switch("##row_icon_switch", ref _rowWithIcon);
		M3SettingRow.End(withIcon);

		var disabledValue = true;
		var disabled = M3SettingRow.Begin("Disabled row", "Label, icon and control all dim.", M3Widgets.SwitchSize(),
			leadingIcon: FontAwesomeIcon.Ban, disabled: true);
		_ = M3Widgets.Switch("##row_disabled_switch", ref disabledValue, enabled: false);
		M3SettingRow.End(disabled);

		var struck = M3SettingRow.Begin("Struck-through row: hidden by a job filter, kept in place so the list does not reflow",
			null, Vector2.Zero, strikeThrough: true);
		M3SettingRow.End(struck);

		var tinted = M3SettingRow.Begin("Custom label colour", null, Vector2.Zero, s.Tertiary);
		M3SettingRow.End(tinted);

		if (M3SettingRow.NavigationRow("##gallery_nav_row", "Navigation row", "The whole row is one target.",
			FontAwesomeIcon.ExternalLinkAlt, FontAwesomeIcon.ChevronRight, s.Tertiary))
		{
			Pressed("Navigation row");
		}

		Heading("Sub-groups");
		_ = M3Widgets.RowSwitch("Parent setting###gallery_parent", ref _parentSetting, "Dependent settings indent under it while it is on.");
		if (_parentSetting)
		{
			using var group = M3SubGroup.Begin();
			_ = M3Widgets.RowSwitch("Dependent switch###gallery_child", ref _childSetting);
			_ = M3Widgets.RowDragInt("Dependent value###gallery_child_value", ref _childValue, 0, 100);
		}

		Heading("Structure");
		Caption("A divider:");
		M3Widgets.Divider();
		M3Widgets.SectionLabel("A section label with an accent", s.Tertiary);

		Heading("Action icons");
		DrawActionIcons();
	}

	private static void DrawActionIcons()
	{
		var actions = DataCenter.CurrentRotation?.AllActions;
		if (actions is not { Length: > 0 })
		{
			_ = M3Widgets.EmptyState("##gallery_no_actions", "No rotation loaded",
				"Action icons need the current job's rotation.", FontAwesomeIcon.Robot);
			return;
		}

		var scale = M3.Scale;
		var flow = new Flow(M3.Space2);
		foreach (var size in _actionIconSizes)
		{
			flow.Next(size * scale);
			_ = M3ActionIcon.Draw($"##gallery_action_{size}", actions[0], size * scale, showCooldown: false);
		}

		flow.Next(48f * scale);
		_ = M3ActionIcon.Draw("##gallery_action_empty", null, 48f * scale, showCooldown: false);

		Caption("With cooldowns and charges, when the rotation's actions have them:");
		flow = new Flow(M3.Space1);
		for (var i = 0; i < actions.Length && i < 16; i++)
		{
			flow.Next(44f * scale);
			_ = M3ActionIcon.Draw($"##gallery_cooldown_{i}", actions[i], 44f * scale, showCooldown: true);
		}
	}

	#endregion

	#region Navigation

	private static void DrawNavigation()
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		Heading("Drawer and rail");
		M3NavItem[] items =
		[
			new("home", "Home", FontAwesomeIcon.Home, _navSelected == "home"),
			new("rotations", "Rotations", FontAwesomeIcon.Sync, _navSelected == "rotations", "Items take a tooltip", Badge: "2"),
			new("actions", "Actions", FontAwesomeIcon.Bolt, _navSelected == "actions", SeparatorAfter: true),
			new("settings", "Settings", FontAwesomeIcon.Cog, _navSelected == "settings"),
			new("debug", "Debug", FontAwesomeIcon.Bug, _navSelected == "debug", Accent: s.Tertiary),
		];

		var height = 300f * scale;
		using (var drawer = ImRaii.Child("##gallery_drawer", new Vector2(220f * scale, height), false))
		{
			if (drawer)
			{
				_navSelected = M3Navigation.Draw("gallery_drawer", items, expanded: true) ?? _navSelected;
			}
		}

		ImGui.SameLine(0f, M3.Space3);

		using (var rail = ImRaii.Child("##gallery_rail", new Vector2(88f * scale, height), false))
		{
			if (rail)
			{
				_navSelected = M3Navigation.Draw("gallery_rail", items, expanded: false) ?? _navSelected;
			}
		}

		Caption($"The drawer collapses to the rail below {M3Navigation.DrawerBreakpoint:0}dp. Both share one selection.");

		Heading("Tabs", isNew: true);
		Caption("The page switcher at the top of this gallery is a primary tab row too.");
		var picked = M3Navigation.Tabs("##gallery_tabs_icons", _demoIconTabs, _tabIcons, width: RowWidth);
		if (picked >= 0)
		{
			_tabIcons = picked;
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));
		picked = M3Navigation.Tabs("##gallery_tabs_text", _demoTextTabs, _tabText, width: RowWidth);
		if (picked >= 0)
		{
			_tabText = picked;
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));
		picked = M3Navigation.Tabs("##gallery_tabs_secondary", _demoSecondaryTabs, _tabSecondary, secondary: true, width: RowWidth);
		if (picked >= 0)
		{
			_tabSecondary = picked;
		}

		Caption("Secondary tabs, for a second level under primary ones.");
	}

	#endregion

	#region Helpers

	private static void Heading(string title, bool isNew = false)
	{
		M3Widgets.SectionLabel(isNew ? $"{title} - new" : title, isNew ? M3.Scheme.Tertiary : null);
	}

	private static float RowWidth => ImGui.GetContentRegionAvail().X - M3Card.RightInset;

	private static void Caption(string text)
	{
		using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(M3.Scheme.OnSurfaceVariant, 0.9f));
		ImGui.TextUnformatted(text);
	}

	private static void Beside(string text, bool muted = true)
	{
		var height = ImGui.GetItemRectSize().Y;
		ImGui.SameLine(0f, M3.Space2);
		ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ((height - ImGui.GetTextLineHeight()) * 0.5f));

		if (muted)
		{
			Caption(text);
		}
		else
		{
			ImGui.TextUnformatted(text);
		}
	}

	private static void Pressed(string what)
	{
		M3Snackbar.Show($"{what} pressed", duration: 2f);
	}

	private static string Hex(Vector4 color)
	{
		static int Channel(float value)
		{
			return (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);
		}

		return $"#{Channel(color.X):X2}{Channel(color.Y):X2}{Channel(color.Z):X2}";
	}

	private struct Flow(float gap)
	{
		private readonly float _available = RowWidth;
		private float _used;

		public void Next(float width)
		{
			if (_used > 0f && _used + gap + width <= _available)
			{
				ImGui.SameLine(0f, gap);
				_used += gap + width;
			}
			else
			{
				_used = width;
			}
		}
	}

	#endregion
}
