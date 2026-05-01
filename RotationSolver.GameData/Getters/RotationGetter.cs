using Lumina.Excel.Sheets;
using RotationSolver.GameData.Getters.Actions;

namespace RotationSolver.GameData.Getters;

/// <summary>
/// Provides methods to generate rotation code for a specific job.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="RotationGetter"/> class.
/// </remarks>
/// <param name="gameData">The game data.</param>
/// <param name="job">The job.</param>
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

	/// <summary>
	/// Finds the PvP Limit Break action for the current job.
	/// </summary>
	/// <returns>The PvP Limit Break action if found; otherwise, null.</returns>
	private Lumina.Excel.Sheets.Action? FindPvPLimitBreak()
	{
		try
		{
			var actionSheet = gameData.GetExcelSheet<Lumina.Excel.Sheets.Action>();
			if (actionSheet == null) return null;

			// Get the job's class job category
			var jobCategory = job.ClassJobCategory.Value;

			// Find PvP Limit Break actions (ActionCategory.RowId == 15)
			// Check each PvP LB to see if it matches this job
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
			// If any error occurs during lookup, return null
			return null;
		}
	}

	/// <summary>
	/// Checks if a job is compatible with a class job category.
	/// </summary>
	/// <param name="category">The class job category.</param>
	/// <param name="targetJob">The job to check.</param>
	/// <returns>True if compatible; otherwise, false.</returns>
	private static bool IsJobCompatible(ClassJobCategory category, ClassJob targetJob)
	{
		// Use a more reliable method to check job compatibility
		// This replaces the fragile reflection-based approach
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
			// "BST" => category.BST,
			_ => false
		};
	}

	/// <summary>
	/// Gets the jobs associated with the current job.
	/// </summary>
	/// <returns>A string representing the jobs.</returns>
	private string GetJobs()
	{
		var jobs = $"Job.{job.Abbreviation}";
		if (job.RowId != 28 && job.RowId != job.ClassJobParent.RowId)
		{
			jobs += $", Job.{(job.ClassJobParent.IsValid ? job.ClassJobParent.Value.Abbreviation.ToString() : "ADV")}";
		}
		return jobs;
	}

	/// <summary>
	/// Gets the job gauge code.
	/// </summary>
	/// <returns>The job gauge code.</returns>
	private string GetJobGauge() => (job.Abbreviation == "BLU" || job.Abbreviation == "BST") ? string.Empty : $"static {job.Abbreviation}Gauge JobGauge => Svc.Gauges.Get<{job.Abbreviation}Gauge>();";
	//TODO: Remove BST exception when the class releases to allow BST gauge data to be generated

	/// <summary>
	/// Gets the limit break code for a specific action.
	/// </summary>
	/// <param name="action">The action.</param>
	/// <param name="index">The index of the limit break.</param>
	/// <returns>The limit break code.</returns>
	private string GetLBInRotation(Lumina.Excel.Sheets.Action action, int index)
	{
		if (action.RowId == 0) return string.Empty;

		var code = GetLBPvE(action, out var name);

		return code + "\n" + $"""
            /// <summary>
            /// {action.GetDescName()}
            /// {GetDesc(action)}
            /// </summary>
            private sealed protected override IBaseAction LimitBreak{index} => {name};
            """;
	}

	/// <summary>
	/// Gets the PvE limit break code for a specific action.
	/// </summary>
	/// <param name="action">The action.</param>
	/// <param name="name">The name of the limit break.</param>
	/// <returns>The PvE limit break code.</returns>
	private string GetLBPvE(Lumina.Excel.Sheets.Action action, out string name)
	{
		name = $"{action.Name.ToString().ToPascalCase()}PvE";
		var descName = action.GetDescName();

		return action.ToCode(name, descName, GetDesc(action), false);
	}

	/// <summary>
	/// Gets the PvP limit break code for a specific action.
	/// </summary>
	/// <param name="action">The action.</param>
	/// <returns>The PvP limit break code.</returns>
	private string GetLBInRotationPvP(Lumina.Excel.Sheets.Action? action)
	{
		if (action == null || action.Value.RowId == 0) return string.Empty;

		var code = GetLBPvP(action.Value, out var name);

		return code + "\n" + $"""
            /// <summary>
            /// {action.Value.GetDescName()}
            /// {GetDesc(action.Value)}
            /// </summary>
            private sealed protected override IBaseAction LimitBreakPvP => {name};
            """;
	}

	/// <summary>
	/// Gets the PvP limit break code for a specific action.
	/// </summary>
	/// <param name="action">The action.</param>
	/// <param name="name">The name of the limit break.</param>
	/// <returns>The PvP limit break code.</returns>
	private string GetLBPvP(Lumina.Excel.Sheets.Action action, out string name)
	{
		name = $"{action.Name.ToString().ToPascalCase()}PvP";
		var descName = action.GetDescName();

		return action.ToCode(name, descName, GetDesc(action), false);
	}

	/// <summary>
	/// Gets the description of a specific action.
	/// </summary>
	/// <param name="item">The action.</param>
	/// <returns>The description of the action.</returns>
	private string GetDesc(Lumina.Excel.Sheets.Action item)
	{
		var transient = gameData.GetExcelSheet<ActionTransient>()?.GetRow(item.RowId);
		var desc = transient?.Description.ToString() ?? string.Empty;

		// Sanitize the description to remove invalid XML tags
		desc = Util.SanitizeXmlDescription(desc);

		return $"<para>{desc.Replace("\n", "</para>\n/// <para>")}</para>";
	}
}
