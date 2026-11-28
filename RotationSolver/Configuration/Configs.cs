using Dalamud.Configuration;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.Logging;
using RebornMaterial;
using System.Collections.Concurrent;
using static RotationSolver.Configuration.ConfigTypes;

namespace RotationSolver.Configuration;

internal partial class Configs : IPluginConfiguration, IM3Settings
{
	[JsonIgnore]
	public const string
		BasicTimer = "BasicTimer",
		BasicAutoSwitch = "BasicAutoSwitch",
		DutySpecificCrucibleOfTheUnbroken = "DutySpecificCrucibleOfTheUnbroken",
		DutySpecificTheMaskedCarnivale = "DutySpecificTheMaskedCarnivale",
		DutySpecificPvP = "DutySpecificPvP",
		DutySpecificFieldOps = "DutySpecificFieldOps",
		DutySpecificAlliance = "DutySpecificAlliance",
		DutySpecificDeepDungeon = "DutySpecificDeepDungeon",
		DutySpecificVariantDungeon = "DutySpecificVariantDungeon",
		DutySpecificTreasureDungeon = "DutySpecificTreasureDungeon",
		DutySpecificChaoticAlliance = "DutySpecificChaoticAlliance",
		DutySpecificDungeon = "DutySpecificDungeon",
		DutySpecificUltimate = "DutySpecificUltimate",
		DutySpecificExtreme = "DutySpecificExtreme",
		DutySpecificSavage = "DutySpecificSavage",
		BasicCommands = "BasicCommands",
		UiInformation = "UiInformation",
		UiCustomize = "UiCustomize",
		UiWindows = "UiWindows",
		UiHotbars = "UiHotbars",
		AutoActionUsage = "AutoActionUsage",
		AutoItemUsage = "AutoItemUsage",
		AutoIntercept = "AutoIntercept",
		HealingActionCondition = "HealingActionCondition",
		RaiseActionCondition = "RaiseActionCondition",
		TargetConfig = "TargetConfig",
		Extra = "Extra",
		ListCastingArea = "ListCastingArea",
		ListKnockback = "ListKnockback",
		Debug = "Debug";

	public const int CurrentVersion = 12;
	public int Version { get; set; } = CurrentVersion;

	public string LastSeenChangelog { get; set; } = "0.0.0.0";
	public bool TutorialDone { get; set; } = false;

	public AoEType StateWindowManualAoEType { get; set; } = AoEType.Cleave;

	public bool ControlWindowLinkAutoFull { get; set; } = false;
	public bool ControlWindowLinkManualCleave { get; set; } = false;

	public bool IsMainWindowLock { get; set; } = false;

	private const float OldDefaultSpacing = 1.5f;

	public bool SpacingDefaultUpdated { get; set; } = false;

	bool IM3Settings.UiDescriptionTooltips => !UiInlineDescriptions;

	public List<ActionEventInfo> Events { get; private set; } = [];
	public SortedSet<Job> DisabledJobs { get; private set; } = [];

	public string[] RotationLibs { get; set; } = [];
	public List<TargetingType> TargetingTypes { get; set; } = [];

	public MacroInfo DutyStart { get; set; } = new MacroInfo();
	public MacroInfo DutyEnd { get; set; } = new MacroInfo();

	#region Integer

	public int ActionSequencerIndex { get; set; }

	[UI("Modifier key that unlocks movement", Description = "RB is for gamepad players.", Parent = nameof(PoslockCasting))]
	public ConsoleModifiers PoslockModifier { get; set; }

	[Range(0, 10, ConfigUnitType.None)]
	public int TargetingIndex { get; set; }

	#endregion

	#region Jobs

	[JobConfig, UI("Use Lucid Dreaming below this MP", Filter = AutoActionUsage)]
	[Range(0, 10000, ConfigUnitType.None)]
	private readonly int _lucidDreamingMpThreshold = 6000;

	[JobConfig, Range(0, 1, ConfigUnitType.Percent)]
	private readonly float _healthAreaAbilityHot = 0.55f;

	[JobConfig, Range(0, 1, ConfigUnitType.Percent)]
	private readonly float _healthAreaSpellHot = 0.55f;

	[JobConfig, Range(0, 1, ConfigUnitType.Percent)]
	private readonly float _healthAreaAbility = 0.75f;

	[JobConfig, Range(0, 1, ConfigUnitType.Percent)]
	private readonly float _healthAreaSpell = 0.65f;

	[JobConfig, Range(0, 1, ConfigUnitType.Percent)]
	private readonly float _healthSingleAbilityHot = 0.65f;

	[JobConfig, Range(0, 1, ConfigUnitType.Percent)]
	private readonly float _healthSingleSpellHot = 0.55f;

	[JobConfig, Range(0, 1, ConfigUnitType.Percent)]
	private readonly float _healthSingleAbility = 0.7f;

