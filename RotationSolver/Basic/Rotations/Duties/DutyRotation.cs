using Dalamud.Game.ClientState.Objects.SubKinds;
using ECommons.ExcelServices;
using System.Collections.Concurrent;

namespace RotationSolver.Basic.Rotations.Duties;
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// Represents the base class for duty rotations.
/// </summary>
public partial class DutyRotation : IDisposable
{
	#region GCD
	public virtual bool EmergencyGCD(IAction? nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool ProvokeGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool MyInterruptGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool GeneralGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool RaiseGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool DispelGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool MoveForwardGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool HealSingleGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool HealAreaGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool DefenseSingleGCD(out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool DefenseAreaGCD(out IAction? act)
	{
		act = null;
		return false;
	}
	#endregion

	#region Ability

	public virtual bool InterruptAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool DispelAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool AntiKnockbackAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool ProvokeAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool MoveBackAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool HealSingleAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool HealAreaAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool SpeedAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	public virtual bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		act = null;
		return false;
	}

	#endregion

	/// <summary>
	/// Last used Combo Action.
	/// <br>WARNING: Do Not make this method the main of your rotation.</br>
	/// </summary>
	/// <param name="ids">True if any of this is matched.</param>
	/// <returns></returns>
	public static bool IsLastComboAction(params ReadOnlySpan<ActionID> ids)
	{
		CustomRotation.CountingOfLastUsing++;
		return IActionHelper.IsLastComboAction(ids);
	}

	public virtual void DisplayDutyStatus()
	{
		if (DataCenter.Orbonne)
		{
			ImGui.Spacing();
			ImGui.Text($"HeavenlyShieldPvE Status: {StatusHelper.PlayerHasStatus(false, StatusID.Shieldbearer)}");
			ImGui.Text($"HeavenlyShieldPvE Slotted: {HeavenlyShieldPvE.Info.IsOnSlot}");
			ImGui.Text($"HeavenlyShieldPvE Charges: {HeavenlyShieldPvE.Cooldown.CurrentCharges}");
			ImGui.Spacing();
			ImGui.Text($"HeavenlySwordPvE Status: {StatusHelper.PlayerHasStatus(false, StatusID.Swordbearer)}");
			ImGui.Text($"HeavenlySwordPvE Slotted: {HeavenlySwordPvE.Info.IsOnSlot}");
			ImGui.Text($"HeavenlySwordPvE Charges: {HeavenlySwordPvE.Cooldown.CurrentCharges}");
			ImGui.Spacing();
			ImGui.Text($"Orbonee Monestary: {InOrbonne}");
			ImGui.Spacing();
			ImGui.Text($"IsAgriasCastingJudgementBlade: {IsAgriasCastingJudgementBlade}");
			ImGui.Spacing();
		}

		if (DataCenter.IsInMonsterHunterDuty)
		{
			ImGui.Spacing();
			ImGui.Text($"MegaPotionPvE Slotted: {MegaPotionPvE.Info.IsOnSlot}");
			ImGui.Text($"MegaPotionPvE Charges: {MegaPotionPvE.Cooldown.CurrentCharges}");
			ImGui.Spacing();
			ImGui.Text($"Rathalos Normal: {RathalosNormal}");
			ImGui.Text($"Rathalos EX: {RathalosEX}");
			ImGui.Spacing();
			ImGui.Spacing();
			ImGui.Text($"MegaPotionPvE_44247 Slotted: {MegaPotionPvE_44247.Info.IsOnSlot}");
			ImGui.Text($"MegaPotionPvE_44247 Charges: {MegaPotionPvE_44247.Cooldown.CurrentCharges}");
			ImGui.Spacing();
			ImGui.Text($"Arkveld Normal: {ArkveldNormal}");
			ImGui.Text($"Arkveld EX: {ArkveldEX}");
			ImGui.Spacing();
		}

		if (DataCenter.IsInOccultCrescentOp)
		{
			ImGui.Text($"ActivePhantomJob: {ActivePhantomJob ?? "N/A"}");
			ImGui.Spacing();

			if (string.Equals(ActivePhantomJob, "Oracle", StringComparison.OrdinalIgnoreCase))
			{
				ImGui.Text($"HasCleansing: {HasCleansing}");
				ImGui.Text($"HasStarfall: {HasStarfall}");
				ImGui.Text($"HasPhantomJudgment: {HasPhantomJudgment}");
				ImGui.Text($"HasBlessing: {HasBlessing}");
			}

			if (string.Equals(ActivePhantomJob, "Samurai", StringComparison.OrdinalIgnoreCase))
			{
				ImGui.Text($"Has item for Zeninage: {ZeninageItem.HasIt}");
			}

			if (string.Equals(ActivePhantomJob, "Chemist", StringComparison.OrdinalIgnoreCase))
			{
				ImGui.Text($"Has item for Occult Potion: {OccultPotionItem.HasIt}");
				ImGui.Text($"Has item for Occult Ether: {OccultPotionItem.HasIt}");
				ImGui.Text($"Has item for Occult Elixir: {OccultElixirItem.HasIt}");
			}
		}

		if (InVariantDungeon)
		{
			ImGui.Text($"Variant Spirit Dart Status: {StatusHelper.PlayerHasStatus(true, StatusID.VariantSpiritDartSet)}");
			ImGui.Text($"VariantSpiritDartPvE_46940 Slotted: {VariantSpiritDartPvE_46940.Info.IsOnSlot}");
			ImGui.Text($"VariantSpiritDartPvE_33863 Slotted: {VariantSpiritDartPvE_33863.Info.IsOnSlot}");
			ImGui.Text($"VariantSpiritDartPvE Slotted: {VariantSpiritDartPvE.Info.IsOnSlot}");
			ImGui.Spacing();
			ImGui.Text($"Variant Rampart Status: {StatusHelper.PlayerHasStatus(true, StatusID.VariantRampartSet)}");
			ImGui.Text($"VariantRampartPvE_46941 Slotted: {VariantRampartPvE_46941.Info.IsOnSlot}");
			ImGui.Text($"VariantRampartPvE_33864 Slotted: {VariantRampartPvE_33864.Info.IsOnSlot}");
			ImGui.Text($"VariantRampartPvE Slotted: {VariantRampartPvE.Info.IsOnSlot}");
			ImGui.Spacing();
			ImGui.Text($"Variant Cure Status: {StatusHelper.PlayerHasStatus(true, StatusID.VariantCureSet)}");
			ImGui.Text($"VariantCurePvE_46939 Slotted: {VariantCurePvE_46939.Info.IsOnSlot}");
			ImGui.Text($"VariantCurePvE_33862 Slotted: {VariantCurePvE_33862.Info.IsOnSlot}");
			ImGui.Text($"VariantCurePvE Slotted: {VariantCurePvE.Info.IsOnSlot}");
			ImGui.Spacing();
			ImGui.Text($"Variant Raise Status: {StatusHelper.PlayerHasStatus(true, StatusID.VariantRaiseSet)}");
			ImGui.Text($"VariantRaisePvE Slotted: {VariantRaisePvE.Info.IsOnSlot}");
			ImGui.Spacing();
			ImGui.Text($"Variant Raise Ii Status: {StatusHelper.PlayerHasStatus(true, StatusID.VariantRaiseSet)}");
			ImGui.Text($"VariantRaiseIiPvE Slotted: {VariantRaiseIiPvE.Info.IsOnSlot}");
			ImGui.Spacing();
			ImGui.Text($"Variant Eagle Eye Shot Status: {StatusHelper.PlayerHasStatus(true, StatusID.VariantEagleEyeShotSet)}");
			ImGui.Text($"VariantEagleEyeShotPvE Slotted: {VariantEagleEyeShotPvE.Info.IsOnSlot}");
			ImGui.Spacing();
			ImGui.Text($"Variant Ultimatum Status: {StatusHelper.PlayerHasStatus(true, StatusID.VariantUltimatumSet)}");
			ImGui.Text($"VariantUltimatumPvE Slotted: {VariantUltimatumPvE.Info.IsOnSlot}");
			ImGui.Spacing();
			ImGui.Text($"The Merchant's Tale: {TheMerchantsTale}");
			ImGui.Text($"Sildihn Subterrane: {SildihnSubterrane}");
			ImGui.Text($"Mount Rokkon: {MountRokkon}");
			ImGui.Text($"Aloalo Island: {AloaloIsland}");
			ImGui.Spacing();
		}
	}

	public DutyRotation()
	{
		Configs = new RotationConfigSet(this);
	}

	/// <summary>
	/// Releases all resources used by the <see cref="DutyRotation"/> class.
	/// </summary>
	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}

