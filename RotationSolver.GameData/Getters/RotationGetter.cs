using Lumina.Excel.Sheets;
using RotationSolver.GameData.Getters.Actions;

namespace RotationSolver.GameData.Getters;

internal class RotationGetter(Lumina.GameData gameData, ClassJob job)
{
	private readonly Lumina.GameData gameData = gameData;
	private readonly ClassJob job = job;

	/// <summary>
	/// Gets the name of the rotation.
	/// </summary>
	/// <returns>The name of the rotation.</returns>
	public string GetName() => $"{job.Name} Rotation".ToPascalCase();

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
     /// <summary>
     /// <see href="https://na.finalfantasyxiv.com/jobguide/{{jobName?.Replace(" ", "").ToLower()}}"><strong>{{jobName}}</strong></see>
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

     {{GetLBInRotation(job.LimitBreak1.Value, 1)}}
     {{GetLBInRotation(job.LimitBreak2.Value, 2)}}
     {{GetLBInRotation(job.LimitBreak3.Value, 3)}}
     {{GetLBInRotationPvP(FindPvPLimitBreak())}}

     #endregion

     #region Traits
     
     {{traitsCode.Table()}}

     {{Util.ArrayNames("AllTraits", "IBaseTrait",
		 "public override", [.. traitsGetter.AddedNames]).Table()}}
     #endregion
     }
     """;
	}

	private Lumina.Excel.Sheets.Action? FindPvPLimitBreak()
	{
		try
		{
			var actionSheet = gameData.GetExcelSheet<Lumina.Excel.Sheets.Action>();
			if (actionSheet == null)
			{
				return null;
			}

			var jobCategory = job.ClassJobCategory.Value;

			foreach (var action in actionSheet)
			{
				if (action.ActionCategory.RowId == 15 && action.ClassJobCategory.IsValid && action.IsPvP)
				{
					if (IsJobCompatible(action.ClassJobCategory.Value, job))
					{
						return action;
					}
				}
			}

			return null;
		}
		catch (Exception)
		{
			return null;
		}
	}

	private static bool IsJobCompatible(ClassJobCategory category, ClassJob targetJob)
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
		var transient = gameData.GetExcelSheet<ActionTransient>()?.GetRow(item.RowId);
		var desc = transient?.Description.ToString() ?? string.Empty;

		desc = Util.SanitizeXmlDescription(desc);

		return $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";
	}
}
