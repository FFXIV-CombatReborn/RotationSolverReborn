﻿using Lumina.Excel.GeneratedSheets;
using Action = Lumina.Excel.GeneratedSheets.Action;

namespace RotationSolver.GameData.Getters.Actions;

/// <summary>
/// Abstract base class for getting action rows from the Excel sheet.
/// </summary>
internal abstract class ActionGetterBase : ExcelRowGetter<Action>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ActionGetterBase"/> class.
    /// </summary>
    /// <param name="gameData">The game data.</param>
    protected ActionGetterBase(Lumina.GameData gameData) : base(gameData) { }

    /// <summary>
    /// Gets the list of added names.
    /// </summary>
    public List<string> AddedNames { get; } = new();

    private string[] _notCombatJobs = Array.Empty<string>();

    /// <summary>
    /// Called before creating the list of items.
    /// </summary>
    protected override void BeforeCreating()
    {
        AddedNames.Clear();
        _notCombatJobs = _gameData.GetExcelSheet<ClassJob>()!
            .Where(c => c.ClassJobCategory.Row is 32 or 33)
            .Select(c => c.Abbreviation.RawString)
            .ToArray();
        base.BeforeCreating();
    }

    /// <summary>
    /// Determines whether the specified action should be added to the list.
    /// </summary>
    /// <param name="item">The action to check.</param>
    /// <returns>True if the action should be added; otherwise, false.</returns>
    protected override bool AddToList(Action item)
    {
        if (item.RowId is 3 or 120) return true; // Sprint and cure.
        if (item.ClassJobCategory.Row == 0) return false;

        var name = item.Name.RawString;
        if (string.IsNullOrEmpty(name)) return false;
        if (!name.All(char.IsAscii)) return false;
        if (item.Icon is 0 or 405) return false;

        if (item.ActionCategory.Row is 6 or 7 or 8 or 12 or > 14 or 9) return false;

        var category = item.ClassJobCategory.Value;
        if (category == null) return false;

        if (category.RowId == 1) return true;

        if (_notCombatJobs.Any(jobName => (bool?)category.GetType().GetRuntimeProperty(jobName)?.GetValue(category) ?? false))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the name of the specified action.
    /// </summary>
    /// <param name="item">The action.</param>
    /// <returns>The name of the action.</returns>
    protected string GetName(Action item)
    {
        var name = item.Name.RawString.ToPascalCase() + (item.IsPvP ? "PvP" : "PvE");

        if (AddedNames.Contains(name))
        {
            name += "_" + item.RowId;
        }
        else
        {
            AddedNames.Add(name);
        }
        return name;
    }

    /// <summary>
    /// Gets the description of the specified action.
    /// </summary>
    /// <param name="item">The action.</param>
    /// <returns>The description of the action.</returns>
    protected string GetDesc(Action item)
    {
        var desc = _gameData.GetExcelSheet<ActionTransient>()?.GetRow(item.RowId)?.Description.RawString ?? string.Empty;
        return $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";
    }
}