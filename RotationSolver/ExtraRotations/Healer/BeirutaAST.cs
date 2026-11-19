using System.ComponentModel;

namespace RotationSolver.ExtraRotations.Healer;

[Rotation("BeirutaAST", CombatType.PvE, GameVersion = "7.58", RotationType = RotationType.Reborn, Description =
	"Savage/Ultimate Astrologian. Damage and healing are automatic; mitigation comes from your CD planner or your own presses. AutoBurst off is enough to hold burst. Set Intercept to GCD only and leave ability intercept unticked, so your planner's oGCDs go straight through and any extra heal or shield GCD you press is queued. Questions? Ask @Beiruta on the RSR Discord. Hover each setting for details.")]
[SourceCode(Path = "main/ExtraRotations/Healer/BeirutaAST.cs")]
[ExtraRotation]

public sealed class BeirutaAST : AstrologianRotation
{
	#region Config Options

	#region Utility

	[RotationConfig(CombatType.PvE, Name = "[Utility] Ward Mode (right-click for the macro)", Tooltip =
		"Turn it on for high-pressure phases: keeps everyone topped up.\n" +
		"- Helios / Helios Conjunction as soon as 3+ are below the heal's full-use line (about 85-90%).\n" +
		"- No waiting for AoE oGCDs, Giant Dominance or Macrocosmos; thresholds use real HP and ignore the co-healer and HoTs.\n" +
		"Auto turns off on death, zone change and countdown.")]
	public bool WardMode { get; set; } = false;

	#endregion

	#region Prepull

	[RotationConfig(CombatType.PvE, Name = "[Prepull] Countdown shield", Tooltip =
		"Aspected Benefic + Celestial Intersection on the stance tank at about 5s, or Neutral Sect + Helios Conjunction for the party.")]
	public CountdownShieldStrategy CountdownShield { get; set; } = CountdownShieldStrategy.None;

	[Range(4, 20, ConfigUnitType.Seconds)]
	[RotationConfig(CombatType.PvE, Name = "[Prepull] Earthly Star at", Tooltip =
		"Seconds left on the countdown when Earthly Star is placed. Needs Earthly Star = On cooldown.")]
	public float UseEarthlyStarTime { get; set; } = 4;

	public enum CountdownShieldStrategy : byte
	{
		[Description("None")]
		None,

		[Description("Aspected Benefic + Celestial Intersection on stance tank (5s)")]
		TankAspectedBeneficIntersection,

		[Description("Neutral Sect + Helios Conjunction")]
		NeutralSectHeliosConjunction,
	}

	#endregion

	#region Opener

	[RotationConfig(CombatType.PvE, Name = "[Opener] First Divination", Tooltip =
		"When the first Divination goes out (needs AutoBurst).\n" +
		"0s: sent just before the countdown ends, then Combust, then Lightspeed in its two weave windows.")]
	public DivinationOpenerTiming DivinationOpener { get; set; } = DivinationOpenerTiming.ThreeGcd;

	public enum DivinationOpenerTiming : byte
	{
		[Description("0s")]
		ZeroSeconds,

		[Description("2.2s (1 GCD)")]
		OneGcd,

		[Description("5s (2 GCD)")]
		TwoGcd,

		[Description("7s (3 GCD)")]
		ThreeGcd,
	}

	#endregion

	#region Damage

