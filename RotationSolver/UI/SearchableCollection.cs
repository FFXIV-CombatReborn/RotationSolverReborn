using RotationSolver.Basic.Configuration;
using RotationSolver.UI.SearchableConfigs;
using System.Collections.Concurrent;

namespace RotationSolver.UI;

internal readonly record struct SearchPair(UIAttribute Attribute, ISearchable Searchable);

internal class SearchableCollection
{
    private readonly List<SearchPair> _items;
    private static readonly char[] _splitChar = [' ', ',', '、', '.', '。'];
    private const int MaxResultLength = 20;

    public SearchableCollection()
    {
        IEnumerable<PropertyInfo> properties = typeof(Configs).GetRuntimeProperties();
        int propertiesLength = 0;
        foreach (PropertyInfo _ in properties)
        {
            propertiesLength++;
        }

        List<SearchPair> pairs = new(propertiesLength);
        Dictionary<string, CheckBoxSearch> parents = new(propertiesLength);

        foreach (PropertyInfo property in properties)
        {
            UIAttribute? ui = property.GetCustomAttribute<UIAttribute>();
            if (ui == null)
            {
                continue;
            }

            ISearchable? item = CreateSearchable(property);
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

        foreach (SearchPair pair in pairs)
        {
            string parentName = pair.Attribute.Parent;
            if (string.IsNullOrEmpty(parentName) || !parents.TryGetValue(parentName, out CheckBoxSearch? parent))
            {
                _items.Add(pair);
            }
            else
            {
                parent.AddChild(pair.Searchable);
            }
        }
    }

    public void DrawItems(string filter)
    {
        bool isFirst = true;
        Dictionary<byte, List<SearchPair>> filteredItems = [];

        foreach (SearchPair item in _items)
        {
            if (item.Attribute.Filter == filter)
            {
                if (!filteredItems.TryGetValue(item.Attribute.Section, out List<SearchPair>? value))
                {
                    value = [];
                    filteredItems[item.Attribute.Section] = value;
                }

                value.Add(item);
            }
        }

        foreach (KeyValuePair<byte, List<SearchPair>> grp in filteredItems)
        {
            if (!isFirst)
            {
                ImGui.Separator();
            }

            List<SearchPair> items = grp.Value;
            // Simple insertion sort by Attribute.Order
            for (int i = 1; i < items.Count; i++)
            {
                SearchPair temp = items[i];
                int j = i - 1;
                while (j >= 0 && items[j].Attribute.Order > temp.Attribute.Order)
                {
                    items[j + 1] = items[j];
                    j--;
                }
                items[j + 1] = temp;
            }

            foreach (SearchPair item in items)
            {
                item.Searchable.Draw();
            }

            isFirst = false;
        }
    }

    public ISearchable[] SearchItems(string searchingText)
    {
        if (string.IsNullOrEmpty(searchingText))
        {
            return [];
        }

        HashSet<ISearchable> results = [];
        List<ISearchable> finalResults = new(MaxResultLength);

        foreach (SearchPair pair in _items)
        {
            foreach (ISearchable searchable in GetChildren(pair.Searchable))
            {
                ISearchable parent = GetParent(searchable);
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
            foreach (ISearchable child in c.Children)
            {
                foreach (ISearchable grandChild in GetChildren(child))
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

        string[] chars = text.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries);
        string[] keys = key.Split(_splitChar, StringSplitOptions.RemoveEmptyEntries);

        int startWithCount = 0;
        int containCount = 0;

        foreach (string c in chars)
        {
            foreach (string k in keys)
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