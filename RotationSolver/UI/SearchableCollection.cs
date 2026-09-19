using RotationSolver.Basic.Configuration;
using RotationSolver.UI.SearchableConfigs;

namespace RotationSolver.UI;

internal readonly record struct SearchPair(UIAttribute Attribute, ISearchable Searchable);

internal class SearchableCollection
{
	private readonly List<SearchPair> _items;
	private static readonly char[] _splitChar = [' ', ',', '、', '.', '。'];
	private const int MaxResultLength = 20;

	public SearchableCollection()
	{
		var properties = typeof(Configs).GetRuntimeProperties();
		var propertiesLength = 0;
		foreach (var _ in properties)
		{
			propertiesLength++;
		}

		List<SearchPair> pairs = new(propertiesLength);
		Dictionary<string, CheckBoxSearch> parents = new(propertiesLength);

		foreach (var property in properties)
		{
			var ui = property.GetCustomAttribute<UIAttribute>();
			if (ui == null)
			{
				continue;
			}

			var item = CreateSearchable(property);
			if (item == null)
			{
				continue;
			}

			item.PvEFilter = new(ui.PvEFilter);
			item.PvPFilter = new(ui.PvPFilter);

			pairs.Add(new(ui, item));

			if (item is CheckBoxSearch search)
			{
				parents[property.Name] = search;
			}
		}

		_items = new List<SearchPair>(pairs.Count);

		foreach (var pair in pairs)
		{
			var parentName = pair.Attribute.Parent;
			if (string.IsNullOrEmpty(parentName) || !parents.TryGetValue(parentName, out var parent))
			{
				_items.Add(pair);
			}
			else
			{
				parent.AddChild(pair.Searchable);
			}
		}
	}

	private readonly Dictionary<string, List<List<SearchPair>>> _sectionsByFilter = [];

	public void DrawItems(string filter)
	{
		var isFirst = true;
		foreach (var items in GetSections(filter))
		{
			if (!isFirst)
			{
				ImGui.Separator();
			}

			foreach (var item in items)
			{
				item.Searchable.Draw();
			}

			isFirst = false;
		}
	}

	private List<List<SearchPair>> GetSections(string filter)
	{
		if (_sectionsByFilter.TryGetValue(filter, out var sections))
		{
			return sections;
		}

		Dictionary<byte, List<SearchPair>> filteredItems = [];
		foreach (var item in _items)
		{
			if (item.Attribute.Filter == filter)
			{
				if (!filteredItems.TryGetValue(item.Attribute.Section, out var value))
				{
					value = [];
					filteredItems[item.Attribute.Section] = value;
				}

				value.Add(item);
			}
		}

		sections = new(filteredItems.Count);
		foreach (var grp in filteredItems)
		{
			var items = grp.Value;
			// Simple insertion sort by Attribute.Order (stable, so equal orders keep declaration order)
			for (var i = 1; i < items.Count; i++)
			{
				var temp = items[i];
				var j = i - 1;
				while (j >= 0 && items[j].Attribute.Order > temp.Attribute.Order)
				{
					items[j + 1] = items[j];
					j--;
				}
				items[j + 1] = temp;
			}

			sections.Add(items);
		}

		_sectionsByFilter[filter] = sections;
		return sections;
	}

	public ISearchable[] SearchItems(string searchingText)
	{
		if (string.IsNullOrEmpty(searchingText))
		{
			return [];
		}

		HashSet<ISearchable> results = [];
		List<ISearchable> finalResults = new(MaxResultLength);

		foreach (var pair in _items)
		{
			foreach (var searchable in GetChildren(pair.Searchable))
			{
				var parent = GetParent(searchable);
				if (results.Contains(parent))
				{
					continue;
				}

				if (Similarity(searchable.SearchingKeys, searchingText) > 0)
				{
					_ = results.Add(parent);
					finalResults.Add(parent);
					if (finalResults.Count >= MaxResultLength)
					{
						break;
					}
				}
			}
		}

		return [.. finalResults];
	}

	private static ISearchable? CreateSearchable(PropertyInfo property)
	{
		if (property.Name == nameof(Configs.AutoHeal))
		{
			return new AutoHealCheckBox(property);
		}
		else if (property.PropertyType.IsEnum)
		{
			return new EnumSearch(property);
		}
		else if (property.PropertyType == typeof(bool))
		{
			return new CheckBoxSearchNoCondition(property);
		}
		else if (property.PropertyType == typeof(ConditionBoolean))
		{
			return new CheckBoxCondition(property);
		}
		else if (property.PropertyType == typeof(float))
		{
			return new DragFloatSearch(property);
		}
		else if (property.PropertyType == typeof(int))
		{
			return new DragIntSearch(property);
		}
		else if (property.PropertyType == typeof(Vector2))
		{
			return new DragFloatRangeSearch(property);
		}
		else if (property.PropertyType == typeof(Vector2Int))
		{
			return new DragIntRangeSearch(property);
		}
		else
		{
			return property.PropertyType == typeof(Vector4) ? new ColorEditSearch(property) : (ISearchable?)null;
		}
	}

	private static IEnumerable<ISearchable> GetChildren(ISearchable searchable)
	{
		yield return searchable;

		if (searchable is CheckBoxSearch c && c.Children != null)
		{
			foreach (var child in c.Children)
			{
				foreach (var grandChild in GetChildren(child))
				{
					yield return grandChild;
				}
			}
		}
	}

	private static ISearchable GetParent(ISearchable searchable)
	{
		return searchable.Parent == null ? searchable : GetParent(searchable.Parent);
	}

	public static float Similarity(string text, string key)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}

		var chars = text.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries);
		var keys = key.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries);

		var startWithCount = 0;
		var containCount = 0;

		foreach (var c in chars)
		{
			foreach (var k in keys)
			{
				if (c.StartsWith(k, StringComparison.OrdinalIgnoreCase))
				{
					startWithCount++;
				}
				else if (c.Contains(k, StringComparison.OrdinalIgnoreCase))
				{
					containCount++;
				}
			}
		}

		return (startWithCount * 3) + containCount;
	}
}