using Dalamud.Game.Gui.Dtr;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using RotationSolver.Commands;

namespace RotationSolver.Updaters;

internal static class MiscUpdater
{
	internal static void UpdateMisc()
	{
		UpdateEntry();
		CancelCastUpdater.UpdateCancelCast();
	}

	private static IDtrBarEntry? _dtrEntry;

	internal static void UpdateEntry()
	{
		string showStr = RSCommands.EntryString;
		BitmapFontIcon icon = GetJobIcon(Player.Job);

		if (Service.Config.ShowInfoOnDtr && !string.IsNullOrEmpty(showStr))
		{
			try
			{
				_dtrEntry ??= Svc.DtrBar.Get("Rotation Solver Reborn");
			}
			catch
			{
				BasicWarningHelper.AddSystemWarning("Unable to add server bar entry");
				return;
			}

			if (_dtrEntry != null && !_dtrEntry.Shown)
			{
				_dtrEntry.Shown = true;
			}

			if (_dtrEntry != null)
			{
				_dtrEntry.Text = new SeString(
					new IconPayload(icon),
					new TextPayload(showStr)
				);

				if (Service.Config.DTRType == DTRType.DTRNormal)
				{
					_dtrEntry.OnClick = _ => RSCommands.CycleStateWithOneTargetTypes();
				}
				else if (Service.Config.DTRType == DTRType.DTRAllAuto)
				{
					_dtrEntry.OnClick = _ => RSCommands.CycleStateWithAllTargetTypes();
				}
				else if (Service.Config.DTRType == DTRType.DTRAuto)
				{
					_dtrEntry.OnClick = _ => RSCommands.CycleStateAuto();
				}
				else if (Service.Config.DTRType == DTRType.DTRManual)
				{
					_dtrEntry.OnClick = _ => RSCommands.CycleStateManual();
				}
				else if (Service.Config.DTRType == DTRType.DTRManualAuto)
				{
					_dtrEntry.OnClick = _ => RSCommands.CycleStateManualAuto();
				}
			}
		}
		else if (_dtrEntry != null && _dtrEntry.Shown)
		{
			_dtrEntry.Shown = false;
		}
	}

	private static BitmapFontIcon GetJobIcon(Job job)
	{
		return job switch
		{
			Job.WAR => BitmapFontIcon.Warrior,
			Job.PLD => BitmapFontIcon.Paladin,
			Job.DRK => BitmapFontIcon.DarkKnight,
			Job.GNB => BitmapFontIcon.Gunbreaker,
			Job.AST => BitmapFontIcon.Astrologian,
			Job.WHM => BitmapFontIcon.WhiteMage,
			Job.SGE => BitmapFontIcon.Sage,
			Job.SCH => BitmapFontIcon.Scholar,
			Job.BLM => BitmapFontIcon.BlackMage,
			Job.SMN => BitmapFontIcon.Summoner,
			Job.RDM => BitmapFontIcon.RedMage,
			Job.PCT => BitmapFontIcon.Pictomancer,
			Job.BLU => BitmapFontIcon.BlueMage,
			Job.MNK => BitmapFontIcon.Monk,
			Job.SAM => BitmapFontIcon.Samurai,
			Job.DRG => BitmapFontIcon.Dragoon,
			Job.RPR => BitmapFontIcon.Reaper,
			Job.NIN => BitmapFontIcon.Ninja,
			Job.VPR => BitmapFontIcon.Viper,
			Job.BRD => BitmapFontIcon.Bard,
			Job.MCH => BitmapFontIcon.Machinist,
			Job.DNC => BitmapFontIcon.Dancer,
			Job.BSM => BitmapFontIcon.Blacksmith,
			Job.ARM => BitmapFontIcon.Armorer,
			Job.WVR => BitmapFontIcon.Weaver,
			Job.ALC => BitmapFontIcon.Alchemist,
			Job.CRP => BitmapFontIcon.Carpenter,
			Job.LTW => BitmapFontIcon.Leatherworker,
			Job.CUL => BitmapFontIcon.Culinarian,
			Job.GSM => BitmapFontIcon.Goldsmith,
			Job.FSH => BitmapFontIcon.Fisher,
			Job.MIN => BitmapFontIcon.Miner,
			Job.BTN => BitmapFontIcon.Botanist,
			Job.GLA => BitmapFontIcon.Gladiator,
			Job.CNJ => BitmapFontIcon.Conjurer,
			Job.MRD => BitmapFontIcon.Marauder,
			Job.PGL => BitmapFontIcon.Pugilist,
			Job.LNC => BitmapFontIcon.Lancer,
			Job.ROG => BitmapFontIcon.Rogue,
			Job.ARC => BitmapFontIcon.Archer,
			Job.THM => BitmapFontIcon.Thaumaturge,
			Job.ACN => BitmapFontIcon.Arcanist,
			//Job.BST => BitmapFontIcon.Beastmaster,
			_ => BitmapFontIcon.ExclamationRectangle,
		};
	}