	/// <summary>
	/// Rotation config set for display
	/// </summary>
	internal IRotationConfigSet Configs { get; }

	/// <summary>
	/// Gets the hostile target.
	/// </summary>
	public static IBattleChara? HostileTarget => DataCenter.HostileTarget;

	/// <summary>
	/// Check if in combat.
	/// </summary>
	public static bool InCombat => DataCenter.InCombat;

	/// <summary>
	/// 
	/// </summary>
	public static bool IsMoving => DataCenter.IsMoving;

	/// <summary>
	/// 
	/// </summary>
	public static bool RathalosEX => DataCenter.RathalosEX;

	/// <summary>
	/// 
	/// </summary>
	public static bool RathalosNormal => DataCenter.RathalosNormal;

	/// <summary>
	/// 
	/// </summary>
	public static bool ArkveldNormal => DataCenter.ArkveldNormal;

	/// <summary>
	/// 
	/// </summary>
	public static bool ArkveldEX => DataCenter.ArkveldEX;

	/// <summary>
	/// 
	/// </summary>
	public static bool TheMerchantsTaleAdvanced => DataCenter.TheMerchantsTaleAdvanced;

	/// <summary>
	/// 
	/// </summary>
	public static bool TheMerchantsTale => DataCenter.TheMerchantsTale;

