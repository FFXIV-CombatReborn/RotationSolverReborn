using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using ECommons.DalamudServices;
using ECommons.Logging;
using RebornMaterial;
using System.Globalization;

namespace RotationSolver.UI.ExtraWindows;

internal sealed class UpdateNotesWindow : Window
{
	private const ImGuiWindowFlags BaseFlags = ImGuiWindowFlags.NoTitleBar
		| ImGuiWindowFlags.NoCollapse
		| ImGuiWindowFlags.NoScrollbar
		| ImGuiWindowFlags.NoScrollWithMouse
		| ImGuiWindowFlags.NoSavedSettings;

	private const string GitHubUrl = $"https://github.com/{Service.USERNAME}/{Service.REPO}";
	private const string ReleasesUrl = $"{GitHubUrl}/releases";
	private const string ContributorsUrl = $"{GitHubUrl}/graphs/contributors";
	private const string BannerResource = "RotationSolver.Changelog.banner.jpg";

	private static readonly Vector2 PreferredSize = new(800f, 820f);

	private static readonly M3WindowAction[] Links =
	[
		new("##changelog_discord", FontAwesomeIcon.Comments, "Join the Discord"),
		new("##changelog_github", FontAwesomeIcon.Code, "View on GitHub"),
		new("##changelog_kofi", FontAwesomeIcon.MugHot, "Support the developer on Ko-fi"),
	];

	private static readonly string[] LinkUrls = ["https://discord.gg/r9V4RHYt6v", GitHubUrl, "https://ko-fi.com/ltscombatreborn"];

	private static readonly M3Tab[] PageTabs =
	[
		new("Changelog", FontAwesomeIcon.ListUl),
		new("Credits", FontAwesomeIcon.Heart),
	];

	private readonly string _currentVersion = typeof(UpdateNotesWindow).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";

	private readonly M3HeroHeader _hero = new()
	{
		Height = 300f,
		MaxImageHeight = 300f,
		ImageFocus = new Vector2(0.5f, 0.35f),
		ImageBlend = 0.14f,
	};

	private ChangelogData? _data;
	private bool[] _expanded = [];

	private Version? _lastSeen;
	private int _newCount;
	private int _tab;
	private bool _scrollToTop;
	private bool _checkedForUpdate;

	private M3Style.Scope _theme;

	public UpdateNotesWindow()
		: base("Rotation Solver Reborn Update Notes###rsrUpdateNotesWindow", BaseFlags)
	{
		SizeConstraints = new WindowSizeConstraints()
		{
			MinimumSize = new Vector2(560f, 480f),
			MaximumSize = new Vector2(1600f, 1400f),
		};
		RespectCloseHotkey = true;
	}

	public void OpenIfUpdated()
	{
		if (_checkedForUpdate)
		{
			return;
		}

		_checkedForUpdate = true;

		var config = Service.Config;
		if (string.Equals(config.LastSeenChangelog, _currentVersion, StringComparison.Ordinal))
		{
			return;
		}

		if (!config.TutorialDone)
		{
			config.LastSeenChangelog = _currentVersion;
			config.Save();
			return;
		}

		if (config.ChangelogPopup)
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
		_data ??= ChangelogData.Load();

		_lastSeen = ChangelogData.ParseVersion(Service.Config.LastSeenChangelog);
		if (_lastSeen == new Version(0, 0, 0, 0))
		{
			_lastSeen = null;
		}

		var entries = _data.Changelog.Entries;
		_expanded = new bool[entries.Count];
		_newCount = 0;
		for (var i = 0; i < entries.Count; i++)
		{
			var isNew = IsNew(entries[i]);
			if (isNew)
			{
				_newCount++;
			}

			_expanded[i] = i == 0 || isNew;
		}

		_tab = 0;
		_scrollToTop = true;
		base.OnOpen();
	}

