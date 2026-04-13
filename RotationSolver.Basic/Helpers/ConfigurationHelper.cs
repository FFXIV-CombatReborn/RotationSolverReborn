using Dalamud.Game.ClientState.Keys;
using ECommons.ExcelServices;
using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Helpers;

internal static class ConfigurationHelper
{
	public static readonly SortedList<ActionID, EnemyPositional> ActionPositional = new()
	{
		{ ActionID.FangAndClawPvE, EnemyPositional.Flank },
		{ ActionID.WheelingThrustPvE, EnemyPositional.Rear },
		{ ActionID.ChaosThrustPvE, EnemyPositional.Rear },
		{ ActionID.ChaoticSpringPvE, EnemyPositional.Rear },
		{ ActionID.DemolishPvE, EnemyPositional.Rear },
		{ ActionID.SnapPunchPvE, EnemyPositional.Flank },
		{ ActionID.PouncingCoeurlPvE, EnemyPositional.Flank },
		{ ActionID.TrickAttackPvE, EnemyPositional.Rear },
		{ ActionID.AeolianEdgePvE, EnemyPositional.Rear },
		{ ActionID.ArmorCrushPvE, EnemyPositional.Flank },
		{ ActionID.GibbetPvE, EnemyPositional.Flank },
		{ ActionID.ExecutionersGibbetPvE, EnemyPositional.Flank },
		{ ActionID.GallowsPvE, EnemyPositional.Rear },
		{ ActionID.ExecutionersGallowsPvE, EnemyPositional.Rear },
		{ ActionID.GekkoPvE, EnemyPositional.Rear },
		{ ActionID.KashaPvE, EnemyPositional.Flank },
		{ ActionID.FlankstingStrikePvE, EnemyPositional.Flank },
		{ ActionID.FlanksbaneFangPvE, EnemyPositional.Flank },
		{ ActionID.HindstingStrikePvE, EnemyPositional.Rear },
		{ ActionID.HindsbaneFangPvE, EnemyPositional.Rear },
		{ ActionID.HuntersCoilPvE, EnemyPositional.Flank },
		{ ActionID.SwiftskinsCoilPvE, EnemyPositional.Rear }
	};

	public static readonly uint[] BadStatus =
	[
		583, // No items
        582, // Item on cooldown
        581, // Unable to use item
        579, // Between Area
        574, // Wrong Job
        573, // Not learned or not high enough level
        572, // Unable to use action
    ];

	public static readonly uint[] BadStatusGCD =
	[
		583, // Action not ready
        579, // Between Area
        574, // Wrong Job
        573, // Not learned or not high enough level
        572, // Unable to use action
        5209, // Duty Action out of charges
        9301, // BLU action not in slot
    ];

	public static readonly uint[] BadStatusAbility =
	[
		583, // Action not ready
        579, // Between Area
        574, // Wrong Job
        573, // Not learned or not high enough level
        572, // Unable to use action
        5209, // Duty Action out of charges
        9301, // BLU action not in slot
    ];

	public static VirtualKey ToVirtual(this ConsoleModifiers modifiers)
	{
		return modifiers switch
		{
			ConsoleModifiers.Alt => VirtualKey.MENU,
			ConsoleModifiers.Shift => VirtualKey.SHIFT,
			_ => VirtualKey.CONTROL,
		};
	}

	public static bool DoesJobMatchCategory(this ClassJobCategory cat, Job job)
	{
		if (job == Job.ADV && cat.ADV)
		{
			return true;
		}

		if (job == Job.GLA && cat.GLA)
		{
			return true;
		}

		if (job == Job.PGL && cat.PGL)
		{
			return true;
		}

		if (job == Job.MRD && cat.MRD)
		{
			return true;
		}

		if (job == Job.LNC && cat.LNC)
		{
			return true;
		}

		if (job == Job.ARC && cat.ARC)
		{
			return true;
		}

		if (job == Job.CNJ && cat.CNJ)
		{
			return true;
		}

		if (job == Job.THM && cat.THM)
		{
			return true;
		}

		if (job == Job.CRP && cat.CRP)
		{
			return true;
		}

		if (job == Job.BSM && cat.BSM)
		{
			return true;
		}

		if (job == Job.ARM && cat.ARM)
		{
			return true;
		}

		if (job == Job.GSM && cat.GSM)
		{
			return true;
		}

		if (job == Job.LTW && cat.LTW)
		{
			return true;
		}

		if (job == Job.WVR && cat.WVR)
		{
			return true;
		}

		if (job == Job.ALC && cat.ALC)
		{
			return true;
		}

		if (job == Job.CUL && cat.CUL)
		{
			return true;
		}

		if (job == Job.MIN && cat.MIN)
		{
			return true;
		}

		if (job == Job.BTN && cat.BTN)
		{
			return true;
		}

		if (job == Job.FSH && cat.FSH)
		{
			return true;
		}

		if (job == Job.PLD && cat.PLD)
		{
			return true;
		}

		if (job == Job.MNK && cat.MNK)
		{
			return true;
		}

		if (job == Job.WAR && cat.WAR)
		{
			return true;
		}

		if (job == Job.DRG && cat.DRG)
		{
			return true;
		}

		if (job == Job.BRD && cat.BRD)
		{
			return true;
		}

		if (job == Job.WHM && cat.WHM)
		{
			return true;
		}

		if (job == Job.BLM && cat.BLM)
		{
			return true;
		}

		if (job == Job.ACN && cat.ACN)
		{
			return true;
		}

		if (job == Job.SMN && cat.SMN)
		{
			return true;
		}

		if (job == Job.SCH && cat.SCH)
		{
			return true;
		}

		if (job == Job.ROG && cat.ROG)
		{
			return true;
		}

		if (job == Job.NIN && cat.NIN)
		{
			return true;
		}

		if (job == Job.MCH && cat.MCH)
		{
			return true;
		}

		if (job == Job.DRK && cat.DRK)
		{
			return true;
		}

		if (job == Job.AST && cat.AST)
		{
			return true;
		}

		if (job == Job.SAM && cat.SAM)
		{
			return true;
		}

		if (job == Job.RDM && cat.RDM)
		{
			return true;
		}

		if (job == Job.BLU && cat.BLU)
		{
			return true;
		}

		if (job == Job.GNB && cat.GNB)
		{
			return true;
		}

		if (job == Job.DNC && cat.DNC)
		{
			return true;
		}

		if (job == Job.RPR && cat.RPR)
		{
			return true;
		}

		return job == Job.SGE && cat.SGE || (job == Job.VPR && cat.VPR) || (job == Job.PCT && cat.PCT);
	}
}