	/// <summary>
	/// 
	/// </summary>
	public static bool SildihnSubterrane => DataCenter.SildihnSubterrane;

	/// <summary>
	/// 
	/// </summary>
	public static bool MountRokkon => DataCenter.MountRokkon;

	/// <summary>
	/// 
	/// </summary>
	public static bool AloaloIsland => DataCenter.AloaloIsland;

	/// <summary>
	/// 
	/// </summary>
	public static bool InVariantDungeon => DataCenter.InVariantDungeon;

	/// <summary>
	/// 
	/// </summary>
	public static bool InOrbonne => DataCenter.Orbonne;

	/// <summary>
	/// 
	/// </summary>
	public static bool IsAgriasCastingJudgementBlade => DataCenter.IsAgriasCastingSpecialIndicator();

	/// <summary>
	/// This is the player.
	/// </summary>
	protected static IPlayerCharacter? Player => ECommons.GameHelpers.Player.Object;

	public static bool IsRDM => DataCenter.Job == Job.RDM;
	public static bool IsPLD => DataCenter.Job == Job.PLD;
	public static bool IsBLM => DataCenter.Job == Job.BLM;

	public static float PartyMembersAverHP => DataCenter.PartyMembersAverHP;

	public static bool InBurstWindow()
	{
		return DataCenter.CurrentRotation?.IsInBurstWindow == true;
	}

	public enum PhantomJob : byte
	{
		Freelancer,
		Knight,
		Berserker,
		Monk,
		Ranger,
		Samurai,
		Bard,
		Geomancer,
		TimeMage,
		Cannoneer,
		Chemist,
		Oracle,
		Thief,
		MysticKnight,
		Gladiator,
		Dancer,
		Ninja,
		WhiteMage,
		BlackMage,
		Dragoon,
		Summoner,
		BlueMage,
		RedMage,
		Necromancer,
		None
	}

	public static PhantomJob GetPhantomJob()
	{
		if (!DataCenter.IsInOccultCrescentOp)
		{
			return PhantomJob.None;
		}

		RefreshPhantomLevels();
		return _activePhantomJob;
	}

	/// <summary>
	/// Does player have swift cast, dual cast or triple cast.
	/// </summary>
	[Description("Has Swift")]
	public static bool HasSwift => CustomRotation.HasSwift;

	/// <summary>
	///
	/// </summary>
	[Description("Has tank stance")]
	public static bool HasTankStance => CustomRotation.HasTankStance;

	/// <summary>
	///
	/// </summary>
	[Description("Has tank stance")]
	public static bool HasTankInvuln => CustomRotation.HasTankInvuln;

	/// <summary>
	/// In the burst status.
	/// </summary>
	[Description("Is burst")]
	public static bool IsBurst => CustomRotation.IsBurst;

	/// <summary>
	/// Is RSR enabled.
	/// </summary>
	[Description("The state of auto. True for on.")]
	public static bool AutoState => DataCenter.State;

	/// <summary>
	/// Is RSR in manual mode.
	/// </summary>
	[Description("The state of manual. True for manual.")]
	public static bool IsManual => DataCenter.IsManual;

	/// <summary>
	/// The merged status, which contains <see cref="AutoState"/> and <see cref="CommandStatus"/>.
	/// </summary>
	public static AutoStatus MergedStatus => DataCenter.MergedStatus;

	/// <summary>
	/// The automatic status, which is checked from RS.
	/// </summary>
	public static AutoStatus AutoStatus => DataCenter.AutoStatus;

	/// <summary>
	/// The CMD status, which is checked from the player.
	/// </summary>
	public static AutoStatus CommandStatus => DataCenter.CommandStatus;

