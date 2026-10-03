using ECommons.Logging;
using RotationSolver.UI.Material;

namespace RotationSolver.UI;

internal class CollapsingHeaderGroup(Dictionary<Func<string>, Action> headers)
{
	private static int _nextGroupId;

	private readonly Dictionary<Func<string>, Action> _headers = headers ?? throw new ArgumentNullException(nameof(headers));
	private readonly Dictionary<string, FontAwesomeIcon> _icons = [];
	private readonly int _groupId = Interlocked.Increment(ref _nextGroupId);
	private int _openedIndex = -1;

	public void AddCollapsingHeader(Func<string> name, Action action)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(action);

		_headers[name] = action;
	}

	public void ClearCollapsingHeader()
	{
		_headers.Clear();
		_icons.Clear();
	}

	public void SetHeaderIcon(string title, FontAwesomeIcon icon)
	{
		if (!string.IsNullOrEmpty(title))
		{
			_icons[title] = icon;
		}
	}

	public void OpenHeaderByTitle(string? title, bool ignoreCase = true)
	{
		if (string.IsNullOrEmpty(title))
		{
			return;
		}

		var idx = -1;
		foreach (var header in _headers)
		{
			idx++;
			var name = header.Key?.Invoke() ?? string.Empty;
			if (string.IsNullOrEmpty(name))
			{
				continue;
			}

			if (string.Equals(name, title, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
			{
				_openedIndex = idx;
				return;
			}
		}
	}

	public void Draw()
	{
		var index = -1;
		foreach (var header in _headers)
		{
			index++;

			if (header.Key is null || header.Value is null)
			{
				continue;
			}

			var name = header.Key();
			if (string.IsNullOrEmpty(name))
			{
				continue;
			}

			try
			{
				var expanded = index == _openedIndex;
				var wasExpanded = expanded;
				_ = _icons.TryGetValue(name, out var icon);

				using (var card = M3ExpandableCard.Begin($"section_{_groupId}_{index}", name, ref expanded, icon, AccentFor(index)))
				{
					if (card.Expanded)
					{
						header.Value();
					}
				}

				if (expanded != wasExpanded)
				{
					_openedIndex = expanded ? index : -1;
				}
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"An error occurred while drawing the header: {ex.Message}");
			}
		}
	}

	private static Vector4 AccentFor(int index)
	{
		var scheme = M3.Scheme;
		return (index % 3) switch
		{
			0 => scheme.Primary,
			1 => scheme.Tertiary,
			_ => scheme.Secondary,
		};
	}
}