	public override void OnClose()
	{
		if (!string.Equals(Service.Config.LastSeenChangelog, _currentVersion, StringComparison.Ordinal))
		{
			Service.Config.LastSeenChangelog = _currentVersion;
			Service.Config.Save();
		}

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
		var data = _data ?? ChangelogData.Empty;

		DrawHeader();
		DrawWhatsNew(data.Changelog.Tagline);

		if (data.Credits.Categories.Count > 0)
		{
			var picked = M3Navigation.Tabs("##changelog_tabs", PageTabs, _tab, secondary: true);
			if (picked >= 0)
			{
				_tab = picked;
				_scrollToTop = true;
			}
		}
		else
		{
			_tab = 0;
		}

		DrawBody(data);
		DrawFooter();
	}

	private bool IsNew(ChangelogEntry entry)
	{
		return _lastSeen != null && entry.ParsedVersion != null && entry.ParsedVersion > _lastSeen;
	}

	#region Header

	private void DrawHeader()
	{
		var (min, max) = _hero.Draw(GetBanner());
		DrawHeaderActions(max.X, min.Y);

		ImGui.SetCursorScreenPos(new Vector2(ImGui.GetWindowPos().X + ImGui.GetStyle().WindowPadding.X, max.Y + (4f * M3.Scale)));
		DrawIdentity();
	}

	private void DrawHeaderActions(float right, float top)
	{
		var inset = 14f * M3.Scale;
		var pressed = M3Widgets.WindowActions("##changelog_actions", new Vector2(right - inset, top + inset), Links, out var closed);
		if (pressed >= 0)
		{
			OpenLink(LinkUrls[pressed]);
		}

		if (closed)
		{
			IsOpen = false;
		}
	}

	private void DrawIdentity()
	{
		const string title = "Rotation Solver Reborn";
		const string subtitle = "Update Notes";

		var s = M3.Scheme;
		var scale = M3.Scale;
		var origin = ImGui.GetCursorScreenPos();
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);

