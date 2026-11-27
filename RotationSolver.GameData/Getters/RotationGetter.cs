using Lumina.Excel.Sheets;
using RotationSolver.GameData.Getters.Actions;

namespace RotationSolver.GameData.Getters;

internal class RotationGetter(Lumina.GameData gameData, ClassJob job, IReadOnlyList<Lumina.Excel.Sheets.Action>? pvpLimitBreaks = null)
{
	private readonly Lumina.GameData gameData = gameData;
	private readonly ClassJob job = job;
	private readonly IReadOnlyList<Lumina.Excel.Sheets.Action>? pvpLimitBreaks = pvpLimitBreaks;

	/// <summary>
	/// Gets the name of the rotation.
	/// </summary>
	/// <returns>The name of the rotation.</returns>
	public string GetName() => $"{job.Name} Rotation".ToPascalCase();

	/// <summary>
	/// Gets the namespace for this job rotation.
	/// </summary>
	/// <returns>The namespace name.</returns>
	public string GetNamespace() => job.Abbreviation.ToString() switch
	{
		"PLD" => "RotationSolver.Rotations.Tank.Paladin",
		"WAR" => "RotationSolver.Rotations.Tank.Warrior",
		"DRK" => "RotationSolver.Rotations.Tank.DarkKnight",
		"GNB" => "RotationSolver.Rotations.Tank.Gunbreaker",
		"WHM" => "RotationSolver.Rotations.Healer.WhiteMage",
		"SCH" => "RotationSolver.Rotations.Healer.Scholar",
		"AST" => "RotationSolver.Rotations.Healer.Astrologian",
		"SGE" => "RotationSolver.Rotations.Healer.Sage",
		"MNK" => "RotationSolver.Rotations.Melee.Monk",
		"DRG" => "RotationSolver.Rotations.Melee.Dragoon",
		"NIN" => "RotationSolver.Rotations.Melee.Ninja",
		"SAM" => "RotationSolver.Rotations.Melee.Samurai",
		"RPR" => "RotationSolver.Rotations.Melee.Reaper",
		"VPR" => "RotationSolver.Rotations.Melee.Viper",
		"BRD" => "RotationSolver.Rotations.Ranged.Bard",
		"MCH" => "RotationSolver.Rotations.Ranged.Machinist",
		"DNC" => "RotationSolver.Rotations.Ranged.Dancer",
		"BLM" => "RotationSolver.Rotations.Magical.BlackMage",
		"SMN" => "RotationSolver.Rotations.Magical.Summoner",
		"RDM" => "RotationSolver.Rotations.Magical.RedMage",
		"PCT" => "RotationSolver.Rotations.Magical.Pictomancer",
		"BLU" => "RotationSolver.Rotations.Limited.BlueMage",
		"BST" => "RotationSolver.Rotations.Limited.Beastmaster",
		_ => "RotationSolver.Rotations",
	};

	/// <summary>
	/// Generates the rotation code.
	/// </summary>
	/// <returns>The generated rotation code.</returns>
	public string GetCode()
	{
		var jobName = job.Name.ToString();
		var jobs = GetJobs();
		var jobGauge = GetJobGauge();
		var rotationsGetter = new ActionSingleRotationGetter(gameData, job);
		var traitsGetter = new TraitRotationGetter(gameData, job);

		var rotationsCode = rotationsGetter.GetCode();
		var traitsCode = traitsGetter.GetCode();

		return $$"""
     namespace {{GetNamespace()}}
     {
     /// <summary>
     /// <see href="https://na.finalfantasyxiv.com/jobguide/{{jobName?.Replace(" ", "").ToLowerInvariant()}}"><strong>{{jobName}}</strong></see>
     /// <br>Number of Actions: {{rotationsGetter.Count}}</br>
     /// <br>Number of Traits: {{traitsGetter.Count}}</br>
     /// </summary>
     [Jobs({{jobs}})]
     public abstract partial class {{GetName()}} : CustomRotation
     {
         {{jobGauge}}

     #region Actions
     {{rotationsCode.Table()}}

     {{Util.ArrayNames("AllBaseActions", "IBaseAction",
		 "public override", [.. rotationsGetter.AddedNames]).Table()}}

     {{GetLBInRotation(job.LimitBreak1.IsValid ? job.LimitBreak1.Value : default, 1)}}
     {{GetLBInRotation(job.LimitBreak2.IsValid ? job.LimitBreak2.Value : default, 2)}}
     {{GetLBInRotation(job.LimitBreak3.IsValid ? job.LimitBreak3.Value : default, 3)}}
     {{GetLBInRotationPvP(FindPvPLimitBreak())}}

     #endregion

     #region Traits
     
     {{traitsCode.Table()}}

     {{Util.ArrayNames("AllTraits", "IBaseTrait",
		 "public override", [.. traitsGetter.AddedNames]).Table()}}
     #endregion
     }
     }
     """;
	}

