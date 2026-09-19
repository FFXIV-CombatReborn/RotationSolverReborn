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

	// Last values pushed to the DTR entry; setting Text rebuilds the node, so only do it on change.
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

	internal static void PulseActionBar(uint actionID)
	{
		LoopAllSlotBar((bar, hot, index) =>
		{
			return IsActionSlotRight(bar, hot, actionID);
		});
	}

	private static bool IsActionSlotRight(ActionBarSlot slot, RaptureHotbarModule.HotbarSlot? hot, uint actionID)
	{
		// Only plain and crafting actions can match; macros and every other slot type are skipped.
		if (hot.HasValue
			&& (hot.Value.OriginalApparentSlotType is not RaptureHotbarModule.HotbarSlotType.CraftAction and not RaptureHotbarModule.HotbarSlotType.Action
				|| hot.Value.ApparentSlotType is not RaptureHotbarModule.HotbarSlotType.CraftAction and not RaptureHotbarModule.HotbarSlotType.Action))
		{
			return false;
		}

		return Service.GetAdjustedActionId((uint)slot.ActionId) == actionID;
	}

	// Own buffer, framework thread only.
	private static readonly List<nint> _pulseAddons = [];

	private delegate bool ActionBarAction(ActionBarSlot bar, RaptureHotbarModule.HotbarSlot? hot, uint highLightID);
	private static unsafe void LoopAllSlotBar(ActionBarAction doingSomething)
	{
		var framework = Framework.Instance();
		var uiModule = framework == null ? null : framework->GetUIModule();
		var raptureModule = uiModule == null ? null : uiModule->GetRaptureHotbarModule();
		if (raptureModule == null)
		{
			return;
		}

		var index = 0;
		var hotBarIndex = 0;

		HotbarAddonHelper.GetHotbarAddons(_pulseAddons);

		foreach (var intPtr in _pulseAddons)
		{
			var actionBar = (AddonActionBarBase*)intPtr;
			var hotBar = raptureModule->Hotbars[Math.Min(hotBarIndex, raptureModule->Hotbars.Length - 1)];

			var slotIndex = 0;
			foreach (var slot in actionBar->ActionBarSlotVector.AsSpan())
			{
				var highLightId = 0x53550000 + index;
				RaptureHotbarModule.HotbarSlot? hotSlot = hotBarIndex > 9 || slotIndex >= hotBar.Slots.Length ? null : hotBar.Slots[slotIndex];

				var iconAddon = slot.Icon;
				if (doingSomething(slot, hotSlot, (uint)highLightId)
					&& iconAddon != null && iconAddon->AtkResNode.IsVisible())
				{
					actionBar->PulseActionBarSlot(slotIndex);
					UIGlobals.PlaySoundEffect(12);
				}

				// Always advance, even for skipped slots, so later slots keep their correct indices.
				slotIndex++;
				index++;
			}
			hotBarIndex++;
		}
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