		Vector2 titleSize;
		float subtitleHeight;
		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			titleSize = ImGui.CalcTextSize(title);
		}

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			subtitleHeight = ImGui.CalcTextSize(subtitle).Y;
		}

		var textHeight = titleSize.Y + (2f * scale) + subtitleHeight;
		var height = MathF.Max(40f * scale, textHeight);
		ImGui.Dummy(new Vector2(width, height));
		var next = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();

		var logoMin = origin + new Vector2(2f * scale, 0f);
		var logoMax = logoMin + new Vector2(height, height);
		var logo = MainWindow.GetLogoTexture();
		if (logo?.Handle != null)
		{
			drawList.AddImageRounded(logo.Handle, logoMin, logoMax, Vector2.Zero, Vector2.One, M3.U32(Vector4.One), M3.ShapeSmall);
		}
		else
		{
			drawList.AddRectFilled(logoMin, logoMax, M3.U32(s.PrimaryContainer), M3.ShapeSmall);
			M3Draw.IconCentered(drawList, FontAwesomeIcon.Fire, logoMin, logoMax, s.OnPrimaryContainer);
		}

		var textX = logoMax.X + (14f * scale);
		var textY = origin.Y + ((height - textHeight) * 0.5f);
		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			drawList.AddText(new Vector2(textX, textY), M3.U32(s.OnSurface), title);
		}

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			drawList.AddText(new Vector2(textX, textY + titleSize.Y + (2f * scale)), M3.U32(s.Primary), subtitle);
		}

		var versionLabel = $"v{_currentVersion}";
		var newLabel = _newCount == 1 ? "1 new update" : $"{_newCount} new updates";
		var versionSize = M3Widgets.PillSize(versionLabel);
		var newSize = _newCount > 0 ? M3Widgets.PillSize(newLabel, FontAwesomeIcon.Star) : Vector2.Zero;
		var room = origin.X + width - (textX + titleSize.X + (16f * scale));
		var showNew = newSize.X > 0f && versionSize.X + M3.Space2 + newSize.X <= room;
		if (versionSize.X > room)
		{
			return;
		}

		var pillsWidth = versionSize.X + (showNew ? M3.Space2 + newSize.X : 0f);
		ImGui.SetCursorScreenPos(new Vector2(origin.X + width - pillsWidth, origin.Y + ((height - versionSize.Y) * 0.5f)));
		_ = M3Widgets.Pill("##changelog_version", versionLabel, s.Primary, tooltip: "The version you are running");

		if (showNew)
		{
			ImGui.SameLine(0f, M3.Space2);
			_ = M3Widgets.Pill("##changelog_new_count", newLabel, s.Success, FontAwesomeIcon.Star);
		}

		ImGui.SetCursorScreenPos(next);
	}

	private static void DrawWhatsNew(string tagline)
	{
		const string label = "What's New";

		var s = M3.Scheme;
		var scale = M3.Scale;
		var origin = ImGui.GetCursorScreenPos();
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);

		float height;
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			height = ImGui.GetTextLineHeight();
		}

		ImGui.Dummy(new Vector2(width, height));
		var drawList = ImGui.GetWindowDrawList();

		var iconSize = M3Draw.MeasureIcon(FontAwesomeIcon.Star);
		var x = origin.X + (4f * scale);
		M3Draw.Icon(drawList, FontAwesomeIcon.Star, new Vector2(x, origin.Y + ((height - iconSize.Y) * 0.5f)), s.Primary);
		x += iconSize.X + (10f * scale);

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			drawList.AddText(new Vector2(x, origin.Y), M3.U32(s.Primary), label);
			x += ImGui.CalcTextSize(label).X + (14f * scale);
		}

		if (!string.IsNullOrEmpty(tagline))
		{
			var text = M3Navigation.Truncate(tagline, origin.X + width - x);
			var textSize = ImGui.CalcTextSize(text);
			drawList.AddText(new Vector2(x, origin.Y + ((height - textSize.Y) * 0.5f)), M3.U32(s.OnSurfaceVariant, 0.9f), text);
		}
	}

	// Fetched every frame on purpose; Dalamud caches the texture.
	internal static IDalamudTextureWrap? GetBanner()
	{
		return Svc.Texture.GetFromManifestResource(typeof(UpdateNotesWindow).Assembly, BannerResource)
			.TryGetWrap(out var banner, out _) ? banner : null;
	}

	#endregion

	#region Body

	private void DrawBody(ChangelogData data)
	{
		var scale = M3.Scale;
		var style = ImGui.GetStyle();

		var footerHeight = M3.Space2 + (style.ItemSpacing.Y * 2f) + M3Widgets.ButtonHeight;
		var height = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().Y - footerHeight);

		var padding = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(M3.Space1, 0f));
		using var child = ImRaii.Child("##changelog_body", new Vector2(0f, height), false, ImGuiWindowFlags.AlwaysUseWindowPadding);
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

		ImGui.Dummy(new Vector2(0f, M3.Space1));

		if (_tab == 1)
		{
			DrawCredits(data.Credits);
		}
		else
		{
			DrawChangelog(data.Changelog);
		}
	}

	private void DrawFooter()
	{
		var scale = M3.Scale;
		ImGui.Dummy(new Vector2(0f, M3.Space2));

		var width = MathF.Min(220f * scale, ImGui.GetContentRegionAvail().X);
		ImGui.SetCursorPosX((ImGui.GetWindowWidth() - width) * 0.5f);
		if (M3Widgets.Button("##changelog_got_it", "Got it!", M3ButtonStyle.Filled, FontAwesomeIcon.Check, width,
			tooltip: "Reopen these notes any time from the About page or with /rotation Changelog."))
		{
			IsOpen = false;
		}
	}

	#endregion

	#region Changelog

	private void DrawChangelog(ChangelogFile changelog)
	{
		var entries = changelog.Entries;
		if (entries.Count == 0)
		{
			if (M3Widgets.EmptyState("##changelog_empty", "No update notes yet", "Notes for every release are on GitHub.",
				FontAwesomeIcon.Newspaper, "Open releases"))
			{
				OpenLink(ReleasesUrl);
			}

			return;
		}

		if (_expanded.Length != entries.Count)
		{
			Array.Resize(ref _expanded, entries.Count);
		}

		for (var i = 0; i < entries.Count; i++)
		{
			DrawEntry(i, entries[i]);
		}

		if (M3Widgets.Button("##changelog_releases", "Older releases on GitHub", M3ButtonStyle.Text, FontAwesomeIcon.ExternalLinkAlt))
		{
			OpenLink(ReleasesUrl);
		}

		ImGui.Dummy(new Vector2(0f, M3.Space2));
	}

	private void DrawEntry(int index, ChangelogEntry entry)
	{
		var s = M3.Scheme;
		var isNew = IsNew(entry);
		var highlighted = isNew || index == 0;
		var title = string.IsNullOrEmpty(entry.Title) ? entry.DisplayVersion : $"{entry.DisplayVersion} - {entry.Title}";
		var badge = isNew ? "New" : index == 0 ? "Latest" : null;

		using var card = M3ExpandableCard.Begin($"changelog_entry_{index}", title, ref _expanded[index],
			isNew ? FontAwesomeIcon.Star : FontAwesomeIcon.Tag,
			highlighted ? s.Primary : s.Secondary,
			badge, isNew ? s.Success : s.Primary, entry.Date);

		if (card.Expanded)
		{
			DrawEntryBody(entry);
		}
	}

	private static void DrawEntryBody(ChangelogEntry entry)
	{
		var s = M3.Scheme;
		var message = entry.Message ?? string.Empty;

		if (message.Length > 0)
		{
			using var color = ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(s.OnSurfaceVariant, 0.95f));
			ImGui.TextUnformatted(message);
		}

		for (var i = 0; i < entry.Sections.Count; i++)
		{
			if (i > 0 || message.Length > 0)
			{
				ImGui.Dummy(new Vector2(0f, M3.Space1));
			}

			DrawSection(entry.Sections[i]);
		}
	}

	private static void DrawSection(ChangelogSection section)
	{
		if (!string.IsNullOrEmpty(section.Title))
		{
			DrawSectionHeading(section.Title, section.ResolvedIcon);
		}

		using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 5f) * M3.Scale);
		foreach (var item in section.Items)
		{
			DrawBullet(item);
		}
	}

	private static void DrawSectionHeading(string title, FontAwesomeIcon icon)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var width = MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X - M3Card.RightInset);

		float lineHeight;
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			lineHeight = ImGui.GetTextLineHeight();
		}

		var height = MathF.Max(30f * scale, lineHeight + (10f * scale));
		ImGui.Dummy(new Vector2(width, height));
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();

		drawList.AddRectFilled(min, max, M3.U32(s.SecondaryContainer, 0.5f), M3.ShapeSmall);

		var x = min.X + (12f * scale);
		if (icon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(icon);
			M3Draw.Icon(drawList, icon, new Vector2(x, min.Y + ((height - iconSize.Y) * 0.5f)), s.Primary);
			x += iconSize.X + (10f * scale);
		}

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			var label = M3Navigation.Truncate(title, max.X - x - (12f * scale));
			var labelSize = ImGui.CalcTextSize(label);
			drawList.AddText(new Vector2(x, min.Y + ((height - labelSize.Y) * 0.5f)), M3.U32(s.OnSecondaryContainer), label);
		}
	}

	private static void DrawBullet(string text)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var indent = 22f * scale;
		var origin = ImGui.GetCursorScreenPos();

		ImGui.GetWindowDrawList().AddCircleFilled(
			new Vector2(origin.X + (10f * scale), origin.Y + (ImGui.GetTextLineHeight() * 0.5f)),
			2.5f * scale, M3.U32(s.Primary, 0.9f), 12);

		ImGui.Indent(indent);
		using (ImRaii.PushColor(ImGuiCol.Text, M3.Alpha(s.OnSurface, 0.9f)))
		{
			ImGui.TextUnformatted(text);
		}

		ImGui.Unindent(indent);
	}

	#endregion

	#region Credits

	private static void DrawCredits(CreditsFile credits)
	{
		var s = M3.Scheme;
		var categories = credits.Categories;

		for (var i = 0; i < categories.Count; i++)
		{
			var category = categories[i];
			using var card = M3Card.Begin($"changelog_credits_{i}", category.Category, category.ResolvedIcon,
				i % 2 == 0 ? s.Primary : s.Tertiary);
			using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, new Vector2(8f, 2f) * M3.Scale);

			for (var j = 0; j < category.Items.Count; j++)
			{
				DrawCredit($"##changelog_credit_{i}_{j}", category.Items[j]);
			}
		}

		if (M3Widgets.EmptyState("##changelog_thanks", "Thank you",
			"…and to everyone who tests, reports bugs, translates and helps out on GitHub and Discord.",
			FontAwesomeIcon.Heart, "All contributors on GitHub"))
		{
			OpenLink(ContributorsUrl);
		}
	}

	private static void DrawCredit(string id, CreditEntry credit)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var width = MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X - M3Card.RightInset);
		var height = M3.FitText(36f, 6f);
		var url = credit.Url ?? string.Empty;
		var linked = url.Length > 0;

		var clicked = false;
		if (linked)
		{
			clicked = ImGui.InvisibleButton(id, new Vector2(width, height));
		}
		else
		{
			ImGui.Dummy(new Vector2(width, height));
		}

		var hovered = linked && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = linked && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();

		if (hovered || held)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), M3.ShapeSmall);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			ImGui.SetTooltip(url);
		}

		var avatar = 26f * scale;
		var avatarCenter = new Vector2(min.X + (6f * scale) + (avatar * 0.5f), min.Y + (height * 0.5f));
		drawList.AddCircleFilled(avatarCenter, avatar * 0.5f, M3.U32(s.SecondaryContainer), 24);
		var initial = Initial(credit.Name);
		drawList.AddText(avatarCenter - (ImGui.CalcTextSize(initial) * 0.5f), M3.U32(s.OnSecondaryContainer), initial);

		var nameX = avatarCenter.X + (avatar * 0.5f) + (12f * scale);
		var right = max.X - (8f * scale);
		if (linked)
		{
			var iconSize = M3Draw.MeasureIcon(FontAwesomeIcon.ExternalLinkAlt);
			M3Draw.Icon(drawList, FontAwesomeIcon.ExternalLinkAlt, new Vector2(right - iconSize.X, min.Y + ((height - iconSize.Y) * 0.5f)),
				M3.Alpha(s.OnSurfaceVariant, hovered ? 1f : 0.7f));
			right -= iconSize.X + (12f * scale);
		}

		var name = M3Navigation.Truncate(credit.Name, MathF.Max(8f * scale, right - nameX));
		var nameSize = ImGui.CalcTextSize(name);
		drawList.AddText(new Vector2(nameX, min.Y + ((height - nameSize.Y) * 0.5f)), M3.U32(s.OnSurface), name);

		var roleRoom = right - (nameX + nameSize.X + (16f * scale));
		if (!string.IsNullOrEmpty(credit.Role) && roleRoom > 24f * scale)
		{
			var role = M3Navigation.Truncate(credit.Role, roleRoom);
			var roleSize = ImGui.CalcTextSize(role);
			drawList.AddText(new Vector2(right - roleSize.X, min.Y + ((height - roleSize.Y) * 0.5f)), M3.U32(s.OnSurfaceVariant, 0.9f), role);
		}

		if (clicked)
		{
			OpenLink(url);
		}
	}

	private static string Initial(string name)
	{
		return string.IsNullOrWhiteSpace(name)
			? "?"
			: StringInfo.GetNextTextElement(name.Trim()).ToUpperInvariant();
	}

	#endregion

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
}
