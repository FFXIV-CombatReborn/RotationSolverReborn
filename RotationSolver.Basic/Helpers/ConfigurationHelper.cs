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
		return job switch
		{
			Job.ADV => cat.ADV,
			Job.GLA => cat.GLA,
			Job.PGL => cat.PGL,
			Job.MRD => cat.MRD,
			Job.LNC => cat.LNC,
			Job.ARC => cat.ARC,
			Job.CNJ => cat.CNJ,
			Job.THM => cat.THM,
			Job.CRP => cat.CRP,
			Job.BSM => cat.BSM,
			Job.ARM => cat.ARM,
			Job.GSM => cat.GSM,
			Job.LTW => cat.LTW,
			Job.WVR => cat.WVR,
			Job.ALC => cat.ALC,
			Job.CUL => cat.CUL,
			Job.MIN => cat.MIN,
			Job.BTN => cat.BTN,
			Job.FSH => cat.FSH,
			Job.PLD => cat.PLD,
			Job.MNK => cat.MNK,
			Job.WAR => cat.WAR,
			Job.DRG => cat.DRG,
			Job.BRD => cat.BRD,
			Job.WHM => cat.WHM,
			Job.BLM => cat.BLM,
			Job.ACN => cat.ACN,
			Job.SMN => cat.SMN,
			Job.SCH => cat.SCH,
			Job.ROG => cat.ROG,
			Job.NIN => cat.NIN,
			Job.MCH => cat.MCH,
			Job.DRK => cat.DRK,
			Job.AST => cat.AST,
			Job.SAM => cat.SAM,
			Job.RDM => cat.RDM,
			Job.BLU => cat.BLU,
			Job.GNB => cat.GNB,
			Job.DNC => cat.DNC,
			Job.RPR => cat.RPR,
			Job.SGE => cat.SGE,
			Job.VPR => cat.VPR,
			Job.PCT => cat.PCT,
			Job.BST => cat.Unknown0,
			_ => false,
		};
	}
}
