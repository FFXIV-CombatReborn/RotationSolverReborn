using ECommons.Logging;

namespace RotationSolver.UI.ExtraWindows;

internal sealed class ChangelogData
{
	private const string ChangelogResource = "RotationSolver.Changelog.changelog.json";
	private const string CreditsResource = "RotationSolver.Changelog.credits.json";

	public static ChangelogData Empty { get; } = new(new ChangelogFile(), new CreditsFile());

	private ChangelogData(ChangelogFile changelog, CreditsFile credits)
	{
		Changelog = changelog;
		Credits = credits;
	}

	public ChangelogFile Changelog { get; }
	public CreditsFile Credits { get; }

	public static ChangelogData Load()
	{
		var changelog = Read<ChangelogFile>(ChangelogResource) ?? new ChangelogFile();
		var credits = Read<CreditsFile>(CreditsResource) ?? new CreditsFile();

		foreach (var entry in changelog.Entries)
		{
			entry.ParsedVersion = ParseVersion(entry.Version);
			foreach (var section in entry.Sections)
			{
				section.ResolvedIcon = ParseIcon(section.Icon);
			}
		}

		foreach (var category in credits.Categories)
		{
			category.ResolvedIcon = ParseIcon(category.Icon);
		}

		return new ChangelogData(changelog, credits);
	}

	public static Version? ParseVersion(string? text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}

		return Version.TryParse(text.Trim().TrimStart('v', 'V'), out var version) ? version : null;
	}

	private static FontAwesomeIcon ParseIcon(string? name)
	{
		return !string.IsNullOrWhiteSpace(name) && Enum.TryParse<FontAwesomeIcon>(name, true, out var icon)
			? icon
			: FontAwesomeIcon.None;
	}

	private static T? Read<T>(string resource) where T : class
	{
		try
		{
			using var stream = typeof(ChangelogData).Assembly.GetManifestResourceStream(resource);
			if (stream == null)
			{
				PluginLog.Warning($"Update notes resource {resource} is missing.");
				return null;
			}

			using var reader = new StreamReader(stream);
			return JsonConvert.DeserializeObject<T>(reader.ReadToEnd());
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to read update notes resource {resource}: {ex.Message}");
			return null;
		}
	}
}

internal sealed class ChangelogFile
{
	public string Tagline { get; set; } = string.Empty;

	public List<ChangelogEntry> Entries { get; set; } = [];
}

internal sealed class ChangelogEntry
{
	public string Version { get; set; } = string.Empty;

	public string Title { get; set; } = string.Empty;

	public string Date { get; set; } = string.Empty;

	public string? Message { get; set; }

	public List<ChangelogSection> Sections { get; set; } = [];

	[JsonIgnore]
	public Version? ParsedVersion { get; set; }

	[JsonIgnore]
	public string DisplayVersion => Version.Length > 0 && char.IsDigit(Version[0]) ? $"v{Version}" : Version;
}

internal sealed class ChangelogSection
{
	public string Title { get; set; } = string.Empty;

	public string Icon { get; set; } = string.Empty;

	public List<string> Items { get; set; } = [];

	[JsonIgnore]
	public FontAwesomeIcon ResolvedIcon { get; set; }
}

internal sealed class CreditsFile
{
	public List<CreditCategory> Categories { get; set; } = [];
}

internal sealed class CreditCategory
{
	public string Category { get; set; } = string.Empty;

	public string Icon { get; set; } = string.Empty;

	public List<CreditEntry> Items { get; set; } = [];

	[JsonIgnore]
	public FontAwesomeIcon ResolvedIcon { get; set; }
}

internal sealed class CreditEntry
{
	public string Name { get; set; } = string.Empty;
	public string Role { get; set; } = string.Empty;

	public string? Url { get; set; }
}