	/// <summary>
	/// Returns the number of hostile targets within the specified range from the player.
	/// </summary>
	/// <param name="range">The range to check (in yalms).</param>
	/// <returns>The number of hostile targets within the given range.</returns>
	[Description("The number of hostiles in specified range")]
	public static int NumberOfHostilesInRangeOf(float range)
	{
		return DataCenter.NumberOfHostilesInRangeOf(range);
	}

	/// <summary>
	/// Is there any hostile target in range? 25 for ranged jobs and healer, 3 for melee and tank.
	/// </summary>
	[Description("Has hostiles in Range")]
	public static bool HasHostilesInRange => DataCenter.HasHostilesInRange;

	/// <summary>
	/// Is there any hostile target in 25 yalms?
	/// </summary>
	[Description("Has hostiles in 25 yalms")]
	public static bool HasHostilesInMaxRange => DataCenter.HasHostilesInMaxRange;

	/// <summary>
	/// How many hostile targets in range? 25 for ranged jobs and healer, 3 for melee and tank.
	/// </summary>
	[Description("The number of hostiles in Range")]
	public static int NumberOfHostilesInRange => DataCenter.NumberOfHostilesInRange;

	/// <summary>
	/// How many hostile targets in max range (25 yalms) regardless of job
	/// </summary>
	[Description("The number of hostiles in max Range")]
	public static int NumberOfHostilesInMaxRange => DataCenter.NumberOfHostilesInMaxRange;

	/// <summary>
	/// How many hostile targets in range? 25 for ranged jobs and healer, 3 for melee and tank. This is all can attack.
	/// </summary>
	[Description("The number of all hostiles in Range")]
	public static int NumberOfAllHostilesInRange => DataCenter.NumberOfAllHostilesInRange;

	/// <summary>
	/// How many hostile targets in max range (25 yalms) regardless of job. This is all can attack.
	/// </summary>
	[Description("The number of all hostiles in max Range")]
	public static int NumberOfAllHostilesInMaxRange => DataCenter.NumberOfAllHostilesInMaxRange;

	#region Phantom Levels

	/// <summary>
	/// Gets the name of the current active Phantom Job, or None if none are active. test
	/// </summary>
	public static string? ActivePhantomJob => GetPhantomJob().ToString();

	private static readonly Dictionary<StatusID, PhantomJob> PhantomJobByStatus = new()
	{
		[StatusID.PhantomFreelancer] = PhantomJob.Freelancer,
		[StatusID.PhantomKnight] = PhantomJob.Knight,
		[StatusID.PhantomMonk] = PhantomJob.Monk,
		[StatusID.PhantomBard] = PhantomJob.Bard,
		[StatusID.PhantomChemist] = PhantomJob.Chemist,
		[StatusID.PhantomTimeMage] = PhantomJob.TimeMage,
		[StatusID.PhantomCannoneer] = PhantomJob.Cannoneer,
		[StatusID.PhantomOracle] = PhantomJob.Oracle,
		[StatusID.PhantomBerserker] = PhantomJob.Berserker,
		[StatusID.PhantomRanger] = PhantomJob.Ranger,
		[StatusID.PhantomThief] = PhantomJob.Thief,
		[StatusID.PhantomSamurai] = PhantomJob.Samurai,
		[StatusID.PhantomGeomancer] = PhantomJob.Geomancer,
		[StatusID.PhantomMysticKnight] = PhantomJob.MysticKnight,
		[StatusID.PhantomDancer] = PhantomJob.Dancer,
		[StatusID.PhantomGladiator] = PhantomJob.Gladiator,
		[StatusID.PhantomNinja] = PhantomJob.Ninja,
		[StatusID.PhantomBlackMage] = PhantomJob.BlackMage,
		[StatusID.PhantomWhiteMage] = PhantomJob.WhiteMage,
		[StatusID.PhantomDragoon] = PhantomJob.Dragoon,
		[StatusID.PhantomSummoner] = PhantomJob.Summoner,
		[StatusID.PhantomNecromancer] = PhantomJob.Necromancer,
		[StatusID.PhantomBlueMage] = PhantomJob.BlueMage,
		[StatusID.PhantomRedMage] = PhantomJob.RedMage,
	};

	private static readonly byte[] _phantomLevels = new byte[(int)PhantomJob.None];
	private static PhantomJob _activePhantomJob = PhantomJob.None;
	private static long _phantomLevelsTick = long.MinValue;