	internal static void PulseActionBar(uint actionID)
	{
		LoopAllSlotBar((bar, hot, index) =>
		{
			return IsActionSlotRight(bar, hot, actionID);
		});
	}

	private static bool IsActionSlotRight(ActionBarSlot slot, RaptureHotbarModule.HotbarSlot? hot, uint actionID)
	{
		if (hot.HasValue)
		{
			if (hot.Value.OriginalApparentSlotType is not RaptureHotbarModule.HotbarSlotType.CraftAction and not RaptureHotbarModule.HotbarSlotType.Action)
			{
				return false;
			}

			if (hot.Value.ApparentSlotType is not RaptureHotbarModule.HotbarSlotType.CraftAction and not RaptureHotbarModule.HotbarSlotType.Action)
			{
				return false;
			}

			if (hot.Value.OriginalApparentSlotType == RaptureHotbarModule.HotbarSlotType.Macro)
			{
				return false;
			}

			if (hot.Value.ApparentSlotType == RaptureHotbarModule.HotbarSlotType.Macro)
			{
				return false;
			}
		}

		return Service.GetAdjustedActionId((uint)slot.ActionId) == actionID;
	}

	private delegate bool ActionBarAction(ActionBarSlot bar, RaptureHotbarModule.HotbarSlot? hot, uint highLightID);
	private unsafe delegate bool ActionBarPredicate(ActionBarSlot bar, RaptureHotbarModule.HotbarSlot* hot);
	private static unsafe void LoopAllSlotBar(ActionBarAction doingSomething)
	{
		int index = 0;
		int hotBarIndex = 0;

		List<nint> addonPtrs =
		[
			.. Service.GetAddons<AddonActionBar>(),
			.. Service.GetAddons<AddonActionBarX>(),
			.. Service.GetAddons<AddonActionCross>(),
			.. Service.GetAddons<AddonActionDoubleCrossBase>(),
		];

		foreach (nint intPtr in addonPtrs)
		{
			if (intPtr == IntPtr.Zero)
			{
				continue;
			}

			AddonActionBarBase* actionBar = (AddonActionBarBase*)intPtr;
			RaptureHotbarModule.Hotbar hotBar = Framework.Instance()->GetUIModule()->GetRaptureHotbarModule()->Hotbars[hotBarIndex];

			int slotIndex = 0;

			foreach (ActionBarSlot slot in actionBar->ActionBarSlotVector.AsSpan())
			{
				int highLightId = 0x53550000 + index;

				if (doingSomething(slot, hotBarIndex > 9 ? null : hotBar.Slots[slotIndex], (uint)highLightId))
				{
					FFXIVClientStructs.FFXIV.Component.GUI.AtkComponentNode* iconAddon = slot.Icon;
					if ((IntPtr)iconAddon == IntPtr.Zero)
					{
						continue;
					}

					if (!iconAddon->AtkResNode.IsVisible())
					{
						continue;
					}

					actionBar->PulseActionBarSlot(slotIndex);
					UIGlobals.PlaySoundEffect(12);
				}
				slotIndex++;
				index++;
			}
			hotBarIndex++;
		}
	}

	public static void Dispose()
	{
		if (_dtrEntry?.Title != null)
		{
			Svc.DtrBar.Remove(_dtrEntry.Title);
		}
	}
}