	[JobConfig, Range(0, 1, ConfigUnitType.Percent)]
	private readonly float _healthSingleSpell = 0.65f;

	[JobConfig, Range(0, 1, ConfigUnitType.Percent, 0.02f)]
	[UI("Use invulnerability below this HP",
		Filter = AutoActionUsage, Section = 2,
		PvEFilter = JobFilterType.Tank, PvPFilter = JobFilterType.NoJob)]
	private readonly float _healthForDyingTanks = 0.15f;

	[JobConfig]
	private readonly string _PvPRotationChoice = string.Empty;

	[JobConfig]
	private readonly string _rotationChoice = string.Empty;
	#endregion

	[JobConfig]
	private readonly ConcurrentDictionary<uint, ActionConfig> _rotationActionConfig = new();

	[JobConfig]
	private readonly ConcurrentDictionary<uint, ItemConfig> _rotationItemConfig = new();

	[JobChoiceConfig]
	private readonly ConcurrentDictionary<string, string> _rotationConfigurations = new();

	public ConcurrentDictionary<uint, string> DutyRotationChoice = new();
	public ConcurrentDictionary<string, Dictionary<string, string>> DutyRotationConfigurations = new();

	private readonly Lock _saveLock = new();

	// CD Plan: picked plan file per "<timeline folder>|<job>".
	public ConcurrentDictionary<string, string> CdPlanChoice = new();

	// CD Plan timeline window locked in place.
	public bool CdPlanTimelineLock;

	// Read-only migration of the settings' earlier names (Beiruta...): loaded once, never written back.
	[JsonProperty("BeirutaPlanChoice")]
	private ConcurrentDictionary<string, string> LegacyPlanChoice
	{
		set
		{
			foreach (var (key, file) in value)
			{
				CdPlanChoice.TryAdd(key, file);
			}
		}
	}

	[JsonProperty("BeirutaPlanTimelineLock")]
	private bool LegacyPlanTimelineLock
	{
		set => CdPlanTimelineLock |= value;
	}

	public void Save()
	{
#if DEBUG
		PluginLog.Information("Saved configurations.");
#endif
		lock (_saveLock)
		{
			var path = Svc.PluginInterface.ConfigFile.FullName;
			var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";
			try
			{
				File.WriteAllText(tempPath, JsonConvert.SerializeObject(this, Formatting.Indented));
				File.Move(tempPath, path, true);
			}
			finally
			{
				if (File.Exists(tempPath))
				{
					try
					{
						File.Delete(tempPath);
					}
					catch
					{
					}
				}
			}
		}
	}

	internal void UpdateSpacingDefault()
	{
		if (SpacingDefaultUpdated)
		{
			return;
		}

		SpacingDefaultUpdated = true;
		if (UiPaddingScale == OldDefaultSpacing)
		{
			UiPaddingScale = 1f;
		}
	}

	public static Configs Migrate(Configs oldConfigs)
	{
		if (oldConfigs.Version != CurrentVersion)
		{
			return new Configs();
		}
		return oldConfigs;
	}

	public void Backup()
	{
		Save();
		File.Copy(Svc.PluginInterface.ConfigFile.FullName, Svc.PluginInterface.ConfigFile.Directory + "\\RotationSolver_Backup.json", true);
		Svc.Toasts.ShowNormal("Configs backed up.");
	}

	public void Restore()
	{
		var configDir = Svc.PluginInterface.ConfigFile.Directory?.FullName;
		if (string.IsNullOrEmpty(configDir))
		{
			return;
		}

		var backupPath = Path.Combine(configDir, "RotationSolver_Backup.json");
		if (!File.Exists(backupPath))
		{
			Svc.Toasts.ShowNormal("No backup configs file found.");
			return;
		}

		Configs? restoredConfigs;
		try
		{
			restoredConfigs = JsonConvert.DeserializeObject<Configs>(File.ReadAllText(backupPath));
		}
		catch (Exception ex)
		{
			PluginLog.Warning($"Failed to read backup configs: {ex.Message}");
			Svc.Toasts.ShowNormal("Backed up configs could not be read.");
			return;
		}

		if (restoredConfigs == null || restoredConfigs.Version != CurrentVersion)
		{
			Svc.Toasts.ShowNormal("Backed up configs are not compatible with the current version.");
			return;
		}

		var safetySavePath = Path.Combine(configDir, "RotationSolver_SafetySave.json");
		if (File.Exists(Svc.PluginInterface.ConfigFile.FullName))
		{
			File.Copy(Svc.PluginInterface.ConfigFile.FullName, safetySavePath, true);
		}

		Service.Config = restoredConfigs;
		restoredConfigs.Save();
		Svc.Toasts.ShowNormal("Configs restored. Closing to set.");
		DataCenter.HoldingRestore = true;
	}
}