	private static void RefreshPhantomLevels()
	{
		var now = Environment.TickCount64;
		if (_phantomLevelsTick != long.MinValue && now - _phantomLevelsTick < 15)
		{
			return;
		}

		_phantomLevelsTick = now;
		Array.Clear(_phantomLevels);
		_activePhantomJob = PhantomJob.None;

		var player = Player;
		if (!DataCenter.IsInOccultCrescentOp || player?.StatusList is not { } statusList)
		{
			return;
		}

		foreach (var status in statusList)
		{
			if (status != null && status.SourceId == player.GameObjectId
				&& PhantomJobByStatus.TryGetValue((StatusID)status.StatusId, out var job) && status.Param > 0)
			{
				_phantomLevels[(int)job] = (byte)status.Param;
				if (_activePhantomJob == PhantomJob.None)
				{
					_activePhantomJob = job;
				}
			}
		}
	}

	private static byte GetPhantomLevel(PhantomJob job)
	{
		RefreshPhantomLevels();
		return _phantomLevels[(int)job];
	}

	public static byte FreelancerLevel => GetPhantomLevel(PhantomJob.Freelancer);

	public static byte KnightLevel => GetPhantomLevel(PhantomJob.Knight);

	public static byte MonkLevel => GetPhantomLevel(PhantomJob.Monk);

	public static byte BardLevel => GetPhantomLevel(PhantomJob.Bard);

	public static byte ChemistLevel => GetPhantomLevel(PhantomJob.Chemist);

	public static byte TimeMageLevel => GetPhantomLevel(PhantomJob.TimeMage);

	public static byte CannoneerLevel => GetPhantomLevel(PhantomJob.Cannoneer);

	public static byte OracleLevel => GetPhantomLevel(PhantomJob.Oracle);

	public static byte BerserkerLevel => GetPhantomLevel(PhantomJob.Berserker);

	public static byte RangerLevel => GetPhantomLevel(PhantomJob.Ranger);

	public static byte ThiefLevel => GetPhantomLevel(PhantomJob.Thief);

	public static byte SamuraiLevel => GetPhantomLevel(PhantomJob.Samurai);

	public static byte GeomancerLevel => GetPhantomLevel(PhantomJob.Geomancer);

	public static byte MysticKnightLevel => GetPhantomLevel(PhantomJob.MysticKnight);

	public static byte DancerLevel => GetPhantomLevel(PhantomJob.Dancer);

	public static byte GladiatorLevel => GetPhantomLevel(PhantomJob.Gladiator);

	public static byte NinjaLevel => GetPhantomLevel(PhantomJob.Ninja);

	public static byte BlackMageLevel => GetPhantomLevel(PhantomJob.BlackMage);

	public static byte WhiteMageLevel => GetPhantomLevel(PhantomJob.WhiteMage);

	public static byte DragoonLevel => GetPhantomLevel(PhantomJob.Dragoon);

	public static byte SummonerLevel => GetPhantomLevel(PhantomJob.Summoner);

	public static byte NecromancerLevel => GetPhantomLevel(PhantomJob.Necromancer);

	public static byte BlueMageLevel => GetPhantomLevel(PhantomJob.BlueMage);

	public static byte RedMageLevel => GetPhantomLevel(PhantomJob.RedMage);

	#endregion

	/// <summary>
	/// The specific duty action IDs that belong to this duty rotation. Overridden by duty rotation
	/// base classes (e.g. <see cref="HardboiledRotation"/>) whose actions replace the player's normal
	/// hotbars instead of occupying the dedicated duty action slots (Quest Battles), since those
	/// duties can't be identified by matching against <see cref="DataCenter.DutyActions"/>.
	/// </summary>
	protected virtual uint[] QuestBattleActionIds => [];

	internal IAction[] AllActions
	{
		get
		{
			var questBattleActionIds = DataCenter.IsInQuestBattle ? QuestBattleActionIds : [];
			var actions = new List<IAction>();
			foreach (var (property, id) in GetActionProperties(GetType()))
			{
				if ((Array.IndexOf(DataCenter.DutyActions, id) >= 0 || Array.IndexOf(questBattleActionIds, id) >= 0)
					&& property.GetValue(this) is IAction action)
				{
					actions.Add(action);
				}
			}

			return [.. actions];
		}
	}

	private static readonly ConcurrentDictionary<Type, (PropertyInfo Property, uint Id)[]> _actionProperties = new();

	private static (PropertyInfo Property, uint Id)[] GetActionProperties(Type type)
	{
		return _actionProperties.GetOrAdd(type, static t =>
		{
			List<(PropertyInfo, uint)> properties = [];
			foreach (var p in t.GetRuntimeProperties())
			{
				if (p.GetCustomAttribute<IDAttribute>() is { } attr)
				{
					properties.Add((p, attr.ID));
				}
			}

			return [.. properties];
		});
	}
}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member