	[RotationConfig(CombatType.PvE, Name = "[Damage] Use potion")]
	public bool UsePotion { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > First potion", Parent = nameof(UsePotion), Tooltip =
		"Opener: during the countdown. 2 minutes: the first potion waits for the 2-minute burst.")]
	public PotTiming PotionTiming { get; set; } = PotTiming.Opener;

	[RotationConfig(CombatType.PvE, Name = "[Damage] Delay Oracle 5s after Divination", Tooltip =
		"Burst oGCD order: Divination > Lightspeed > potion > Balance / Spear > Lord of Crowns > Draw > Oracle.")]
	public bool DelayOracleForAlignment { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "[Damage] Earthly Star", Tooltip =
		"On cooldown: placed in the countdown and again whenever it is ready. Healing changes while Earthly / Giant Dominance is up.")]
	public EarthlyStarUsage EarthlyStarMode { get; set; } = EarthlyStarUsage.OnCooldown;

	public enum PotTiming : byte
	{
		[Description("Opener (countdown)")]
		Opener,

		[Description("2 minutes")]
		TwoMinutes,
	}

	public enum EarthlyStarUsage : byte
	{
		[Description("On cooldown (and countdown)")]
		OnCooldown,

		[Description("Manual / CD planner")]
		Manual,
	}

	#endregion

	#region Lightspeed

	[RotationConfig(CombatType.PvE, Name = "[Lightspeed] Spare charge", Tooltip =
		"One charge is always kept for burst and used about 5s before Divination. This sets what the other charge does.")]
	public LightspeedOtherChargeUsage LightspeedOtherCharge { get; set; } = LightspeedOtherChargeUsage.Manual;

	public enum LightspeedOtherChargeUsage : byte
	{
		[Description("Manual")]
		Manual,

		[Description("Movement")]
		Movement,

		[Description("Movement, and before capping")]
		MovementAndCap,
	}

	#endregion

	#region Macrocosmos

	[RotationConfig(CombatType.PvE, Name = "[Macrocosmos] Custom triggers (off = CD planner / manual)", Tooltip =
		"A trigger is something you do that makes AST cast Macrocosmos.\n" +
		"In DMU, triggers are blocked while 2+ players have Accretion, or while Earth Resistance Down II is up with one left.")]
	public bool UseMacrocosmosSignals { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Your Surecast", Parent = nameof(UseMacrocosmosSignals))]
	public bool MacrocosmosSignalSurecast { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Your Sprint", Parent = nameof(UseMacrocosmosSignals))]
	public bool MacrocosmosSignalSprint { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Your Synastry (Synastry is then never automatic)", Parent = nameof(UseMacrocosmosSignals))]
	public bool MacrocosmosSignalSynastry { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Your Celestial Intersection on yourself (keeps a charge)", Parent = nameof(UseMacrocosmosSignals), Tooltip =
		"While Macrocosmos is ready within 30s, one Intersection charge is kept for the trigger.")]
	public bool MacrocosmosSignalIntersection { get; set; } = false;

	private bool SignalSurecast => UseMacrocosmosSignals && MacrocosmosSignalSurecast;
	private bool SignalSprint => UseMacrocosmosSignals && MacrocosmosSignalSprint;
	private bool SignalSynastry => UseMacrocosmosSignals && MacrocosmosSignalSynastry;
	private bool SignalIntersection => UseMacrocosmosSignals && MacrocosmosSignalIntersection;

	#endregion

	#region Movement

	[RotationConfig(CombatType.PvE, Name = "[Movement] Early Combust refresh", Tooltip =
		"While moving, refreshes Combust early as an instant GCD.")]
	public bool EarlyDotRefreshWhenMoving { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "[Movement] Use Swiftcast", Tooltip =
		"Instant options while moving: Lightspeed > early Combust > Aspected Benefic (tanks first), then Swiftcast.\n" +
		"Nothing extra is used while Lightspeed or Swiftcast is up.")]
	public bool UseSwiftcastForMovement { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "    > When to use it", Parent = nameof(UseSwiftcastForMovement), Tooltip =
		"Last resort: only when no instant option is available. First: before the instant options.\n" +
		"Raise and AoE heal GCDs only: kept for raise and for moving Helios GCDs.")]
	public SwiftcastMovementPriority SwiftcastMovement { get; set; } = SwiftcastMovementPriority.LastResort;

	public enum SwiftcastMovementPriority : byte
	{
		[Description("Last resort (keep for raise)")]
		LastResort = 0,

		[Description("First (e.g. death = enrage)")]
		First = 2,

		[Description("Raise and AoE heal GCDs only")]
		HealOnly = 3,
	}

	#endregion

	#region Healing

	[RotationConfig(CombatType.PvE, Name = "[Healing] Custom healing settings (off = defaults)", Tooltip =
		"Off: AoE thresholds come from each heal's potency, Neutral Sect, the co-healer's smallest GCD heal and incoming HoT ticks, so heals are not wasted.\n" +
		"Off also means: Helios Conjunction after Horoscope, Essential Dignity below 70% / 50% / 30% (3 / 2 / 1 charges), Synastry second target below 50%.\n" +
		"On: the values below replace the Ward-off thresholds. Heal area = 3+ players below that HP, like RSR's own heal-area settings. Ward Mode always uses its own.")]
	public bool UseCustomHPThreshold { get; set; } = false;

	[RotationConfig(CombatType.PvE, Name = "    > Helios Conjunction after Horoscope", Parent = nameof(UseCustomHPThreshold), Tooltip =
		"After you press Horoscope, the next Helios Conjunction upgrades it to Horoscope Helios.")]
	public bool AutoUpgradeHoroscope { get; set; } = true;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Lady of Crowns: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float LadyOfHeals { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Stellar Detonation: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float StellarDetonationHeal { get; set; } = 0.65f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Horoscope: heal area, Horoscope holders only (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float HoroscopeHeal { get; set; } = 0.8f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Celestial Opposition: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float CelestialOppositionHeal { get; set; } = 0.65f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Helios Conjunction: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float HeliosConjunctionHeal { get; set; } = 0.65f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Helios: heal area (Ward off)", Parent = nameof(UseCustomHPThreshold))]
	public float HeliosHeal { get; set; } = 0.6f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Essential Dignity 3 charges: below", Parent = nameof(UseCustomHPThreshold))]
	public float EssentialDignityThird { get; set; } = 0.7f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Essential Dignity 2 charges: below", Parent = nameof(UseCustomHPThreshold))]
	public float EssentialDignitySecond { get; set; } = 0.5f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Essential Dignity 1 charge: below", Parent = nameof(UseCustomHPThreshold))]
	public float EssentialDignityLast { get; set; } = 0.3f;

	[Range(0, 1, ConfigUnitType.Percent)]
	[RotationConfig(CombatType.PvE, Name = "    > Synastry second target: below", Parent = nameof(UseCustomHPThreshold))]
	public float SynastryHeal { get; set; } = 0.5f;

	private bool AutoUpgradeHoroscopeValue => !UseCustomHPThreshold || AutoUpgradeHoroscope;
	private float EssentialDignityThirdValue => UseCustomHPThreshold ? EssentialDignityThird : 0.7f;
	private float EssentialDignitySecondValue => UseCustomHPThreshold ? EssentialDignitySecond : 0.5f;
	private float EssentialDignityLastValue => UseCustomHPThreshold ? EssentialDignityLast : 0.3f;
	private float SynastryHealValue => UseCustomHPThreshold ? SynastryHeal : 0.5f;

	#endregion

	#region DMU

	[RotationConfig(CombatType.PvE, Name = "[DMU] Experimental Accretion top-up", Tooltip =
		"Fills Accretion players in order: the healer first, then the DPS once Earth Resistance Down II is gone.\n" +
		"Waits until 3+ non-tanks other than the target are above 30%, and 3s after Accretion changes.\n" +
		"AST: Essential Dignity, then Benefic II once Dignity is out of charges. Microcosmos fills the last Accretion DPS if they have your Macrocosmos (no 30% check).\n" +
		"Always active in DMU, even with this off: group heals stop once an Accretion player is above 50%.")]
	public bool AccretionTopUp { get; set; } = false;

	#endregion

	#endregion

	#region Constants / Fields

	private const long DivinationFirst5sMs = 5_000;
	private const float BallparkPercent = 0.08f;
	private const float DivinationCombustRefreshSeconds = 11f;
	private const float MovementLeadSeconds = 0.5f;
	private const float MovementGraceSeconds = 0.3f;
	private const float MovementTimeThreshold = 1.0f;
	private const float CombustWorthRefreshSeconds = 15f;
	private const float LightspeedLocksSwiftcastSeconds = 18f;
	private const float SwiftcastLocksLightspeedSeconds = 3f;
	private const float LightspeedBurstLeadSeconds = 5f;

	private long _divinationUsedAtMs;
	private float _lastLightspeedUseCombatTime = float.MinValue;
	private float _lastSwiftcastUseCombatTime = float.MinValue;
	private DateTime _neutralSectStartedAt = DateTime.MinValue;
	private bool _neutralSectWasUp;
	private bool _lastWardMode;
	private readonly BeirutaRaidwide _raidwide = new();

	#endregion

	#region Burst / Opener

	private float DivinationOpenerSeconds => DivinationOpener switch
	{
		DivinationOpenerTiming.ZeroSeconds => 0f,
		DivinationOpenerTiming.OneGcd => 2.2f,
		DivinationOpenerTiming.TwoGcd => 5.1f,
		_ => 7.2f,
	};

	private bool IsOpen => InCombat && CombatTime < DivinationOpenerSeconds;

	private bool DivinationOpenerPassed => InCombat && !CombatElapsedLess(DivinationOpenerSeconds);

	private bool InFirst5sAfterDivination =>
		_divinationUsedAtMs != 0 &&
		Environment.TickCount64 - _divinationUsedAtMs < DivinationFirst5sMs;

	private bool OracleGatedByDivination => DelayOracleForAlignment && InFirst5sAfterDivination;

	private float DivIn =>
		DivinationPvE.Cooldown.CurrentCharges >= 1
			? 0f
			: DivinationPvE.Cooldown.RecastTimeRemainOneCharge;

	private float DivinationDueIn
	{
		get
		{
			if (!DivinationPvE.EnoughLevel)
			{
				return float.MaxValue;
			}

			var openerWait = InCombat ? MathF.Max(0f, DivinationOpenerSeconds - CombatTime) : DivinationOpenerSeconds;
			return MathF.Max(DivIn, openerWait);
		}
	}

	private bool BurstPrep => DivinationPvE.EnoughLevel && DivinationDueIn <= LightspeedBurstLeadSeconds;

	private bool BurstCardsAllowed => !DivinationPvE.EnoughLevel || HasDivination;

	private static bool IsPartyMedicated
	{
		get
		{
			foreach (var member in PartyMembers)
			{
				if (member != null && member.HasStatus(false, StatusID.Medicated))
				{
					return true;
				}
			}
			return false;
		}
	}

	private bool ShouldUsePotionInCombat =>
		UsePotion &&
		InCombat &&
		IsBurst &&
		!OpenerWeaveHold &&
		!(PotionTiming == PotTiming.TwoMinutes && CombatElapsedLess(30f)) &&
		(IsPartyMedicated || HasDivination || BurstPrep);

	private bool ShouldUseLightspeedForBurst =>
		IsBurst &&
		InCombat &&
		!HasLightspeed &&
		LightspeedPvE.EnoughLevel &&
		DivinationPvE.EnoughLevel &&
		(BurstPrep || InFirst5sAfterDivination);

	private bool DrawReadySoon(uint gcds) =>
		(AstralDrawPvE.EnoughLevel && AstralDrawPvE.Cooldown.WillHaveOneChargeGCD(gcds)) ||
		(UmbralDrawPvE.EnoughLevel && UmbralDrawPvE.Cooldown.WillHaveOneChargeGCD(gcds));

	private bool ShouldUseLord =>
		HasLord &&
		(HasDivination || !DivinationPvE.EnoughLevel || DrawReadySoon(1));

	private bool TryPriorityWeaves(out IAction? act)
	{
		act = null;

		if (!InCombat)
		{
			return false;
		}

		if (IsBurst && DivinationOpenerPassed && DivinationPvE.CanUse(out act))
		{
			_divinationUsedAtMs = Environment.TickCount64;
			return true;
		}

		if (OpenerWeaveHold)
		{
			return false;
		}

		if (ShouldUseLightspeedForBurst && LightspeedPvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		if (ShouldUsePotionInCombat && UseBurstMedicine(out act))
		{
			return true;
		}

		if (BurstCardsAllowed && (TheBalancePvE.CanUse(out act) || TheSpearPvE.CanUse(out act)))
		{
			return true;
		}

		if (ShouldUseLord && LordOfCrownsPvE.CanUse(out act))
		{
			return true;
		}

		if (HasLady &&
			!HasDivination &&
			!AccretionGroupHealBlocked &&
			DrawReadySoon(1) &&
			LadyOfCrownsPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		if (TryDraw(out act))
		{
			return true;
		}

		if (!OracleGatedByDivination && OraclePvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private static bool HoldingDamageCard => HasBalance || HasSpear || HasLord;

	private bool TryDraw(out IAction? act)
	{
		act = null;
		return !HoldingDamageCard &&
			(AstralDrawPvE.CanUse(out act) || UmbralDrawPvE.CanUse(out act));
	}

	#endregion

	#region Lightspeed / Movement

	private bool MovingPastGrace => InCombat && IsMoving && MovingTime > MovementGraceSeconds;

	private bool HasSufficientMovement => InCombat && IsMoving && MovingTime > MovementTimeThreshold;

	private bool MovingPastLead => HasSufficientMovement && MovingTime > MovementTimeThreshold + MovementLeadSeconds;

	private bool ShouldHoldRaiseSwift =>
		(HasSwift || IsLastAction(ActionID.SwiftcastPvE)) &&
		MergedStatus.HasFlag(AutoStatus.Raise);

	private bool IsLightspeedLockingSwiftcastActive =>
		InCombat &&
		_lastLightspeedUseCombatTime > float.MinValue / 2 &&
		CombatTime - _lastLightspeedUseCombatTime <= LightspeedLocksSwiftcastSeconds;

	private bool IsSwiftcastLockingLightspeedActive =>
		InCombat &&
		_lastSwiftcastUseCombatTime > float.MinValue / 2 &&
		CombatTime - _lastSwiftcastUseCombatTime <= SwiftcastLocksLightspeedSeconds;

	private float LightspeedNextChargeIn =>
		LightspeedPvE.Cooldown.CurrentCharges >= LightspeedPvE.Cooldown.MaxCharges
			? 0f
			: LightspeedPvE.Cooldown.RecastTimeRemainOneCharge;

	private bool HoldLastLightspeedForDivination
	{
		get
		{
			if (!DivinationPvE.EnoughLevel ||
				DivinationDueIn > 60f ||
				BurstPrep ||
				LightspeedPvE.Cooldown.CurrentCharges != 1 ||
				HasLightspeed)
			{
				return false;
			}

			var lsMustBeBackBy = MathF.Max(0f, DivinationDueIn - LightspeedBurstLeadSeconds);
			return LightspeedNextChargeIn > lsMustBeBackBy;
		}
	}

	private void UpdateMovementCooldownTracking()
	{
		if (!InCombat)
		{
			_lastLightspeedUseCombatTime = float.MinValue;
			_lastSwiftcastUseCombatTime = float.MinValue;
			return;
		}

		if (IsLastAction(ActionID.LightspeedPvE))
		{
			_lastLightspeedUseCombatTime = CombatTime;
		}

		if (IsLastAction(ActionID.SwiftcastPvE))
		{
			_lastSwiftcastUseCombatTime = CombatTime;
		}
	}

	private bool LightspeedReadyForMovement =>
		LightspeedOtherCharge != LightspeedOtherChargeUsage.Manual &&
		MovingPastLead &&
		!HasLightspeed &&
		!HasSwift &&
		LightspeedPvE.EnoughLevel &&
		!HoldLastLightspeedForDivination &&
		!IsSwiftcastLockingLightspeedActive &&
		LightspeedPvE.CanUse(out _, usedUp: LightspeedPvE.Cooldown.CurrentCharges > 1);

	private bool LightspeedCapping =>
		LightspeedOtherCharge == LightspeedOtherChargeUsage.MovementAndCap &&
		InCombat &&
		!HasLightspeed &&
		LightspeedPvE.EnoughLevel &&
		LightspeedPvE.Cooldown.MaxCharges >= 2 &&
		LightspeedPvE.Cooldown.WillHaveXCharges(LightspeedPvE.Cooldown.MaxCharges, 5f);

	private bool SwiftcastReadyForMovement =>
		UseSwiftcastForMovement &&
		MovingPastLead &&
		!HasSwift &&
		!HasLightspeed &&
		SwiftcastPvE.EnoughLevel &&
		SwiftcastPvE.IsEnabled &&
		!SwiftcastPvE.Cooldown.IsCoolingDown &&
		!ShouldHoldRaiseSwift &&
		!IsLightspeedLockingSwiftcastActive;

	private bool MovementCombustDue(float expectedHPToLive12Seconds) =>
		EarlyDotRefreshWhenMoving &&
		HasSufficientMovement &&
		CurrentTarget != null &&
		CurrentTargetCombustMissingOrEnding(CombustWorthRefreshSeconds) &&
		CurrentTargetHasEnoughHpForCombust(expectedHPToLive12Seconds);

	private static IBattleChara? MovementAspectedBeneficTarget() =>
		FindLowest(SingleHealRange, static m => IsTankMember(m) && m.GetHealthRatio() < 1f && !HasAspectedBeneficFromSelf(m)) ??
		FindLowest(SingleHealRange, static m => m.GetHealthRatio() < 1f && !HasAspectedBeneficFromSelf(m));

	private bool HasCheaperMovementOption =>
		LightspeedReadyForMovement ||
		MovementCombustDue(GetExpectedHpToLive12Seconds()) ||
		(AspectedBeneficPvE.EnoughLevel && AspectedBeneficPvE.IsEnabled && MovementAspectedBeneficTarget() != null);

	private bool IsMovementPreferredNextGCD(IAction nextGCD)
	{
		if (nextGCD == null)
		{
			return false;
		}

		if (nextGCD.IsTheSameTo(false, AspectedBeneficPvE, MacrocosmosPvE))
		{
			return true;
		}

		return CanUseCurrentCombust(out var combustAct) && nextGCD == combustAct;
	}

	private bool ShouldUseSwiftcastForMovement(IAction nextGCD)
	{
		if (!SwiftcastReadyForMovement || IsLastAction(ActionID.SwiftcastPvE))
		{
			return false;
		}

		if (SwiftcastMovement == SwiftcastMovementPriority.HealOnly)
		{
			return !MergedStatus.HasFlag(AutoStatus.Raise) && HeliosGcdWanted;
		}

		if (IsMovementPreferredNextGCD(nextGCD))
		{
			return false;
		}

		return SwiftcastMovement == SwiftcastMovementPriority.First || !HasCheaperMovementOption;
	}

	private bool ShouldUseLightspeedForMovement(IAction nextGCD)
	{
		if (!LightspeedReadyForMovement ||
			IsLastAction(ActionID.LightspeedPvE) ||
			IsMovementPreferredNextGCD(nextGCD))
		{
			return false;
		}

		return !(SwiftcastMovement == SwiftcastMovementPriority.First && SwiftcastReadyForMovement);
	}

	private bool TryMovementGCD(float expectedHPToLive12Seconds, out IAction? act)
	{
		act = null;

		if (!MovingPastGrace || HasSwift || HasLightspeed || ShouldHoldRaiseSwift)
		{
			return false;
		}

		if ((SwiftcastMovement == SwiftcastMovementPriority.First && SwiftcastReadyForMovement) ||
			LightspeedReadyForMovement)
		{
			return false;
		}

		if (MovementCombustDue(expectedHPToLive12Seconds) &&
			CanUseCurrentCombust(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (MovementAspectedBeneficTarget() is { } target &&
			BeirutaAccretion.TryUseOn(AspectedBeneficPvE, target, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	#endregion

	#region Damage / DoT Helpers

	private bool CanUseCurrentCombust(out IAction? act, bool skipStatusProvideCheck = false)
	{
		if (CombustIiiPvE.EnoughLevel && CombustIiiPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
		{
			return true;
		}

		if (!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel && CombustIiPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
		{
			return true;
		}

		if (!CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel && CombustPvE.CanUse(out act, skipStatusProvideCheck: skipStatusProvideCheck))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool CanUseCurrentGravity(out IAction? act)
	{
		if (GravityIiPvE.EnoughLevel && GravityIiPvE.CanUse(out act))
		{
			return true;
		}

		if (!GravityIiPvE.EnoughLevel && GravityPvE.EnoughLevel && GravityPvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool CanUseCurrentMalefic(out IAction? act)
	{
		if (FallMaleficPvE.EnoughLevel && FallMaleficPvE.CanUse(out act))
		{
			return true;
		}

		if (!FallMaleficPvE.EnoughLevel && MaleficIvPvE.EnoughLevel && MaleficIvPvE.CanUse(out act))
		{
			return true;
		}

		if (!MaleficIvPvE.EnoughLevel && MaleficIiiPvE.EnoughLevel && MaleficIiiPvE.CanUse(out act))
		{
			return true;
		}

		if (!MaleficIiiPvE.EnoughLevel && MaleficIiPvE.EnoughLevel && MaleficIiPvE.CanUse(out act))
		{
			return true;
		}

		if (!MaleficIiPvE.Info.EnoughLevelAndQuest() && MaleficPvE.CanUse(out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool CurrentTargetCombustMissingOrEnding(float remainingSeconds)
	{
		if (CurrentTarget == null)
		{
			return false;
		}

		return
			(CombustIiiPvE.EnoughLevel &&
				(!CurrentTarget.HasStatus(true, StatusID.CombustIii) ||
				 CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.CombustIii)))
			||
			(!CombustIiiPvE.EnoughLevel && CombustIiPvE.EnoughLevel &&
				(!CurrentTarget.HasStatus(true, StatusID.CombustIi) ||
				 CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.CombustIi)))
			||
			(!CombustIiiPvE.EnoughLevel && !CombustIiPvE.EnoughLevel && CombustPvE.EnoughLevel &&
				(!CurrentTarget.HasStatus(true, StatusID.Combust) ||
				 CurrentTarget.WillStatusEnd(remainingSeconds, true, StatusID.Combust)));
	}

	private float GetExpectedHpToLive12Seconds()
	{
		if (Player == null)
		{
			return 0f;
		}

		var alive = 0;
		foreach (var member in PartyMembers)
		{
			if (!member.IsDead)
			{
				alive++;
			}
		}

		return BallparkPercent * Player.MaxHp * Math.Max(1, alive) * 12f;
	}

	private bool CurrentTargetHasEnoughHpForCombust(float expectedHpToLive12Seconds) =>
		CurrentTarget != null &&
		CurrentTarget.CurrentHp >= expectedHpToLive12Seconds;

	#endregion

	#region Healing Engine

	private const float SingleHealRange = 30f;
	private const float PartyHealRange = 20f;
	private const float HeliosRange = 15f;
	private const int AoeInjuredCount = 3;
	private const float CriticalHp = 0.3f;
	private const float CapTankHp = 0.9f;
	private const float NeutralSectTankHp = 0.9f;
	private const float UndeadReleaseSeconds = 1f;
	private const float EarthlyDominanceGateSeconds = 3f;
	private const float FreshMitigationSeconds = 6f;
	private const float RampartDuration = 20f;
	private const float BigMitigationDuration = 15f;
	private const float LadyPotency = 400f;
	private const float HoroscopeHeliosPotency = 400f;
	private const float StellarDetonationPotency = 720f;
	private const float OppositionDirectPotency = 400f;
	private const float OppositionHotCountedPotency = 100f * BeirutaHealModel.HotCreditTicks;
	private const float OppositionCountedPotency = OppositionDirectPotency + OppositionHotCountedPotency;
	private const float HeliosPotency = 400f;
	private const float HeliosWardOffCap = 0.6f;
	private const float ConjunctionDirectPotency = 250f;
	private const float ConjunctionHotCountedPotency = 175f * BeirutaHealModel.HotCreditTicks;
	private const float ConjunctionCountedPotency = ConjunctionDirectPotency + ConjunctionHotCountedPotency;
	private const float NeutralSectBoost = 1.2f;

	private static readonly StatusID[] FullInvulnStatus =
	[
		StatusID.HallowedGround,
		StatusID.HallowedGround_1302,
		StatusID.Superbolide,
	];

	private static readonly StatusID[] UndeadStatus =
	[
		StatusID.Holmgang_409,
		StatusID.WalkingDead,
	];

	private static readonly StatusID[] TankBigMitigationStatus =
	[
		StatusID.Rampart,
		StatusID.Rampart_1191,
		StatusID.Sentinel,
		StatusID.Guardian,
		StatusID.Vengeance,
		StatusID.Damnation,
		StatusID.ShadowWall,
		StatusID.ShadowedVigil,
		StatusID.Nebula,
		StatusID.GreatNebula,
	];

	private static readonly StatusID[] HeliosHotStatus =
	[
		StatusID.HeliosConjunction,
		StatusID.AspectedHelios,
	];

	private static readonly StatusID[] MacrocosmosStatus =
	[
		StatusID.Macrocosmos,
		StatusID.Macrocosmos_3104,
		StatusID.Macrocosmos_3989,
	];

	private static bool CanRunHealEngine => Service.Config.AutoHeal && InCombat;

	private static bool AccretionGroupHealBlocked => BeirutaAccretion.GroupHealBlocked(PartyMembers);

	private static bool IsTankMember(IBattleChara member) => member.IsJobCategory(JobRole.Tank);

	private static bool IsSelf(IBattleChara member) => member.GameObjectId == Player?.GameObjectId;

	private static bool IsHealBlocked(IBattleChara member) =>
		member.HasStatus(false, FullInvulnStatus) ||
		member.HasStatus(false, StatusID.LivingDead) ||
		(member.HasStatus(false, UndeadStatus) && member.StatusTime(false, UndeadStatus) > UndeadReleaseSeconds);

	private static bool IsJustOffUndead(IBattleChara member) =>
		member.HasStatus(false, UndeadStatus) &&
		member.StatusTime(false, UndeadStatus) <= UndeadReleaseSeconds;

	private static bool HasFreshBigMitigation(IBattleChara member)
	{
		foreach (var status in TankBigMitigationStatus)
		{
			if (!member.HasStatus(false, status))
			{
				continue;
			}

			var duration = status is StatusID.Rampart or StatusID.Rampart_1191 ? RampartDuration : BigMitigationDuration;
			if (duration - member.StatusTime(false, status) <= FreshMitigationSeconds)
			{
				return true;
			}
		}

		return false;
	}

	private static bool IsFreshMitigatingTank(IBattleChara member) =>
		IsTankMember(member) &&
		!IsHealBlocked(member) &&
		HasFreshBigMitigation(member);

	private float LandingHotPotency(IBattleChara member)
	{
		var potency = 0f;

		if (IsHealableInRange(member, PartyHealRange) &&
			!member.HasStatus(false, StatusID.Opposition) &&
			BeirutaHealWindow.OgcdJustUsed(CelestialOppositionPvE))
		{
			potency += OppositionHotCountedPotency;
		}

		if (IsHealableInRange(member, HeliosRange) &&
			!member.HasStatus(false, HeliosHotStatus) &&
			BeirutaHealWindow.GcdJustUsed(ActionID.AspectedHeliosPvE, ActionID.HeliosConjunctionPvE))
		{
			potency += ConjunctionHotCountedPotency;
		}

		return potency;
	}

	private float EffectiveHpRatio(IBattleChara member) =>
		BeirutaHealModel.EffectiveHpRatio(member, WardMode, LandingHotPotency(member));

	private bool CountsAsInjured(IBattleChara member, float threshold) =>
		member.GetHealthRatio() < BeirutaHealModel.AlwaysInjuredHp ||
		EffectiveHpRatio(member) < threshold;

	private static bool IsHealableInRange(IBattleChara? member, float range) =>
		member != null &&
		!member.IsDead &&
		member.DistanceToPlayer() <= range;

	private int CountInjured(float range, float threshold)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (IsHealableInRange(member, range) && !IsHealBlocked(member) && CountsAsInjured(member, threshold))
			{
				count++;
			}
		}
		return count;
	}

	private static bool AnyCritical(float range)
	{
		foreach (var member in PartyMembers)
		{
			if (IsHealableInRange(member, range) && !IsHealBlocked(member) && member.GetHealthRatio() < CriticalHp)
			{
				return true;
			}
		}
		return false;
	}

	private bool AoeNeeded(float range, float threshold) => CountInjured(range, threshold) >= AoeInjuredCount;

	private int CountInjuredWithHoroscope(float threshold)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (member != null &&
				!member.IsDead &&
				!IsHealBlocked(member) &&
				member.HasStatus(true, StatusID.HoroscopeHelios) &&
				CountsAsInjured(member, threshold))
			{
				count++;
			}
		}
		return count;
	}

	private static bool PartyBigHealSoon
	{
		get
		{
			foreach (var member in PartyMembers)
			{
				if (member == null || member.IsDead)
				{
					continue;
				}

				if (member.HasStatus(false, StatusID.GiantDominance) ||
					member.HasStatus(false, MacrocosmosStatus) ||
					(member.HasStatus(false, StatusID.EarthlyDominance) &&
					 member.StatusTime(false, StatusID.EarthlyDominance) < EarthlyDominanceGateSeconds))
				{
					return true;
				}
			}
			return false;
		}
	}

	private static bool HoldAoe => AccretionGroupHealBlocked || (PartyBigHealSoon && !AnyCritical(PartyHealRange));

	private static IBattleChara? FindLowest(float range, Func<IBattleChara, bool> filter)
	{
		IBattleChara? best = null;
		foreach (var member in PartyMembers)
		{
			if (!IsHealableInRange(member, range) || IsHealBlocked(member) || !filter(member))
			{
				continue;
			}

			if (best == null || member.GetHealthRatio() < best.GetHealthRatio())
			{
				best = member;
			}
		}
		return best;
	}

	private static IBattleChara? EnemyAggroTank
	{
		get
		{
			var aggroId = CurrentTarget?.TargetObject?.GameObjectId;
			if (aggroId == null)
			{
				return null;
			}

			foreach (var member in PartyMembers)
			{
				if (member.GameObjectId == aggroId && IsTankMember(member))
				{
					return member;
				}
			}
			return null;
		}
	}

	private static IBattleChara? MainTank => EnemyAggroTank ?? FindLowest(SingleHealRange, static m => IsTankMember(m));

	private static float HealBoost => HasNeutralSect ? NeutralSectBoost : 1f;

	private float AoeThreshold(float potency, float customWardOff, float wardOffCap = 1f) =>
		BeirutaHealModel.Threshold(potency, HealBoost, WardMode, UseCustomHPThreshold, customWardOff, PartyMembers, wardOffCap);

	private float DirectPartThreshold(float directPotency, float countedPotency, float customWardOff) =>
		WardMode
			? BeirutaHealModel.RoundToFive(BeirutaHealModel.FullUseHp(directPotency, HealBoost))
			: AoeThreshold(countedPotency, customWardOff);

	private float LadyHp => AoeThreshold(LadyPotency, LadyOfHeals);

	private float StellarHp => AoeThreshold(StellarDetonationPotency, StellarDetonationHeal);

	private float HoroscopeHp => AoeThreshold(HoroscopeHeliosPotency, HoroscopeHeal);

	private float OppositionHp => DirectPartThreshold(OppositionDirectPotency, OppositionCountedPotency, CelestialOppositionHeal);

	private float HeliosHp => AoeThreshold(HeliosPotency, HeliosHeal, HeliosWardOffCap);

	private float ConjunctionHp => DirectPartThreshold(ConjunctionDirectPotency, ConjunctionCountedPotency, HeliosConjunctionHeal);

	private static bool AoeHealGcdPending =>
		BeirutaHealWindow.GcdJustUsed(ActionID.HeliosPvE, ActionID.AspectedHeliosPvE, ActionID.HeliosConjunctionPvE);

	private static bool PartyHasHeliosHot
	{
		get
		{
			if (BeirutaHealWindow.GcdJustUsed(ActionID.AspectedHeliosPvE, ActionID.HeliosConjunctionPvE))
			{
				return true;
			}

			var total = 0;
			var withHot = 0;
			foreach (var member in PartyMembers)
			{
				if (!IsHealableInRange(member, HeliosRange))
				{
					continue;
				}

				total++;
				if (member.HasStatus(true, HeliosHotStatus))
				{
					withHot++;
				}
			}
			return total > 0 && withHot * 2 >= total;
		}
	}

	private static int CountWithoutNeutralSectShield(float range)
	{
		var count = 0;
		foreach (var member in PartyMembers)
		{
			if (IsHealableInRange(member, range) && !member.HasStatus(true, StatusID.NeutralSect_1921))
			{
				count++;
			}
		}
		return count;
	}

	private bool EssentialDignityReady =>
		EssentialDignityPvE.EnoughLevel &&
		EssentialDignityPvE.IsEnabled &&
		EssentialDignityPvE.Cooldown.HasOneCharge;

	private float EssentialDignityThreshold => EssentialDignityPvE.Cooldown.CurrentCharges switch
	{
		>= 3 => EssentialDignityThirdValue,
		2 => EssentialDignitySecondValue,
		_ => EssentialDignityLastValue,
	};

	private bool AoeOgcdHealReady =>
		(HasLady && LadyOfCrownsPvE.IsEnabled) ||
		HasGiantDominance ||
		HasHoroscopeHelios ||
		(CelestialOppositionPvE.EnoughLevel && CelestialOppositionPvE.IsEnabled && !CelestialOppositionPvE.Cooldown.IsCoolingDown);

	private static bool HasHoroscopeHelios => StatusHelper.PlayerHasStatus(true, StatusID.HoroscopeHelios);

	private static bool HasHoroscope => StatusHelper.PlayerHasStatus(true, StatusID.Horoscope);

	#endregion

	#region Macrocosmos Signal

	private const float SignalFreshSeconds = 5f;
	private const float SynastryDuration = 20f;
	private const float IntersectionDuration = 30f;
	private const float MacrocosmosReserveSeconds = 30f;

	private static bool PlayerStatusFresh(StatusID status, float duration) =>
		StatusHelper.PlayerHasStatus(true, status) &&
		StatusHelper.PlayerStatusTime(true, status) > duration - SignalFreshSeconds;

	private bool MacrocosmosSignalActive =>
		(SignalSurecast && StatusHelper.PlayerHasStatus(true, StatusID.Surecast)) ||
		(SignalSprint && StatusHelper.PlayerHasStatus(true, StatusID.Sprint)) ||
		(SignalSynastry && PlayerStatusFresh(StatusID.Synastry, SynastryDuration)) ||
		(SignalIntersection && PlayerStatusFresh(StatusID.Intersection, IntersectionDuration));

	private bool MacrocosmosReadySoon =>
		MacrocosmosPvE.EnoughLevel &&
		MacrocosmosPvE.Cooldown.WillHaveOneCharge(MacrocosmosReserveSeconds);

	private bool TryMacrocosmosSignal(out IAction? act)
	{
		act = null;
		return MacrocosmosSignalActive &&
			!HasMacrocosmos &&
			BeirutaAccretion.MicrocosmosSafe(PartyMembers) &&
			MacrocosmosPvE.CanUse(out act, skipAoeCheck: true);
	}

	#endregion

	#region Single-Target Helpers

	private static bool HasCelestialIntersection(IBattleChara target) => target.HasStatus(true, StatusID.Intersection);

	private static bool HasAspectedBeneficFromSelf(IBattleChara target) => target.HasStatus(true, StatusID.AspectedBenefic);

	private bool IntersectionAllowedOn(IBattleChara target) =>
		!HasCelestialIntersection(target) &&
		!(SignalIntersection && IsSelf(target)) &&
		!(SignalIntersection && MacrocosmosReadySoon && CelestialIntersectionPvE.Cooldown.CurrentCharges < 2);

	private bool TryIntersectionOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return IntersectionAllowedOn(target) &&
			BeirutaAccretion.TryUseOn(CelestialIntersectionPvE, target, out act, usedUp: true);
	}

	private bool TryExaltationOn(IBattleChara target, out IAction? act)
	{
		act = null;
		return !target.HasStatus(true, StatusID.Exaltation) &&
			BeirutaAccretion.TryUseOn(ExaltationPvE, target, out act);
	}

	private bool TryTankMitigation(IBattleChara? tank, out IAction? act)
	{
		act = null;

		if (tank == null || !IsTankMember(tank) || IsHealBlocked(tank) || !IsHealableInRange(tank, SingleHealRange))
		{
			return false;
		}

		return TryIntersectionOn(tank, out act) || TryExaltationOn(tank, out act);
	}

	private bool TryRaidwideIntersection(out IAction? act)
	{
		act = null;
		var target = FindLowest(SingleHealRange, m => !IsTankMember(m) && IntersectionAllowedOn(m));
		return target != null && TryIntersectionOn(target, out act);
	}

	private bool TryEssentialDignityOn(IBattleChara target, out IAction? act) =>
		BeirutaAccretion.TryUseOn(EssentialDignityPvE, target, out act, usedUp: true);

	#endregion

	#region Countdown Logic

	private const long CountdownResponseDelayMs = 1_000;
	private long _countdownStartedAtMs;
	private long _countdownLastSeenMs;
	private float _countdownLastRemain;

	private bool CountdownDelayPassed(float remainTime)
	{
		var now = Environment.TickCount64;

		if (_countdownStartedAtMs == 0 ||
			now - _countdownLastSeenMs > 500 ||
			remainTime > _countdownLastRemain + 0.5f)
		{
			_countdownStartedAtMs = now;
		}

		_countdownLastSeenMs = now;
		_countdownLastRemain = remainTime;

		return now - _countdownStartedAtMs >= CountdownResponseDelayMs;
	}

	private const float OpenerBuffCountdownSeconds = 0.35f;
	private const long OpenerWeaveHoldMs = 3_500;

	private long _openerBuffSentMs;

	private bool OpenerWeaveHold =>
		_openerBuffSentMs != 0 &&
		Environment.TickCount64 - _openerBuffSentMs < OpenerWeaveHoldMs &&
		!IsLastGCD(true, CombustIiiPvE, CombustIiPvE, CombustPvE);

	private bool TryOpenerDivination(float remainTime, out IAction? act)
	{
		act = null;

		if (DivinationOpener != DivinationOpenerTiming.ZeroSeconds ||
			!IsBurst ||
			remainTime >= OpenerBuffCountdownSeconds ||
			ECommons.GameHelpers.Player.Object is not { IsCasting: false } ||
			!DivinationPvE.CanUse(out act, skipTTKCheck: true))
		{
			return false;
		}

		_openerBuffSentMs = Environment.TickCount64;
		_divinationUsedAtMs = _openerBuffSentMs;
		return true;
	}

	protected override IAction? CountDownAction(float remainTime)
	{
		if (WardMode)
		{
			BeirutaWard.SetWardMode(this, false);
		}

		if (!CountdownDelayPassed(remainTime))
		{
			return null;
		}

		if (remainTime < MaleficPvE.Info.CastTime + CountDownAhead && MaleficPvE.CanUse(out var act))
		{
			return act;
		}

		if (TryOpenerDivination(remainTime, out act))
		{
			return act;
		}

		if (remainTime < 3f &&
			UsePotion &&
			PotionTiming == PotTiming.Opener &&
			UseBurstMedicine(out act))
		{
			return act;
		}

		if (EarthlyStarMode == EarthlyStarUsage.OnCooldown &&
			remainTime < UseEarthlyStarTime &&
			EarthlyStarPvE.CanUse(out act, skipTTKCheck: true))
		{
			return act;
		}

		if (TryCountdownShield(remainTime, out act))
		{
			return act;
		}

		return remainTime < 30 && !HoldingDamageCard && AstralDrawPvE.CanUse(out act)
			? act
			: base.CountDownAction(remainTime);
	}

	private bool TryCountdownShield(float remainTime, out IAction? act)
	{
		act = null;

		switch (CountdownShield)
		{
			case CountdownShieldStrategy.TankAspectedBeneficIntersection:
				if (BeirutaWard.StanceTank(PartyMembers, SingleHealRange) is not { } tank)
				{
					return false;
				}

				if (remainTime is < 5.5f and > 4.5f &&
					!HasAspectedBeneficFromSelf(tank) &&
					BeirutaAccretion.TryUseOn(AspectedBeneficPvE, tank, out act))
				{
					return true;
				}

				return remainTime is < 5f and > 2f && TryIntersectionOn(tank, out act);

			case CountdownShieldStrategy.NeutralSectHeliosConjunction:
				if (remainTime is < 9f and > 7f && !HasNeutralSect && NeutralSectPvE.CanUse(out act))
				{
					return true;
				}

				return remainTime is < 7f and > 4.5f && HasNeutralSect && !NeutralSectHeliosDone && TryHeliosGCD(true, out act);

			default:
				return false;
		}
	}

	#endregion

	#region Healing Abilities

	private bool TryHealAbility(out IAction? act)
	{
		act = null;

		if (!CanRunHealEngine)
		{
			return false;
		}

		var undeadTank = FindLowest(SingleHealRange, static m => IsTankMember(m) && IsJustOffUndead(m));
		if (undeadTank != null && TryEssentialDignityOn(undeadTank, out act))
		{
			return true;
		}

		var critical = FindLowest(SingleHealRange, static m => m.GetHealthRatio() < CriticalHp);
		if (critical != null && TryEssentialDignityOn(critical, out act))
		{
			return true;
		}

		if (_raidwide.Coming && TryRaidwideIntersection(out act))
		{
			return true;
		}

		foreach (var member in PartyMembers)
		{
			if (IsFreshMitigatingTank(member) && TryTankMitigation(member, out act))
			{
				return true;
			}
		}

		if (CelestialIntersectionPvE.EnoughLevel &&
			CelestialIntersectionPvE.Cooldown.MaxCharges >= 2 &&
			CelestialIntersectionPvE.Cooldown.CurrentCharges >= CelestialIntersectionPvE.Cooldown.MaxCharges &&
			MainTank is { } capTank &&
			capTank.GetHealthRatio() < CapTankHp &&
			TryIntersectionOn(capTank, out act))
		{
			return true;
		}

		var edThreshold = EssentialDignityThreshold;
		var edTank = FindLowest(SingleHealRange, m => IsTankMember(m) && EffectiveHpRatio(m) < edThreshold);
		if (edTank != null && TryEssentialDignityOn(edTank, out act))
		{
			return true;
		}

		if (!AccretionGroupHealBlocked)
		{
			if (AoeNeeded(PartyHealRange, LadyHp) &&
				LadyOfCrownsPvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}

			if (HasGiantDominance &&
				AoeNeeded(PartyHealRange, StellarHp) &&
				StellarDetonationPvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (!HoldAoe)
		{
			if (HasHoroscopeHelios &&
				CountInjuredWithHoroscope(HoroscopeHp) >= AoeInjuredCount &&
				HoroscopePvE_16558.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}

			if (AoeNeeded(PartyHealRange, OppositionHp) &&
				CelestialOppositionPvE.CanUse(out act, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (!AoeNeeded(PartyHealRange, LadyHp))
		{
			var topUp = FindLowest(SingleHealRange, m => EffectiveHpRatio(m) < edThreshold);
			if (topUp != null && TryEssentialDignityOn(topUp, out act))
			{
				return true;
			}
		}

		if (NonTankTopUp is { } nonTank && TryEssentialDignityOn(nonTank, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool TrySynastryPairing(IAction nextGCD, out IAction? act)
	{
		act = null;

		if (SignalSynastry ||
			HasSynastry ||
			nextGCD == null ||
			!nextGCD.IsTheSameTo(false, BeneficIiPvE))
		{
			return false;
		}

		var healTargetId = BeneficIiPvE.Target.Target?.GameObjectId;
		var threshold = SynastryHealValue;
		var bond =
			FindLowest(SingleHealRange, m => IsTankMember(m) && m.GameObjectId != healTargetId && m.GetHealthRatio() < threshold) ??
			FindLowest(SingleHealRange, m => m.GameObjectId != healTargetId && m.GetHealthRatio() < threshold);

		return bond != null && BeirutaAccretion.TryUseOn(SynastryPvE, bond, out act);
	}

	#endregion

	#region Utility Cards

	private const float UtilityCardTankHp = 0.7f;
	private const uint UtilityCardDumpGcds = 2;

	private bool UtilityCardsHeld => IsOpen || HasDivination || OpenerWeaveHold;

	private static bool HasCardStatus(IBattleChara member) =>
		member.HasStatus(false, StatusID.TheBole_3890, StatusID.TheSpire_3892, StatusID.TheEwer_3891, StatusID.TheArrow_3888);

	private bool TryArrowBeforeHelios(IAction nextGCD, out IAction? act)
	{
		act = null;

		if (!HasArrow ||
			IsOpen ||
			OpenerWeaveHold ||
			nextGCD == null ||
			!nextGCD.IsTheSameTo(false, HeliosConjunctionPvE, AspectedHeliosPvE, HeliosPvE) ||
			Player is not { } self ||
			self.HasStatus(true, StatusID.TheArrow_3888))
		{
			return false;
		}

		return BeirutaAccretion.TryUseOn(TheArrowPvE, self, out act);
	}

	private bool TryUtilityCards(out IAction? act)
	{
		act = null;

		if (!InCombat || UtilityCardsHeld)
		{
			return false;
		}

		if (HasBole || HasSpire)
		{
			var card = HasBole ? TheBolePvE : TheSpirePvE;

			var target =
				FindLowest(SingleHealRange, static m => IsTankMember(m) && !HasCardStatus(m) && (HasFreshBigMitigation(m) || m.GetHealthRatio() < UtilityCardTankHp)) ??
				(_raidwide.Coming ? FindLowest(SingleHealRange, static m => !IsTankMember(m) && !HasCardStatus(m)) : null);

			if (target == null && DrawReadySoon(UtilityCardDumpGcds) && MainTank is { } tank && !HasCardStatus(tank))
			{
				target = tank;
			}

			if (target != null && BeirutaAccretion.TryUseOn(card, target, out act))
			{
				return true;
			}
		}

		if (HasEwer)
		{
			var target =
				FindLowest(SingleHealRange, static m => IsTankMember(m) && m.GetHealthRatio() < 1f && !HasCardStatus(m)) ??
				FindLowest(SingleHealRange, static m => m.GetHealthRatio() < 1f && !HasCardStatus(m));

			if (target != null && BeirutaAccretion.TryUseOn(TheEwerPvE, target, out act))
			{
				return true;
			}
		}

		act = null;
		return false;
	}

	#endregion

	#region oGCD Logic

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		UpdateMovementCooldownTracking();

		if (TryPriorityWeaves(out act) ||
			TryAccretionAbility(out act) ||
			TryHealAbility(out act) ||
			TrySynastryPairing(nextGCD, out act) ||
			TryArrowBeforeHelios(nextGCD, out act))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.CelestialIntersectionPvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		if (TryRaidwideIntersection(out act))
		{
			return true;
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.CelestialIntersectionPvE, ActionID.ExaltationPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		var tank = FindLowest(SingleHealRange, static m => IsFreshMitigatingTank(m)) ?? MainTank;
		if (TryTankMitigation(tank, out act))
		{
			return true;
		}

		return base.DefenseSingleAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		if (StatusHelper.PlayerHasStatus(true, StatusID.Suntouched) &&
			StatusHelper.PlayerWillStatusEndGCD(3, 0, true, StatusID.Suntouched) &&
			SunSignPvE.CanUse(out act, skipAoeCheck: true, skipTTKCheck: true))
		{
			return true;
		}

		if (TryUtilityCards(out act))
		{
			return true;
		}

		return base.GeneralAbility(nextGCD, out act);
	}

	[RotationDesc(
		ActionID.DivinationPvE,
		ActionID.LightspeedPvE,
		ActionID.TheBalancePvE,
		ActionID.TheSpearPvE,
		ActionID.LordOfCrownsPvE,
		ActionID.AstralDrawPvE,
		ActionID.UmbralDrawPvE,
		ActionID.OraclePvE,
		ActionID.EarthlyStarPvE,
		ActionID.SwiftcastPvE)]
	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		UpdateMovementCooldownTracking();

		if (TryPriorityWeaves(out act))
		{
			return true;
		}

		if (InCombat)
		{
			if (ShouldUseSwiftcastForMovement(nextGCD) && SwiftcastPvE.CanUse(out act))
			{
				return true;
			}

			if (ShouldUseLightspeedForMovement(nextGCD) &&
				LightspeedPvE.CanUse(out act, usedUp: LightspeedPvE.Cooldown.CurrentCharges > 1))
			{
				return true;
			}

			if (EarthlyStarMode == EarthlyStarUsage.OnCooldown &&
				IsBurst &&
				!OpenerWeaveHold &&
				!HasGiantDominance &&
				!HasEarthlyDominance &&
				EarthlyStarPvE.CanUse(out act))
			{
				return true;
			}

			if (LightspeedCapping && LightspeedPvE.CanUse(out act))
			{
				return true;
			}
		}

		return base.AttackAbility(nextGCD, out act);
	}

	#endregion

	#region Accretion Top Up

	private static bool MicrocosmosFillsAccretion(IBattleChara target) =>
		!target.IsJobCategory(JobRole.Healer) &&
		HasMacrocosmos &&
		target.HasStatus(false, MacrocosmosStatus) &&
		BeirutaAccretion.MicrocosmosSafe(PartyMembers);

	private bool TryAccretionAbility(out IAction? act)
	{
		act = null;

		if (!AccretionTopUp)
		{
			return false;
		}

		if (BeirutaAccretion.FindTarget(PartyMembers, requireSafeParty: false) is { } last &&
			MicrocosmosFillsAccretion(last))
		{
			return MicrocosmosPvE.CanUse(out act, skipAoeCheck: true);
		}

		var target = BeirutaAccretion.FindTarget(PartyMembers);
		return target != null && BeirutaAccretion.TryUseOn(EssentialDignityPvE, target, out act, usedUp: true);
	}

	protected override bool EmergencyGCD(IAction? nextGCD, out IAction? act)
	{
		var target = AccretionTopUp ? BeirutaAccretion.FindTarget(PartyMembers) : null;
		if (target != null &&
			!MicrocosmosFillsAccretion(target) &&
			!HasSwift &&
			!EssentialDignityReady &&
			BeirutaAccretion.TryUseOn(BeneficIiPvE, target, out act))
		{
			return true;
		}

		return base.EmergencyGCD(nextGCD, out act);
	}

	#endregion

	#region Neutral Sect / Horoscope

	private void TrackNeutralSect()
	{
		var up = HasNeutralSect;
		if (up && !_neutralSectWasUp)
		{
			_neutralSectStartedAt = DateTime.Now;
		}

		_neutralSectWasUp = up;
	}

	private bool NeutralSectHeliosDone =>
		BeirutaHealWindow.UsedSince(_neutralSectStartedAt, ActionID.HeliosConjunctionPvE, ActionID.AspectedHeliosPvE);

	private bool TryHeliosGCD(bool withHot, out IAction? act)
	{
		if (withHot)
		{
			if (HeliosConjunctionPvE.EnoughLevel &&
				HeliosConjunctionPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true))
			{
				return true;
			}

			if (!HeliosConjunctionPvE.EnoughLevel &&
				AspectedHeliosPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true))
			{
				return true;
			}
		}

		if (HeliosPvE.CanUse(out act, skipAoeCheck: true))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool TryHoroscopeUpgradeGCD(out IAction? act)
	{
		act = null;
		return AutoUpgradeHoroscopeValue &&
			HasHoroscope &&
			!HasHoroscopeHelios &&
			!AoeHealGcdPending &&
			TryHeliosGCD(!PartyHasHeliosHot, out act);
	}

	private bool TryNeutralSectHeliosGCD(out IAction? act)
	{
		act = null;
		return HasNeutralSect &&
			!NeutralSectHeliosDone &&
			!AccretionGroupHealBlocked &&
			CountWithoutNeutralSectShield(HeliosRange) >= BeirutaWard.UnshieldedPartyCount &&
			TryHeliosGCD(true, out act);
	}

	#endregion

	#region GCD Logic

	private IBattleChara? NonTankTopUp =>
		BeirutaHealModel.NonTankTopUpTarget(PartyMembers, SingleHealRange, EffectiveHpRatio, static m => IsHealBlocked(m), AoeInjuredCount);
	private bool TryHealGCD(out IAction? act)
	{
		act = null;

		if (!CanRunHealEngine)
		{
			return false;
		}

		if (!EssentialDignityReady &&
			FindLowest(SingleHealRange, static m => m.GetHealthRatio() < CriticalHp) is { } critical &&
			BeirutaAccretion.TryUseOn(BeneficIiPvE.EnoughLevel ? BeneficIiPvE : BeneficPvE, critical, out act))
		{
			return true;
		}

		if (HasNeutralSect &&
			MainTank is { } tank &&
			tank.GetHealthRatio() < NeutralSectTankHp &&
			!tank.HasStatus(true, StatusID.NeutralSect_1921) &&
			BeirutaAccretion.TryUseOn(AspectedBeneficPvE, tank, out act))
		{
			return true;
		}

		if (HeliosGcdWanted && TryHeliosGCD(!PartyHasHeliosHot, out act))
		{
			return true;
		}

		if (NonTankTopUp is { } topUp &&
			!EssentialDignityReady &&
			BeirutaAccretion.TryUseOn(BeneficIiPvE.EnoughLevel ? BeneficIiPvE : BeneficPvE, topUp, out act))
		{
			return true;
		}

		act = null;
		return false;
	}

	private bool HeliosGcdWanted
	{
		get
		{
			if (!CanRunHealEngine ||
				(WardMode ? AccretionGroupHealBlocked : HoldAoe) ||
				AoeHealGcdPending)
			{
				return false;
			}

			var aoeNeeded = AoeNeeded(HeliosRange, PartyHasHeliosHot ? HeliosHp : ConjunctionHp);
			var aoeEmergency =
				AoeNeeded(HeliosRange, 0f) ||
				(AnyCritical(HeliosRange) && aoeNeeded);

			return aoeEmergency || (aoeNeeded && (WardMode || !AoeOgcdHealReady));
		}
	}


	[RotationDesc(ActionID.HeliosConjunctionPvE)]
	protected override bool DefenseAreaGCD(out IAction? act)
	{
		if (!ShouldHoldRaiseSwift && TryNeutralSectHeliosGCD(out act))
		{
			return true;
		}

		return base.DefenseAreaGCD(out act);
	}

	[RotationDesc(ActionID.AscendPvE)]
	protected override bool RaiseGCD(out IAction? act)
	{
		if (AscendPvE.CanUse(out act))
		{
			return true;
		}

		return base.RaiseGCD(out act);
	}

	protected override bool GeneralGCD(out IAction? act)
	{
		UpdateMovementCooldownTracking();

		if (ShouldHoldRaiseSwift)
		{
			return base.GeneralGCD(out act);
		}

		if (TryMacrocosmosSignal(out act) ||
			TryNeutralSectHeliosGCD(out act) ||
			TryHoroscopeUpgradeGCD(out act) ||

			TryHealGCD(out act) ||
			CanUseCurrentGravity(out act))
		{
			return true;
		}

		var expectedHPToLive12Seconds = GetExpectedHpToLive12Seconds();

		if (HasDivination &&
			InCombat &&
			StatusHelper.PlayerStatusTime(true, StatusID.Divination) <= 5f &&
			CurrentTargetCombustMissingOrEnding(DivinationCombustRefreshSeconds) &&
			CurrentTargetHasEnoughHpForCombust(expectedHPToLive12Seconds) &&
			CanUseCurrentCombust(out act, skipStatusProvideCheck: true))
		{
			return true;
		}

		if (CurrentTargetHasEnoughHpForCombust(expectedHPToLive12Seconds) && CanUseCurrentCombust(out act))
		{
			return true;
		}

		if (TryMovementGCD(expectedHPToLive12Seconds, out act) || CanUseCurrentMalefic(out act))
		{
			return true;
		}

		return base.GeneralGCD(out act);
	}

	#endregion

	#region Overrides

	protected override void UpdateInfo()
	{
		BeirutaWard.Track(this, WardMode, ref _lastWardMode);
		_raidwide.Track(InCombat, PartyMembers);
		TrackNeutralSect();
		base.UpdateInfo();
	}

	public override void OnTerritoryChanged()
	{
		if (WardMode)
		{
			BeirutaWard.SetWardMode(this, false);
		}

		base.OnTerritoryChanged();
	}

	public override void DisplayRotationStatus()
	{
		ImGui.Text($"Divination due in: {DivinationDueIn:F1}s");
		ImGui.Text($"Hold last Lightspeed: {HoldLastLightspeedForDivination}");
		ImGui.Text($"Macrocosmos signal: {MacrocosmosSignalActive}");
		ImGui.Text($"Hold AoE: {HoldAoe}");
		ImGui.Text($"Heal boost: {HealBoost:P0}");
	}

	#endregion
}