	internal static List<Lumina.Excel.Sheets.Action> CollectPvPLimitBreaks(Lumina.GameData gameData)
	{
		var result = new List<Lumina.Excel.Sheets.Action>();
		var actionSheet = gameData.GetExcelSheet<Lumina.Excel.Sheets.Action>();
		if (actionSheet == null)
		{
			return result;
		}

		foreach (var action in actionSheet)
		{
			if (action.ActionCategory.RowId == 15 && action.ClassJobCategory.IsValid && action.IsPvP)
			{
				result.Add(action);
			}
		}

		return result;
	}

	private Lumina.Excel.Sheets.Action? FindPvPLimitBreak()
	{
		var limitBreaks = pvpLimitBreaks ?? CollectPvPLimitBreaks(gameData);
		for (var i = 0; i < limitBreaks.Count; i++)
		{
			var action = limitBreaks[i];
			if (action.ClassJobCategory.IsValid && IsJobCompatible(action.ClassJobCategory.Value, job))
			{
				return action;
			}
		}

		return null;
	}

	internal static bool IsJobCompatible(ClassJobCategory category, ClassJob targetJob)
	{
		return targetJob.Abbreviation.ToString() switch
		{
			"PLD" => category.PLD,
			"WAR" => category.WAR,
			"DRK" => category.DRK,
			"GNB" => category.GNB,
			"WHM" => category.WHM,
			"SCH" => category.SCH,
			"AST" => category.AST,
			"SGE" => category.SGE,
			"MNK" => category.MNK,
			"DRG" => category.DRG,
			"NIN" => category.NIN,
			"SAM" => category.SAM,
			"RPR" => category.RPR,
			"VPR" => category.VPR,
			"BRD" => category.BRD,
			"MCH" => category.MCH,
			"DNC" => category.DNC,
			"BLM" => category.BLM,
			"SMN" => category.SMN,
			"RDM" => category.RDM,
			"PCT" => category.PCT,
			"BLU" => category.BLU,
			"BST" => category.Unknown0,
			_ => false
		};
	}

	private string GetJobs()
	{
		var jobs = $"Job.{job.Abbreviation}";
		if (job.RowId != 28 && job.RowId != job.ClassJobParent.RowId)
		{
			jobs += $", Job.{(job.ClassJobParent.IsValid ? job.ClassJobParent.Value.Abbreviation.ToString() : "ADV")}";
		}
		return jobs;
	}

	private string GetJobGauge() => (job.Abbreviation == "BLU" || job.Abbreviation == "BST") ? string.Empty : $"static {job.Abbreviation}Gauge JobGauge => Svc.Gauges.Get<{job.Abbreviation}Gauge>();";
	//TODO: Remove BST exception when the class releases to allow BST gauge data to be generated

	private string GetLBInRotation(Lumina.Excel.Sheets.Action action, int index)
	{
		if (action.RowId == 0)
		{
			return string.Empty;
		}

		var code = GetLBPvE(action, out var name);

		return code + "\n" + $"""
            /// <summary>
            /// {action.GetDescName()}
            /// {GetDesc(action)}
            /// </summary>
            private sealed protected override IBaseAction LimitBreak{index} => {name};
            """;
	}

	private string GetLBPvE(Lumina.Excel.Sheets.Action action, out string name)
	{
		name = $"{action.Name.ToString().ToPascalCase()}PvE";
		var descName = action.GetDescName();

		return action.ToCode(name, descName, GetDesc(action), false);
	}

	private string GetLBInRotationPvP(Lumina.Excel.Sheets.Action? action)
	{
		if (action == null || action.Value.RowId == 0)
		{
			return string.Empty;
		}

		var code = GetLBPvP(action.Value, out var name);

		return code + "\n" + $"""
            /// <summary>
            /// {action.Value.GetDescName()}
            /// {GetDesc(action.Value)}
            /// </summary>
            private sealed protected override IBaseAction LimitBreakPvP => {name};
            """;
	}

	private string GetLBPvP(Lumina.Excel.Sheets.Action action, out string name)
	{
		name = $"{action.Name.ToString().ToPascalCase()}PvP";
		var descName = action.GetDescName();

		return action.ToCode(name, descName, GetDesc(action), false);
	}

	private string GetDesc(Lumina.Excel.Sheets.Action item)
	{
		var transientSheet = gameData.GetExcelSheet<ActionTransient>();
		var desc = transientSheet != null && transientSheet.TryGetRow(item.RowId, out var transient)
			? transient.Description.ToString()
			: string.Empty;

		desc = Util.SanitizeXmlDescription(desc);

		return $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";
	}
}
