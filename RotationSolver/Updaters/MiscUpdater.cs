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
using RotationSolver.UI.HighlightTeachingMode;

namespace RotationSolver.Updaters;

internal static class MiscUpdater
{
	internal static void UpdateMisc()
	{
		UpdateEntry();
		CancelCastUpdater.UpdateCancelCast();
	}

	private static IDtrBarEntry? _dtrEntry;
	private static string? _dtrText;
	private static BitmapFontIcon _dtrIcon;
	private static DTRType? _dtrClickType;

	internal static void UpdateEntry()
	{
		if (!Service.Config.ShowInfoOnDtr)
		{
			HideEntry();
			return;
		}

		var showStr = RSCommands.EntryString;
		if (string.IsNullOrEmpty(showStr))
		{
			HideEntry();
			return;
		}

		try
		{
			_dtrEntry ??= Svc.DtrBar.Get("Rotation Solver Reborn");
		}
		catch
		{
			BasicWarningHelper.AddSystemWarning("Unable to add server bar entry");
			return;
		}

		if (_dtrEntry == null)
		{
			return;
		}

		if (!_dtrEntry.Shown)
		{
			_dtrEntry.Shown = true;
		}

		var icon = GetJobIcon(Player.Job);
		if (showStr != _dtrText || icon != _dtrIcon)
		{
			_dtrEntry.Text = new SeString(
				new IconPayload(icon),
				new TextPayload(showStr)
			);
			_dtrText = showStr;
			_dtrIcon = icon;
		}

		var dtrType = Service.Config.DTRType;
		if (dtrType != _dtrClickType)
		{
			_dtrEntry.OnClick = dtrType switch
			{
				DTRType.DTRNormal => _ => RSCommands.CycleStateWithOneTargetTypes(),
				DTRType.DTRAllAuto => _ => RSCommands.CycleStateWithAllTargetTypes(),
				DTRType.DTRAuto => _ => RSCommands.CycleStateAuto(),
				DTRType.DTRManual => _ => RSCommands.CycleStateManual(),
				DTRType.DTRManualAuto => _ => RSCommands.CycleStateManualAuto(),
				_ => _dtrEntry.OnClick,
			};
			_dtrClickType = dtrType;
		}
	}

	private static void HideEntry()
	{
		if (_dtrEntry != null && _dtrEntry.Shown)
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
			Job.BST => BitmapFontIcon.Beastmaster,
			_ => BitmapFontIcon.ExclamationRectangle,
		};
	}

	internal readonly record struct ActionBarSlotRef(int AddonIndex, int SlotIndex);

	internal static unsafe void FindActionBarSlots(uint actionID, List<ActionBarSlotRef> slots)
	{
		slots.Clear();

		var framework = Framework.Instance();
		var uiModule = framework == null ? null : framework->GetUIModule();
		var raptureModule = uiModule == null ? null : uiModule->GetRaptureHotbarModule();
		if (raptureModule == null)
		{
			return;
		}

		for (var addonIndex = 0; addonIndex < HotbarAddonHelper.AddonCount; addonIndex++)
		{
			var addon = HotbarAddonHelper.GetHotbarAddon(addonIndex);
			if (addon == nint.Zero)
			{
				continue;
			}

			var hotSlots = HotbarAddonHelper.TryGetRaptureHotbarIndex(addon, addonIndex, raptureModule, out var hotBarIndex)
				? raptureModule->Hotbars[hotBarIndex].Slots
				: default;

			var slotIndex = -1;
			foreach (var slot in ((AddonActionBarBase*)addon)->ActionBarSlotVector.AsSpan())
			{
				slotIndex++;

				RaptureHotbarModule.HotbarSlot? hotSlot = slotIndex < hotSlots.Length ? hotSlots[slotIndex] : null;
				if (slot.Icon != null && IsActionSlotRight(slot, hotSlot, actionID))
				{
					slots.Add(new ActionBarSlotRef(addonIndex, slotIndex));
				}
			}
		}
	}

	internal static unsafe void PulseActionBarSlots(List<ActionBarSlotRef> slots)
	{
		foreach (var (addonIndex, slotIndex) in slots)
		{
			var actionBar = (AddonActionBarBase*)HotbarAddonHelper.GetHotbarAddon(addonIndex);
			if (actionBar == null)
			{
				continue;
			}

			var barSlots = actionBar->ActionBarSlotVector.AsSpan();
			if (slotIndex >= barSlots.Length)
			{
				continue;
			}

			var iconAddon = barSlots[slotIndex].Icon;
			if (iconAddon != null && iconAddon->AtkResNode.IsVisible())
			{
				actionBar->PulseActionBarSlot(slotIndex);
				UIGlobals.PlaySoundEffect(12);
			}
		}
	}

	private static bool IsActionSlotRight(ActionBarSlot slot, RaptureHotbarModule.HotbarSlot? hot, uint actionID)
	{
		if (hot.HasValue && HotbarAddonHelper.TryMatchPvPCombo(hot.Value, actionID, out var comboMatch))
		{
			return comboMatch;
		}

		if (hot.HasValue
			&& (hot.Value.OriginalApparentSlotType is not RaptureHotbarModule.HotbarSlotType.CraftAction and not RaptureHotbarModule.HotbarSlotType.Action
				|| hot.Value.ApparentSlotType is not RaptureHotbarModule.HotbarSlotType.CraftAction and not RaptureHotbarModule.HotbarSlotType.Action))
		{
			return false;
		}

		return Service.GetAdjustedActionId((uint)slot.ActionId) == actionID;
	}

	public static void Dispose()
	{
		if (_dtrEntry == null)
		{
			return;
		}

		_dtrEntry.Remove();
		_dtrEntry = null;
		_dtrText = null;
		_dtrClickType = null;
	}
}