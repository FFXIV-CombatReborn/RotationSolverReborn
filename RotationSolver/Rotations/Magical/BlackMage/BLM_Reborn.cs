namespace RotationSolver.Rotations.Magical.BlackMage;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn)]
[SourceCode(Path = "main/Rotations/Magical/BlackMage/BLM_Reborn.cs")]
public sealed class BLM_Reborn : BlackMageRotation
{
	public enum OpenerChoice : byte
	{
		[Description("Standard 5+7 Opener")]
		Standard57,

		[Description("Alternative Flare Opener")]
		AltFlare,
	}

	[RotationConfig(CombatType.PvE, Name = "Use pre-pull Fire III during countdown", Tooltip = "Starts hardcasting Fire III (or Fire below Lv 35) before pull so it lands on engagement.")]
	public bool UseCountdownFireIII { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Level 100 opener style", Tooltip = "Standard 5+7 performs 5 Fire IVs before Manafont and 7 after for double Flare Star. Alternative Flare uses 4 Fire IVs + Despair before Manafont and finishes the second cycle with Flare -> Flare Star.")]
	public OpenerChoice OpenerType { get; set; } = OpenerChoice.Standard57;

	[RotationConfig(CombatType.PvE, Name = "Use Ley Lines automatically", Tooltip = "Places Ley Lines when standing still with sufficient target time-to-kill. Holds one charge for burst unless overcapping or overridden below.")]
	public bool UseLeyLines { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use all Ley Lines charges on cooldown", Parent = nameof(UseLeyLines), Tooltip = "Spends both Ley Lines charges on cooldown instead of pooling a charge for raid buffs.")]
	public bool UseBothLeyLinesCharges { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Retrace when outside Ley Lines", Tooltip = "Relocates active Ley Lines to your position when standing still outside Circle of Power.")]
	public bool UseRetrace { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Swiftcast + Transpose for instant Blizzard III", Tooltip = "Weaves Swiftcast before Transpose at the end of Astral Fire so Blizzard III fires instantly under Umbral Ice.")]
	public bool UseSwiftcastForB3 { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Triplecast + Transpose for instant Blizzard III", Tooltip = "Falls back to Triplecast before Transpose when Swiftcast is on cooldown. Disable to save Triplecast strictly for movement.")]
	public bool UseTriplecastForB3 { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Triplecast for movement", Tooltip = "Consumes Triplecast charges while moving in combat after instant GCD options are exhausted.")]
	public bool UseTriplecastForMovement { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Swiftcast for movement", Tooltip = "Consumes Swiftcast while moving in combat if Triplecast is unavailable.")]
	public bool UseSwiftcastForMovement { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Keep one Polyglot stack for movement", Tooltip = "Pools one Polyglot stack outside of overcap and Manafont weave windows for emergency movement.")]
	public bool KeepOnePolyglotForMovement { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "Use Intelligence potions", Tooltip = "Uses burst medicine pre-pull during countdown or inside Astral Fire burst windows.")]
	public bool UsePotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use Scathe as last-resort movement filler", Tooltip = "Casts Scathe when moving with no instant spells, Polyglot, Swiftcast, or Triplecast available.")]
	public bool UseScatheWhileMoving { get; set; } = false;

	private const float DoubleWeaveThreshold = 1.25f;
	private const float MovementWeaveCutoff = 0.6f;
	private const float SustainedMovementSeconds = 0.25f;
	private const long NoHostilesDowntimeMs = 2500;
	private const float OutOfCombatDowntimeSeconds = 4.0f;
	private const float OutOfCombatUmbralSoulSeconds = 1.5f;
	private const float ThunderTtkThreshold = 9.0f;
	private const float ThunderMovementRefreshSeconds = 6.0f;
	private const float ThunderAoeFillerRefreshSeconds = 9.0f;
	private const float OpenerWindowSeconds = 35.0f;
	private const int MaxMp = 10000;
	private const int BaseFireIvCost = 1600;
	private const int HeartFireIvCost = 800;
	private const int ParadoxAfCost = 1600;
	private const int DespairMinCost = 800;
	private const int FlareMinCost = 800;
	private const int ModernAoeMinMp = 2400;
	private const int Lv50AoeComboMinMp = 3800;
	private const int NeutralFireIiiMinMp = 6000;

	private static readonly StatusID[] ThunderStatuses =
	[
		StatusID.Thunder,
		StatusID.ThunderIi,
		StatusID.ThunderIii,
		StatusID.ThunderIv,
		StatusID.HighThunder,
		StatusID.HighThunder_3872,
	];

	private long _noHostilesSinceMs;
	private int _cachedAoeTargetCount;

	protected override void UpdateInfo()
	{
		base.UpdateInfo();
		_cachedAoeTargetCount = GetAoeTargetCount(FlarePvE);
	}

	public override void OnTerritoryChanged()
	{
		_noHostilesSinceMs = 0;
		_cachedAoeTargetCount = 0;
		base.OnTerritoryChanged();
	}

	private bool IsDowntimeReady
	{
		get
		{
			if (!InCombat)
			{
				return TimeSinceLastAction.TotalSeconds >= OutOfCombatDowntimeSeconds;
			}
			return _noHostilesSinceMs != 0 && Environment.TickCount64 - _noHostilesSinceMs >= NoHostilesDowntimeMs;
		}
	}

	private static int MaxPolyglotStacks =>
		EnhancedPolyglotIiTrait.EnoughLevel
			? 3
			: EnhancedPolyglotTrait.EnoughLevel ? 2 : 1;

	private static int GetAoeTargetCount(IBaseAction action)
	{
		if (AllHostileTargets == null)
		{
			return 0;
		}

		if (Service.Config.AoEType != ConfigTypes.AoEType.Full || (DataCenter.IsInM9S && Service.Config.M9SCleaveOnly))
		{
			return HasHostilesInMaxRange ? 1 : 0;
		}

		var effectRange = action.Info.EffectRange;

		if (IsManual)
		{
			if (CurrentTarget == null || CurrentTarget.IsDead || !CurrentTarget.IsEnemy() || CurrentTarget.DistanceToPlayer() > action.Info.Range || !CurrentTarget.CanSee())
			{
				return HasHostilesInMaxRange ? 1 : 0;
			}

			var manualCount = 0;
			foreach (var other in AllHostileTargets)
			{
				if (other == null || other.IsDead)
				{
					continue;
				}
				if (Vector3.Distance(CurrentTarget.Position, other.Position) <= effectRange + other.HitboxRadius)
				{
					if (Service.Config.NoNewHostiles && other.TargetObject == null)
					{
						return 0;
					}
					manualCount++;
				}
			}
			return manualCount;
		}

		var bestCount = 0;
		foreach (var center in AllHostileTargets)
		{
			if (center == null || center.IsDead || center.DistanceToPlayer() > action.Info.Range || !center.CanSee())
			{
				continue;
			}

			var count = 0;
			var hitsNewHostile = false;
			foreach (var other in AllHostileTargets)
			{
				if (other == null || other.IsDead)
				{
					continue;
				}
				if (Vector3.Distance(center.Position, other.Position) <= effectRange + other.HitboxRadius)
				{
					if (Service.Config.NoNewHostiles && other.TargetObject == null)
					{
						hitsNewHostile = true;
						break;
					}
					count++;
				}
			}

			if (!hitsNewHostile && count > bestCount)
			{
				bestCount = count;
			}
		}

		return bestCount;
	}

	private int AoeTargetCount => _cachedAoeTargetCount;

	private bool IsAoeMode
	{
		get
		{
			if (FlareStarPvE.EnoughLevel && FlarePvE.Info.EnoughLevelAndQuest() && UmbralHeartTrait.EnoughLevel)
			{
				return AoeTargetCount >= 2;
			}
			if (FireIvPvE.Info.EnoughLevelAndQuest())
			{
				return AoeTargetCount >= 3;
			}
			if (UmbralHeartTrait.EnoughLevel && FlarePvE.Info.EnoughLevelAndQuest())
			{
				return AoeTargetCount >= 2;
			}
			return BlizzardIiPvE.EnoughLevel && AoeTargetCount >= 3;
		}
	}

	private bool ShouldUseAoeThunder
	{
		get
		{
			if (HighThunderIiPvE.EnoughLevel || ThunderIvPvE.EnoughLevel)
			{
				return AoeTargetCount >= 2;
			}
			if (ThunderIiiPvE.Info.EnoughLevelAndQuest())
			{
				return AoeTargetCount >= 3;
			}
			return ThunderIiPvE.EnoughLevel && AoeTargetCount >= 2;
		}
	}

	private bool CanUseFlareStar =>
		FlareStarPvE.EnoughLevel && Service.Config.AoEType != ConfigTypes.AoEType.Off;

	private bool HasPendingFlareStar => CanUseFlareStar && AstralSoulStacks >= 6;

	private bool ManafontReady =>
		ManafontPvE.Info.EnoughLevelAndQuest() && !ManafontPvE.Cooldown.IsCoolingDown;

	private static int FireIvCost => UmbralHearts > 0 ? HeartFireIvCost : BaseFireIvCost;

	private static int MpForFireIvs(int count, int hearts)
	{
		if (count <= 0)
		{
			return 0;
		}
		var discounted = Math.Min(count, hearts);
		var regular = count - discounted;
		return (discounted * HeartFireIvCost) + (regular * BaseFireIvCost);
	}

	private bool IsAltFlarePreManafontDespair =>
		OpenerType == OpenerChoice.AltFlare
		&& CanUseFlareStar
		&& InCombat
		&& CombatTime < OpenerWindowSeconds
		&& ManafontReady
		&& AstralSoulStacks == 4
		&& CurrentMp >= DespairMinCost
		&& CurrentMp <= BaseFireIvCost;

	private bool CanBridgeDoubleFlareStarViaManafont =>
		CanUseFlareStar
		&& ManafontReady
		&& !IsAltFlarePreManafontDespair
		&& AstralSoulStacks < 6
		&& (AstralSoulStacks >= 5 || CurrentMp >= (uint)MpForFireIvs(5 - AstralSoulStacks, UmbralHearts));

	private bool CanReachDoubleFlareStarPostManafont =>
		CanUseFlareStar
		&& AstralSoulStacks > 0
		&& AstralSoulStacks < 6
		&& CurrentMp >= (uint)MpForFireIvs(12 - AstralSoulStacks, UmbralHearts);

	private bool CanReachFlareStarViaFireIvST =>
		CanUseFlareStar
		&& (AstralSoulStacks >= 6
			|| CurrentMp >= (uint)MpForFireIvs(6 - AstralSoulStacks, UmbralHearts)
			|| CanBridgeDoubleFlareStarViaManafont);

	private bool CanReachFlareStarViaFlareST
	{
		get
		{
			if (!CanUseFlareStar || !FlarePvE.Info.EnoughLevelAndQuest() || CanReachFlareStarViaFireIvST)
			{
				return false;
			}
			var neededForThree = Math.Max(0, 3 - AstralSoulStacks);
			return CurrentMp >= (uint)(MpForFireIvs(neededForThree, UmbralHearts) + FlareMinCost);
		}
	}

	private bool ShouldCastAfParadox
	{
		get
		{
			if (!IsParadoxActive || !ParadoxPvE.EnoughLevel || CurrentMp < ParadoxAfCost)
			{
				return false;
			}
			if (!CanUseFlareStar)
			{
				var minAfterParadox = DespairPvE.EnoughLevel ? DespairMinCost : 0;
				return CurrentMp >= (uint)(ParadoxAfCost + minAfterParadox);
			}
			if (AstralSoulStacks >= 6 || IsAltFlarePreManafontDespair)
			{
				return false;
			}
			var neededForSix = MpForFireIvs(6 - AstralSoulStacks, UmbralHearts);
			if (CanBridgeDoubleFlareStarViaManafont && CurrentMp < (uint)neededForSix)
			{
				return CurrentMp >= (uint)(ParadoxAfCost + MpForFireIvs(5 - AstralSoulStacks, UmbralHearts));
			}
			if (CanReachDoubleFlareStarPostManafont)
			{
				return CurrentMp >= (uint)(ParadoxAfCost + MpForFireIvs(12 - AstralSoulStacks, UmbralHearts) + DespairMinCost);
			}
			if (CurrentMp >= (uint)neededForSix)
			{
				return CurrentMp >= (uint)(ParadoxAfCost + neededForSix + DespairMinCost);
			}
			if (CanReachFlareStarViaFlareST)
			{
				var neededForThree = Math.Max(0, 3 - AstralSoulStacks);
				return CurrentMp >= (uint)(ParadoxAfCost + MpForFireIvs(neededForThree, UmbralHearts) + FlareMinCost);
			}
			return CurrentMp >= (uint)(ParadoxAfCost + (DespairPvE.EnoughLevel ? DespairMinCost : 0));
		}
	}

	private bool NeedManafont
	{
		get
		{
			if (!InAstralFire || !ManafontReady || HasPendingFlareStar || IsAltFlarePreManafontDespair)
			{
				return false;
			}
			if (IsAoeMode)
			{
				if (FlarePvE.Info.EnoughLevelAndQuest())
				{
					return CurrentMp < FlareMinCost;
				}
				if (FreezePvE.Info.EnoughLevelAndQuest())
				{
					return false;
				}
				return CurrentMp < FireIiPvE.Info.MPNeed;
			}
			if (CanUseFlareStar)
			{
				return CurrentMp < DespairMinCost || (CanBridgeDoubleFlareStarViaManafont && CurrentMp < (uint)FireIvCost);
			}
			if (DespairPvE.EnoughLevel)
			{
				return CurrentMp < DespairMinCost;
			}
			if (FireIvPvE.Info.EnoughLevelAndQuest())
			{
				return CurrentMp < (uint)FireIvCost;
			}
			return CurrentMp < FirePvE.Info.MPNeed;
		}
	}

	private bool CanCastMoreFireST
	{
		get
		{
			if (HasPendingFlareStar || IsAltFlarePreManafontDespair)
			{
				return true;
			}
			if (CanUseFlareStar)
			{
				if (CanReachFlareStarViaFireIvST && AstralSoulStacks < 6 && CurrentMp >= (uint)FireIvCost)
				{
					return true;
				}
				if (CanReachFlareStarViaFlareST
					&& ((AstralSoulStacks < 3 && CurrentMp >= (uint)FireIvCost) || (AstralSoulStacks >= 3 && CurrentMp >= FlareMinCost)))
				{
					return true;
				}
				if (ShouldCastAfParadox)
				{
					return true;
				}
				return DespairPvE.EnoughLevel && CurrentMp >= DespairMinCost;
			}
			if (FireIvPvE.Info.EnoughLevelAndQuest())
			{
				if (ShouldCastAfParadox)
				{
					return true;
				}
				return DespairPvE.EnoughLevel ? CurrentMp >= DespairMinCost : CurrentMp >= (uint)FireIvCost;
			}
			if (BlizzardIiiPvE.Info.EnoughLevelAndQuest())
			{
				return CurrentMp >= FirePvE.Info.MPNeed
					|| (FlarePvE.Info.EnoughLevelAndQuest() && Service.Config.AoEType != ConfigTypes.AoEType.Off && CurrentMp >= FlareMinCost);
			}
			return CurrentMp >= FirePvE.Info.MPNeed;
		}
	}

	private bool ReadyToGoIceST => !IsAoeMode && InAstralFire && !NeedManafont && !CanCastMoreFireST;

	private bool ReadyToTransposeToFireST =>
		!IsAoeMode
		&& InUmbralIce
		&& BlizzardIiiPvE.Info.EnoughLevelAndQuest()
		&& HasFire
		&& UmbralIceStacks == MaxSoulCount
		&& CurrentMp >= MaxMp
		&& (!UmbralHeartTrait.EnoughLevel || !BlizzardIvPvE.Info.EnoughLevelAndQuest() || UmbralHearts == 3)
		&& !IsParadoxActive;

	private static bool IsThunderAction(IAction action) =>
		action.IsTheSameTo(
			true,
			ActionID.ThunderPvE,
			ActionID.ThunderIiPvE,
			ActionID.ThunderIiiPvE,
			ActionID.ThunderIvPvE,
			ActionID.HighThunderPvE,
			ActionID.HighThunderIiPvE);

	public override void DisplayRotationStatus()
	{
		ImGui.Text($"Level: {DataCenter.PlayerSyncedLevel()} | AoE Targets: {AoeTargetCount} | AoE Mode: {IsAoeMode}");
		ImGui.Text($"MP: {CurrentMp} | AF: {AstralFireStacks} | UI: {UmbralIceStacks} | Hearts: {UmbralHearts} | Souls: {AstralSoulStacks}");
		ImGui.Text($"FlareStarST: {CanReachFlareStarViaFireIvST} (Bridge: {CanBridgeDoubleFlareStarViaManafont}, Double: {CanReachDoubleFlareStarPostManafont}, ViaFlare: {CanReachFlareStarViaFlareST})");
		ImGui.Text($"ShouldCastAfParadox: {ShouldCastAfParadox} | NeedManafont: {NeedManafont}");
		ImGui.Text($"ReadyToGoIceST: {ReadyToGoIceST} | ReadyToTransposeToFireST: {ReadyToTransposeToFireST}");
	}

	protected override IAction? CountDownAction(float remainTime)
	{
		var openerSpellCastTime = FireIiiPvE.EnoughLevel ? FireIiiPvE.Info.CastTime : FirePvE.Info.CastTime;
		if (UsePotion
			&& remainTime <= openerSpellCastTime + 1.5f + CountDownAhead
			&& remainTime > openerSpellCastTime + CountDownAhead
			&& UseBurstMedicine(out var potAct, false))
		{
			return potAct;
		}

		if (UseCountdownFireIII)
		{
			if (FireIiiPvE.EnoughLevel && remainTime <= FireIiiPvE.Info.CastTime + CountDownAhead && FireIiiPvE.CanUse(out var f3Act))
			{
				return f3Act;
			}
			if (!FireIiiPvE.EnoughLevel && remainTime <= FirePvE.Info.CastTime + CountDownAhead && FirePvE.CanUse(out var f1Act, skipStatusProvideCheck: true))
			{
				return f1Act;
			}
		}

		return base.CountDownAction(remainTime);
	}

	[RotationDesc]
	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		if (InAstralFire && HasHostilesInRange && NeedManafont)
		{
			if (ManafontPvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
		}

		if (HasHostilesInRange && ReadyToTransposeToFireST && !IsThunderAction(nextGCD))
		{
			if (TransposePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (HasHostilesInRange && ReadyToGoIceST && BlizzardIiiPvE.Info.EnoughLevelAndQuest())
		{
			if (!NextGCDisInstant
				&& TransposePvE.Cooldown.HasOneCharge
				&& (WeaponRemain >= DoubleWeaveThreshold || WeaponRemain == 0f))
			{
				if (UseSwiftcastForB3 && SwiftcastPvE.CanUse(out act))
				{
					return true;
				}
				if (UseTriplecastForB3 && TriplecastPvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
			}

			if (NextGCDisInstant && TransposePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (!IsAoeMode
			&& HasHostilesInRange
			&& InUmbralIce
			&& UmbralIceStacks < MaxSoulCount
			&& !NextGCDisInstant
			&& nextGCD.IsTheSameTo(true, BlizzardIiiPvE))
		{
			if (UseSwiftcastForB3 && SwiftcastPvE.CanUse(out act))
			{
				return true;
			}
			if (UseTriplecastForB3 && TriplecastPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		if (IsAoeMode && HasHostilesInRange)
		{
			if (UmbralHeartTrait.EnoughLevel && FlarePvE.Info.EnoughLevelAndQuest())
			{
				if (InUmbralIce && !IsParadoxActive && UmbralHearts > 0 && CurrentMp >= ModernAoeMinMp && !IsThunderAction(nextGCD) && TransposePvE.CanUse(out act))
				{
					return true;
				}
				if (InAstralFire && !HasPendingFlareStar && CurrentMp < FlareMinCost && !NeedManafont && TransposePvE.CanUse(out act))
				{
					return true;
				}
			}
			else if (FlarePvE.Info.EnoughLevelAndQuest())
			{
				if (InUmbralIce && CurrentMp >= Lv50AoeComboMinMp && !IsThunderAction(nextGCD) && TransposePvE.CanUse(out act))
				{
					return true;
				}
				if (InAstralFire && CurrentMp < FlareMinCost && !NeedManafont && TransposePvE.CanUse(out act))
				{
					return true;
				}
			}
			else if (FreezePvE.Info.EnoughLevelAndQuest())
			{
				if (InUmbralIce && !HasThunder && NeedsThunderRefreshSoon() && !IsThunderAction(nextGCD) && TransposePvE.CanUse(out act))
				{
					return true;
				}
				if (InAstralFire && TransposePvE.CanUse(out act))
				{
					return true;
				}
			}
			else if (!BlizzardIiiPvE.Info.EnoughLevelAndQuest() && FireIiPvE.EnoughLevel)
			{
				if (InUmbralIce && CurrentMp >= MaxMp && !IsThunderAction(nextGCD) && TransposePvE.CanUse(out act))
				{
					return true;
				}
				if (InAstralFire && CurrentMp < FireIiPvE.Info.MPNeed && !NeedManafont && TransposePvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		if (!IsAoeMode && !BlizzardIiiPvE.Info.EnoughLevelAndQuest() && HasHostilesInRange)
		{
			if (InUmbralIce && CurrentMp >= MaxMp && !IsThunderAction(nextGCD) && TransposePvE.CanUse(out act))
			{
				return true;
			}
			if (InAstralFire && CurrentMp < FirePvE.Info.MPNeed && !NeedManafont && TransposePvE.CanUse(out act))
			{
				return true;
			}
		}

		if (nextGCD.IsTheSameTo(true, ActionID.OccultCometPvE) && !NextGCDisInstant)
		{
			if (SwiftcastPvE.CanUse(out act))
			{
				return true;
			}
			if (TriplecastPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.AetherialManipulationPvE)]
	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{
		if (AetherialManipulationPvE.CanUse(out act))
		{
			return true;
		}
		return base.MoveForwardAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.BetweenTheLinesPvE)]
	protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
	{
		if (BetweenTheLinesPvE.CanUse(out act))
		{
			return true;
		}
		return base.MoveBackAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.ManawardPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		if (ManawardPvE.CanUse(out act))
		{
			return true;
		}
		return base.DefenseSingleAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.AddlePvE, ActionID.ManawardPvE)]
	protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (AddlePvE.CanUse(out act))
		{
			return true;
		}
		if (ManawardPvE.CanUse(out act))
		{
			return true;
		}
		return base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.LeyLinesPvE, ActionID.AmplifierPvE, ActionID.RetracePvE, ActionID.LucidDreamingPvE)]
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		if (InCombat && !IsPolyglotStacksMaxed && AmplifierPvE.CanUse(out act))
		{
			return true;
		}

		if (UseLeyLines && InCombat && !HasLeyLines)
		{
			var useCharge = UseBothLeyLinesCharges || HasBuffs || IsBurst || CombatElapsedLess(15);
			if (LeyLinesPvE.CanUse(out act, usedUp: useCharge))
			{
				return true;
			}
		}

		if (UsePotion && InCombat && InAstralFire && (HasBuffs || HasLeyLines || CombatElapsedLess(10)))
		{
			if (UseBurstMedicine(out act))
			{
				return true;
			}
		}

		if (UseRetrace && InCombat && !IsLastAbility(ActionID.LeyLinesPvE) && RetracePvE.CanUse(out act))
		{
			return true;
		}

		if (!BlizzardIiiPvE.Info.EnoughLevelAndQuest() && InUmbralIce && CurrentMp < 7500 && LucidDreamingPvE.CanUse(out act))
		{
			return true;
		}

		return base.AttackAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.TriplecastPvE, ActionID.SwiftcastPvE, ActionID.RetracePvE)]
	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		if (IsMoving && InCombat && HasHostilesInRange && !NextGCDisInstant)
		{
			var nextNeedsCast = nextGCD == AddlePvE || (nextGCD is IBaseAction nextBase && nextBase.Info.CastTime > 0f);
			if (nextNeedsCast)
			{
				if (UseTriplecastForMovement && TriplecastPvE.CanUse(out act, usedUp: true))
				{
					return true;
				}
				if (UseSwiftcastForMovement && SwiftcastPvE.CanUse(out act))
				{
					return true;
				}
			}
		}

		if (UseRetrace && InCombat && HasHostilesInMaxRange && !IsLastAbility(ActionID.LeyLinesPvE) && RetracePvE.CanUse(out act))
		{
			return true;
		}

		return base.GeneralAbility(nextGCD, out act);
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		if (!HasHostilesInMaxRange)
		{
			if (InCombat && _noHostilesSinceMs == 0)
			{
				_noHostilesSinceMs = Environment.TickCount64;
			}
			if (TryDowntimeMaintenance(out act))
			{
				return true;
			}
			return base.GeneralGCD(out act);
		}

		_noHostilesSinceMs = 0;

		if (!InAstralFire && !InUmbralIce)
		{
			if (TryEnterStanceFromNeutral(out act))
			{
				return true;
			}
		}
		else if (IsAoeMode)
		{
			if (TryAoeGCD(out act))
			{
				return true;
			}
		}
		else
		{
			if (TrySingleTargetGCD(out act))
			{
				return true;
			}
		}

		if (TryMovementFallbackGCD(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}

	private bool TryDowntimeMaintenance(out IAction? act)
	{
		act = null;

		if (InUmbralIce && UmbralSoulPvE.Info.EnoughLevelAndQuest())
		{
			var needsIceStacks = UmbralIceStacks < MaxSoulCount;
			var needsMp = CurrentMp < MaxMp;
			var needsHearts = UmbralHeartTrait.EnoughLevel && UmbralHearts < 3;
			var ready = InCombat
				? _noHostilesSinceMs != 0 && Environment.TickCount64 - _noHostilesSinceMs >= NoHostilesDowntimeMs
				: TimeSinceLastAction.TotalSeconds >= OutOfCombatUmbralSoulSeconds;

			if (ready && (needsIceStacks || needsMp || needsHearts) && UmbralSoulPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (InAstralFire
			&& UmbralSoulPvE.Info.EnoughLevelAndQuest()
			&& AstralSoulStacks < 6
			&& (CurrentMp < MaxMp || (UmbralHeartTrait.EnoughLevel && UmbralHearts < 3))
			&& IsDowntimeReady
			&& TransposePvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		return false;
	}

	private bool TryEnterStanceFromNeutral(out IAction? act)
	{
		act = null;

		if (IsAoeMode)
		{
			if (UmbralHeartTrait.EnoughLevel && FlarePvE.Info.EnoughLevelAndQuest())
			{
				if (AoeTargetCount >= 3 && TryCastAoeBlizzard(out act))
				{
					return true;
				}
				if (BlizzardIiiPvE.CanUse(out act))
				{
					return true;
				}
			}

			if (FreezePvE.Info.EnoughLevelAndQuest())
			{
				if (BlizzardIiPvE.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}

			if (CurrentMp >= MaxMp && FireIiPvE.EnoughLevel && TryCastAoeFire(out act))
			{
				return true;
			}

			if (TryCastAoeBlizzard(out act))
			{
				return true;
			}
		}

		if (FireIiiPvE.EnoughLevel)
		{
			if (CurrentMp >= NeutralFireIiiMinMp && FireIiiPvE.CanUse(out act))
			{
				return true;
			}
			if (BlizzardIiiPvE.Info.EnoughLevelAndQuest() && BlizzardIiiPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (CurrentMp >= FirePvE.Info.MPNeed && FirePvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		return BlizzardPvE.CanUse(out act);
	}

	private bool TrySingleTargetGCD(out IAction? act)
	{
		if (!BlizzardIiiPvE.Info.EnoughLevelAndQuest())
		{
			return InUmbralIce ? TryStLv30IceGCD(out act) : TryStLv30FireGCD(out act);
		}

		return InUmbralIce ? TrySingleTargetIceGCD(out act) : TrySingleTargetFireGCD(out act);
	}

	private bool TryStLv30IceGCD(out IAction? act)
	{
		act = null;

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (CurrentMp < MaxMp && BlizzardPvE.CanUse(out act))
		{
			return true;
		}

		if (CurrentMp >= MaxMp && TransposePvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		return BlizzardPvE.CanUse(out act);
	}

	private bool TryStLv30FireGCD(out IAction? act)
	{
		act = null;

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (CurrentMp >= FirePvE.Info.MPNeed && FirePvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (NeedManafont && ManafontPvE.CanUse(out act, skipStatusProvideCheck: true, skipCastingCheck: true))
		{
			return true;
		}

		return TransposePvE.CanUse(out act, skipCastingCheck: true);
	}

	private bool TrySingleTargetIceGCD(out IAction? act)
	{
		act = null;

		if (UmbralIceStacks < MaxSoulCount && BlizzardIiiPvE.CanUse(out act))
		{
			return true;
		}

		if (IsPolyglotStacksMaxed && EnochianEndAfterGCD(2) && TryUsePolyglot(out act, forceSpend: true))
		{
			return true;
		}

		if (UmbralHeartTrait.EnoughLevel && BlizzardIvPvE.Info.EnoughLevelAndQuest() && UmbralHearts < 3)
		{
			if (BlizzardIvPvE.CanUse(out act))
			{
				return true;
			}
		}
		else if (CurrentMp < MaxMp)
		{
			if (BlizzardPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (IsParadoxActive && ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (TryUsePolyglot(out act))
		{
			return true;
		}

		if (ReadyToTransposeToFireST)
		{
			if (!TransposePvE.Cooldown.HasOneCharge
				&& TransposePvE.Cooldown.RecastTimeRemain <= 1.2f
				&& EnhancedFoulTrait.EnoughLevel
				&& TryUsePolyglot(out act, forceSpend: true))
			{
				return true;
			}
			if (TransposePvE.CanUse(out act, skipCastingCheck: true))
			{
				return true;
			}
		}

		if (UmbralIceStacks == MaxSoulCount
			&& CurrentMp >= MaxMp
			&& (!UmbralHeartTrait.EnoughLevel || !BlizzardIvPvE.Info.EnoughLevelAndQuest() || UmbralHearts == 3)
			&& FireIiiPvE.CanUse(out act))
		{
			return true;
		}

		return false;
	}

	private bool TrySingleTargetFireGCD(out IAction? act)
	{
		act = null;

		if (AstralFireStacks < MaxSoulCount)
		{
			if (HasFire && FireIiiPvE.CanUse(out act))
			{
				return true;
			}
			if (IsParadoxActive && CurrentMp >= ParadoxAfCost && ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
			if (CurrentMp >= FireIiiPvE.Info.MPNeed + BaseFireIvCost && FireIiiPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (HasPendingFlareStar)
		{
			if (FlareStarPvE.CanUse(out act))
			{
				return true;
			}
			if (IsMoving)
			{
				return false;
			}
		}

		if (TryUsePolyglot(out act))
		{
			return true;
		}

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (CanUseFlareStar)
		{
			if (IsAltFlarePreManafontDespair && DespairPvE.CanUse(out act))
			{
				return true;
			}

			if (CanReachFlareStarViaFireIvST && AstralSoulStacks < 6 && CurrentMp >= (uint)FireIvCost)
			{
				if (FireIvPvE.CanUse(out act))
				{
					return true;
				}
			}
			else if (CanReachFlareStarViaFlareST)
			{
				var paradoxReserve = ShouldCastAfParadox ? ParadoxAfCost : 0;
				if ((AstralSoulStacks < 3 || (AstralSoulStacks < 5 && CurrentMp >= (uint)(FireIvCost + paradoxReserve + FlareMinCost)))
					&& CurrentMp >= (uint)FireIvCost)
				{
					if (FireIvPvE.CanUse(out act))
					{
						return true;
					}
				}
				if (ShouldCastAfParadox && ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true))
				{
					return true;
				}
				if (AstralSoulStacks >= 3 && CurrentMp >= FlareMinCost && FlarePvE.CanUse(out act, skipAoeCheck: true))
				{
					return true;
				}
			}
			else
			{
				var endReserve = (ShouldCastAfParadox ? ParadoxAfCost : 0) + DespairMinCost;
				if (AstralSoulStacks < 6 && CurrentMp >= (uint)(FireIvCost + endReserve) && FireIvPvE.CanUse(out act))
				{
					return true;
				}
				if (ShouldCastAfParadox && ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true))
				{
					return true;
				}
				if (DespairPvE.EnoughLevel && CurrentMp >= DespairMinCost && DespairPvE.CanUse(out act))
				{
					return true;
				}
			}
		}
		else if (ParadoxPvE.EnoughLevel)
		{
			var afReserve = (ShouldCastAfParadox ? ParadoxAfCost : 0) + (DespairPvE.EnoughLevel ? DespairMinCost : 0);
			if (CurrentMp >= (uint)(FireIvCost + afReserve) && FireIvPvE.CanUse(out act))
			{
				return true;
			}
			if (ShouldCastAfParadox && ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
			if (DespairPvE.EnoughLevel && CurrentMp >= DespairMinCost && DespairPvE.CanUse(out act))
			{
				return true;
			}
		}
		else if (FireIvPvE.Info.EnoughLevelAndQuest())
		{
			var despairReserve = DespairPvE.EnoughLevel ? DespairMinCost : 0;
			if (CurrentMp >= (uint)(FireIvCost + despairReserve) && FireIvPvE.CanUse(out act))
			{
				return true;
			}
			if (DespairPvE.EnoughLevel && CurrentMp >= DespairMinCost && DespairPvE.CanUse(out act))
			{
				return true;
			}
		}
		else
		{
			if (HasFire && CurrentMp >= FirePvE.Info.MPNeed && FireIiiPvE.CanUse(out act))
			{
				return true;
			}
			if (CurrentMp >= FirePvE.Info.MPNeed && FirePvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}
			if (FlarePvE.Info.EnoughLevelAndQuest()
				&& Service.Config.AoEType != ConfigTypes.AoEType.Off
				&& CurrentMp >= FlareMinCost
				&& FlarePvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (NeedManafont)
		{
			if (EnhancedFoulTrait.EnoughLevel && TryUsePolyglot(out act, forceSpend: true))
			{
				return true;
			}
			if (ManafontPvE.CanUse(out act, skipStatusProvideCheck: true, skipCastingCheck: true))
			{
				return true;
			}
		}

		if (ReadyToGoIceST && BlizzardIiiPvE.CanUse(out act))
		{
			return true;
		}

		return false;
	}

	private bool TryAoeGCD(out IAction? act)
	{
		if (UmbralHeartTrait.EnoughLevel && FlarePvE.Info.EnoughLevelAndQuest())
		{
			return InUmbralIce ? TryAoeModernIceGCD(out act) : TryAoeModernFireGCD(out act);
		}

		if (FlarePvE.Info.EnoughLevelAndQuest())
		{
			return InUmbralIce ? TryAoeLv50IceGCD(out act) : TryAoeLv50FireGCD(out act);
		}

		if (FreezePvE.Info.EnoughLevelAndQuest())
		{
			return InUmbralIce ? TryAoeLv40IceGCD(out act) : TryAoeLv40FireGCD(out act);
		}

		if (BlizzardIiiPvE.Info.EnoughLevelAndQuest())
		{
			return InUmbralIce ? TryAoeLv35IceGCD(out act) : TryAoeLv35FireGCD(out act);
		}

		if (FireIiPvE.EnoughLevel)
		{
			return InUmbralIce ? TryAoeLv18IceGCD(out act) : TryAoeLv18FireGCD(out act);
		}

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (InAstralFire && TransposePvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		return BlizzardIiPvE.CanUse(out act, skipAoeCheck: true);
	}

	private bool TryAoeLv18IceGCD(out IAction? act)
	{
		act = null;

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (CurrentMp < MaxMp && BlizzardIiPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (CurrentMp >= MaxMp && TransposePvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		return BlizzardIiPvE.CanUse(out act, skipAoeCheck: true);
	}

	private bool TryAoeLv18FireGCD(out IAction? act)
	{
		act = null;

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (CurrentMp >= FireIiPvE.Info.MPNeed && FireIiPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (NeedManafont && ManafontPvE.CanUse(out act, skipStatusProvideCheck: true, skipCastingCheck: true))
		{
			return true;
		}

		return CurrentMp < FireIiPvE.Info.MPNeed && TransposePvE.CanUse(out act, skipCastingCheck: true);
	}

	private bool TryAoeLv35IceGCD(out IAction? act)
	{
		act = null;

		if (UmbralIceStacks < MaxSoulCount && BlizzardIiPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (CurrentMp < MaxMp && BlizzardIiPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		return FireIiPvE.CanUse(out act, skipAoeCheck: true);
	}

	private bool TryAoeLv35FireGCD(out IAction? act)
	{
		act = null;

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (CurrentMp >= FireIiPvE.Info.MPNeed && FireIiPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (NeedManafont && ManafontPvE.CanUse(out act, skipStatusProvideCheck: true, skipCastingCheck: true))
		{
			return true;
		}

		return BlizzardIiPvE.CanUse(out act, skipAoeCheck: true);
	}

	private bool TryAoeLv40IceGCD(out IAction? act)
	{
		act = null;

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (!HasThunder && NeedsThunderRefreshSoon() && TransposePvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		return FreezePvE.CanUse(out act, skipAoeCheck: true);
	}

	private bool TryAoeLv40FireGCD(out IAction? act)
	{
		act = null;

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (TransposePvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		if (CurrentMp >= FireIiPvE.Info.MPNeed && FireIiPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		return BlizzardIiPvE.CanUse(out act, skipAoeCheck: true);
	}

	private bool TryAoeLv50IceGCD(out IAction? act)
	{
		act = null;

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (CurrentMp < Lv50AoeComboMinMp && FreezePvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (CurrentMp >= Lv50AoeComboMinMp)
		{
			if (TransposePvE.CanUse(out act, skipCastingCheck: true))
			{
				return true;
			}
			if (FreezePvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		return false;
	}

	private bool TryAoeLv50FireGCD(out IAction? act)
	{
		act = null;

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (CurrentMp >= FireIiPvE.Info.MPNeed + FlareMinCost && FireIiPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (CurrentMp >= FlareMinCost && FlarePvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (NeedManafont && ManafontPvE.CanUse(out act, skipStatusProvideCheck: true, skipCastingCheck: true))
		{
			return true;
		}

		if (CurrentMp < FlareMinCost && TransposePvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		return false;
	}

	private bool TryAoeModernIceGCD(out IAction? act)
	{
		act = null;

		if (UmbralHearts == 0 || CurrentMp < ModernAoeMinMp)
		{
			if (AoeTargetCount >= 3 && FreezePvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
			if (AoeTargetCount < 3 && BlizzardIvPvE.Info.EnoughLevelAndQuest() && UmbralHearts < 3 && BlizzardIvPvE.CanUse(out act))
			{
				return true;
			}
			if (FreezePvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (UmbralHearts > 0 && CurrentMp >= ModernAoeMinMp)
		{
			var waitingOnTranspose = !TransposePvE.Cooldown.HasOneCharge;

			if (TryUseThunder(out act, forAoeFiller: waitingOnTranspose))
			{
				return true;
			}

			if (IsParadoxActive && ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true))
			{
				return true;
			}

			if (TryUsePolyglot(out act, forceSpend: waitingOnTranspose))
			{
				return true;
			}

			if (TransposePvE.CanUse(out act, skipCastingCheck: true))
			{
				return true;
			}

			if (AoeTargetCount >= 3 && FreezePvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}

			if (BlizzardIvPvE.CanUse(out act))
			{
				return true;
			}

			if (FreezePvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		return false;
	}

	private bool TryAoeModernFireGCD(out IAction? act)
	{
		act = null;

		if (HasPendingFlareStar)
		{
			if (FlareStarPvE.CanUse(out act))
			{
				return true;
			}
			if (IsMoving)
			{
				return false;
			}
		}

		if (CurrentMp >= FlareMinCost)
		{
			return FlarePvE.CanUse(out act, skipAoeCheck: true);
		}

		if (NeedManafont)
		{
			if (EnhancedFoulTrait.EnoughLevel && TryUsePolyglot(out act, forceSpend: true))
			{
				return true;
			}
			if (TryUseThunder(out act))
			{
				return true;
			}
			if (ManafontPvE.CanUse(out act, skipStatusProvideCheck: true, skipCastingCheck: true))
			{
				return true;
			}
		}

		if (TryUseThunder(out act))
		{
			return true;
		}

		if (PolyglotStacks >= 2 && EnhancedFoulTrait.EnoughLevel && TryUsePolyglot(out act, forceSpend: true))
		{
			return true;
		}

		if (TryUsePolyglot(out act))
		{
			return true;
		}

		if (TransposePvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		return false;
	}

	private bool TryMovementFallbackGCD(out IAction? act)
	{
		act = null;

		if (!IsMoving || !InCombat || !HasHostilesInRange)
		{
			return false;
		}

		if (InUmbralIce && IsParadoxActive && ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (InAstralFire && ShouldCastAfParadox && ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (EnhancedFoulTrait.EnoughLevel && TryUsePolyglot(out act, forceSpend: true))
		{
			return true;
		}

		if (TryUseThunder(out act, forMovement: true))
		{
			return true;
		}

		if (!NextGCDisInstant && (NextAbilityToNextGCD <= MovementWeaveCutoff || WeaponRemain == 0f))
		{
			if (UseTriplecastForMovement && TriplecastPvE.CanUse(out act, usedUp: true))
			{
				return true;
			}
			if (UseSwiftcastForMovement && SwiftcastPvE.CanUse(out act))
			{
				return true;
			}
		}

		if (IsParadoxActive && ParadoxPvE.CanUse(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (UseScatheWhileMoving && DataCenter.MovingRaw >= SustainedMovementSeconds && ScathePvE.CanUse(out act))
		{
			return true;
		}

		return false;
	}

	private bool NeedsThunderRefreshSoon()
	{
		if (AllHostileTargets == null || !ThunderIiPvE.EnoughLevel)
		{
			return false;
		}

		foreach (var target in AllHostileTargets)
		{
			if (target == null || target.IsDead || target.DistanceToPlayer() > ThunderIiPvE.Info.Range || !target.CanSee())
			{
				continue;
			}

			var ttk = target.GetTTK();
			if (!float.IsNaN(ttk) && ttk <= ThunderTtkThreshold)
			{
				continue;
			}

			if (!target.HasStatus(true, ThunderStatuses) || target.WillStatusEnd(3.0f, true, ThunderStatuses))
			{
				return true;
			}
		}

		return false;
	}

	private bool TryUseThunder(out IAction? act, bool forMovement = false, bool forAoeFiller = false)
	{
		act = null;

		if (!HasThunder)
		{
			return false;
		}

		if (IsLastGCD(
			ActionID.ThunderPvE,
			ActionID.ThunderIiPvE,
			ActionID.ThunderIiiPvE,
			ActionID.ThunderIvPvE,
			ActionID.HighThunderPvE,
			ActionID.HighThunderIiPvE))
		{
			return false;
		}

		var useAoe = ShouldUseAoeThunder;
		IBaseAction thunderAction;

		if (useAoe)
		{
			if (HighThunderIiPvE.EnoughLevel)
			{
				thunderAction = HighThunderIiPvE;
			}
			else if (ThunderIvPvE.EnoughLevel)
			{
				thunderAction = ThunderIvPvE;
			}
			else if (ThunderIiPvE.EnoughLevel)
			{
				thunderAction = ThunderIiPvE;
			}
			else
			{
				return false;
			}
		}
		else
		{
			if (HighThunderPvE.Info.EnoughLevelAndQuest() && ThunderIiiPvE.Info.IsQuestUnlocked())
			{
				thunderAction = HighThunderPvE;
			}
			else if (ThunderIiiPvE.Info.EnoughLevelAndQuest())
			{
				thunderAction = ThunderIiiPvE;
			}
			else
			{
				thunderAction = ThunderPvE;
			}
		}

		if (!thunderAction.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: useAoe))
		{
			return false;
		}

		var target = thunderAction.Target.Target;
		if (target == null)
		{
			act = null;
			return false;
		}

		var ttk = target.GetTTK();
		if (!float.IsNaN(ttk) && ttk <= ThunderTtkThreshold)
		{
			act = null;
			return false;
		}

		if (forMovement)
		{
			if (!target.WillStatusEnd(ThunderMovementRefreshSeconds, true, ThunderStatuses))
			{
				act = null;
				return false;
			}
			return true;
		}

		if (forAoeFiller)
		{
			if (!target.WillStatusEnd(ThunderAoeFillerRefreshSeconds, true, ThunderStatuses))
			{
				act = null;
				return false;
			}
			return true;
		}

		if (target.HasStatus(true, ThunderStatuses) && !target.WillStatusEndGCD(thunderAction.Config.StatusRefreshGcdCount, 0, true, ThunderStatuses))
		{
			act = null;
			return false;
		}

		return true;
	}

	private bool TryUsePolyglot(out IAction? act, bool forceSpend = false)
	{
		act = null;

		if (PolyglotStacks == 0)
		{
			return false;
		}

		var overcapping = (IsPolyglotStacksMaxed && (EnochianEndAfterGCD(2) || (AmplifierPvE.EnoughLevel && AmplifierPvE.Cooldown.WillHaveOneChargeGCD(2))))
			|| (AmplifierPvE.EnoughLevel && PolyglotStacks >= MaxPolyglotStacks - 1 && AmplifierPvE.Cooldown.WillHaveOneChargeGCD(1) && EnochianEndAfterGCD(3));

		var burstDump = !HasPendingFlareStar && (HasBuffs || (IsBurst && HasLeyLines)) && (!KeepOnePolyglotForMovement || PolyglotStacks > 1);

		if (!forceSpend && !overcapping && !burstDump)
		{
			return false;
		}

		if (AoeTargetCount >= 2 || !XenoglossyPvE.EnoughLevel)
		{
			if (FoulPvE.Info.EnoughLevelAndQuest() && FoulPvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		return XenoglossyPvE.CanUse(out act);
	}

	private bool TryCastAoeBlizzard(out IAction? act)
	{
		if (HighBlizzardIiPvE.EnoughLevel)
		{
			return HighBlizzardIiPvE.CanUse(out act, skipAoeCheck: true);
		}
		return BlizzardIiPvE.CanUse(out act, skipAoeCheck: true);
	}

	private bool TryCastAoeFire(out IAction? act)
	{
		if (HighFireIiPvE.EnoughLevel)
		{
			return HighFireIiPvE.CanUse(out act, skipAoeCheck: true);
		}
		return FireIiPvE.CanUse(out act, skipAoeCheck: true);
	}
}
