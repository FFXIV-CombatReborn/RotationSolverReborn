using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;

namespace RotationSolver.Basic.Configuration.Conditions;

internal class MajorConditionValue(string name = MajorConditionValue.conditionName)
{
	private const string conditionName = "Unnamed";

	[JsonIgnore]
	public bool IsUnnamed => Name == conditionName;

	/// <summary>
	/// Key for action id.
	/// </summary>
	public Dictionary<Job, Dictionary<uint, ConditionSet>> Conditions { get; } = [];

	[JsonIgnore]
	public Dictionary<uint, ConditionSet> ConditionDict
	{
		get
		{
			if (!Conditions.TryGetValue(DataCenter.Job, out Dictionary<uint, ConditionSet>? dict))
			{
				dict = Conditions[DataCenter.Job] = [];
			}
			return dict;
		}
	}

	public Dictionary<Job, Dictionary<uint, ConditionSet>> DisabledConditions { get; } = [];

	[JsonIgnore]
	public Dictionary<uint, ConditionSet> DisableConditionDict
	{
		get
		{
			if (!DisabledConditions.TryGetValue(DataCenter.Job, out Dictionary<uint, ConditionSet>? dict))
			{
				dict = DisabledConditions[DataCenter.Job] = [];
			}
			return dict;
		}
	}

	public Dictionary<string, ConditionSet> ForceEnableConditions { get; private set; } = [];

	public Dictionary<string, ConditionSet> ForceDisableConditions { get; private set; } = [];

	public ConditionSet HealAreaConditionSet { get; set; } = new();
	public ConditionSet HealSingleConditionSet { get; set; } = new();
	public ConditionSet DefenseAreaConditionSet { get; set; } = new();
	public ConditionSet DefenseSingleConditionSet { get; set; } = new();
	public ConditionSet DispelStancePositionalConditionSet { get; set; } = new();
	public ConditionSet RaiseShirkConditionSet { get; set; } = new();
	public ConditionSet MoveForwardConditionSet { get; set; } = new();
	public ConditionSet MoveBackConditionSet { get; set; } = new();
	public ConditionSet AntiKnockbackConditionSet { get; set; } = new();
	public ConditionSet SpeedConditionSet { get; set; } = new();
	public ConditionSet NoCastingConditionSet { get; set; } = new();
	public ConditionSet SwitchAutoConditionSet { get; set; } = new();
	public ConditionSet SwitchManualConditionSet { get; set; } = new();
	public ConditionSet SwitchCancelConditionSet { get; set; } = new();

	public (string Name, ConditionSet Condition)[] NamedConditions { get; set; } = [];

	public string Name = name;

	public ConditionSet GetCondition(uint id)
	{
		if (!ConditionDict.TryGetValue(id, out ConditionSet? conditionSet))
		{
			conditionSet = ConditionDict[id] = new ConditionSet();
		}
		return conditionSet;
	}

	public ConditionSet GetDisabledCondition(uint id)
	{
		if (!DisableConditionDict.TryGetValue(id, out ConditionSet? conditionSet))
		{
			conditionSet = DisableConditionDict[id] = new ConditionSet();
		}
		return conditionSet;
	}

	public ConditionSet GetEnableCondition(string config)
	{
		if (!ForceEnableConditions.TryGetValue(config, out ConditionSet? conditionSet))
		{
			conditionSet = ForceEnableConditions[config] = new ConditionSet();
		}
		return conditionSet;
	}

	public ConditionSet GetDisableCondition(string config)
	{
		if (!ForceDisableConditions.TryGetValue(config, out ConditionSet? conditionSet))
		{
			conditionSet = ForceDisableConditions[config] = new ConditionSet();
		}
		return conditionSet;
	}

	public void Save(string folder)
	{
		try
		{
			// Ensure the target directory exists at write-time (avoids races).
			Directory.CreateDirectory(folder);

			// Sanitize file name to avoid invalid characters.
			string safeName = string.IsNullOrWhiteSpace(Name) ? conditionName : Name;
			char[] invalids = Path.GetInvalidFileNameChars();
			foreach (char c in invalids)
			{
				safeName = safeName.Replace(c, '_');
			}

			string path = Path.Combine(folder, safeName + ".json");
			string str = JsonConvert.SerializeObject(this, Formatting.Indented);
			File.WriteAllText(path, str);
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to save condition set \"{Name}\" to \"{folder}\": {ex.Message}");
		}
	}

	public static MajorConditionValue[] Read(string folder)
	{
		if (!Directory.Exists(folder))
		{
			return [];
		}

		List<MajorConditionValue> result = [];

		string[] files = Directory.GetFiles(folder, "*.json");
		foreach (string p in files)
		{
			string str = File.ReadAllText(p);

			try
			{
				var obj = JsonConvert.DeserializeObject<MajorConditionValue>(str, new IConditionConverter());
				if (obj != null && !string.IsNullOrEmpty(obj.Name))
				{
					result.Add(obj);
				}
			}
			catch (Exception ex)
			{
				PluginLog.Warning($"Failed to load the types from {p}: {ex.Message}");
				Svc.Chat.Print($"Failed to load the ConditionSet from {p}");
			}
		}

		return result.ToArray();
	}
}