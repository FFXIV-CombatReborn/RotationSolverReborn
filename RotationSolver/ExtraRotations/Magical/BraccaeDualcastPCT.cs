namespace RotationSolver.ExtraRotations.Magical;

/// <summary>
/// Braccae's Dualcast Pictomancer Rotation (designed for Occult Crescent / Level 6 Phantom Red Mage Dualcast).
/// Based on the foundational rotation architecture and burst alignments of BeirutaPCT by Beiruta.
/// </summary>
[Rotation("Braccae Dualcast PCT", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/ExtraRotations/Magical/BraccaeDualcastPCT.cs")]
[ExtraRotation]
public sealed class BraccaeDualcastPCT : PictomancerRotation
{
	#region Config Options

	public enum HammerEarlyHoldSeconds
	{
		Sec0 = 0,
		Sec5 = 5,
		Sec10 = 10,
		Sec15 = 15,
	}

	public enum MotifPriorityOrder
	{
		CreatureWeaponLandscape,
		LandscapeCreatureWeapon,
		WeaponCreatureLandscape,
	}

	[RotationConfig(CombatType.PvE, Name =
		"Braccae Dualcast Pictomancer Rotation (Based on BeirutaPCT):\n" +
		"• Built for Phantom Job Dualcast (e.g. Occult Crescent with Level 6 Phantom Red Mage).\n" +
		"• Automatically dualcasts Motifs (Creature, Weapon, Landscape) when corresponding Muse charges are available.\n" +
		"• Intelligently gates Motif painting to avoid 0-DPS GCD lockouts when Muses have 0 charges.\n" +
		"• When Dualcast is absent, hardcasts fast 1.5s Aetherhue spells (Fire/Aero/Water) as primers.\n" +
		"• Uses Dualcast on 2.3s Subtractive Inks and burst Rainbow Drip when motifs are filled.\n" +
		"• Full burst alignment with Starry Muse, Retribution of the Madeen, Mog of the Ages, and Hammer Time."
	)]
	public bool Info_DoNotChange { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Dualcast Motif Priority Order")]
	public MotifPriorityOrder MotifPriority { get; set; } = MotifPriorityOrder.CreatureWeaponLandscape;

	[RotationConfig(CombatType.PvE, Name = "Use Dualcast on Rainbow Drip when motifs are already drawn")]
	public bool DualcastRainbowDrip { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Only spend Dualcast on raw Rainbow Drip inside Starry Muse (burst) or downtime")]
	public bool RawRainbowDripInBurstOnly { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use HolyInWhite or CometInBlack while moving")]
	public bool HolyCometMoving { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Paint overcap protection.")]
	public bool UseCapCometHoly { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Use the paint overcap protection (will still use comet while moving if the setup is on)")]
	public bool UseCapCometOnly { get; set; } = false;

	[Range(1, 5, ConfigUnitType.None, 1)]
	[RotationConfig(CombatType.PvE, Name = "Paint overcap protection limit. How many paint you need to be at for it to use Holy out of burst (Setting is ignored when you have Hyperphantasia)")]
	public int HolyCometMax { get; set; } = 5;

	[RotationConfig(CombatType.PvE, Name = "Prevent the use of defense abilities during bursts")]
	private bool BurstDefense { get; set; } = true;

	[RotationConfig(CombatType.PvE, Name = "Hold hammer chain for movement time (0/5/10/15s).")]
	public HammerEarlyHoldSeconds HammerEarlyHold { get; set; } = HammerEarlyHoldSeconds.Sec10;

	#endregion

	#region Helper Properties

	// Dualcast and Instant Buff detection
	private static bool HasDualcast =>
		StatusHelper.PlayerHasStatus(true, StatusID.Dualcast, StatusID.Dualcast_5438);

	private static bool HasInstantCast =>
		HasDualcast || HasSwift;

	private bool NextIsMovementSafeGcd(IAction nextGCD) =>
		nextGCD.IsTheSameTo(false, HolyInWhitePvE, CometInBlackPvE);

	// Calculate the remaining time until Starry Muse is ready.
	private float StarryIn =>
		HasStarryMuse ? 0f : StarryMusePvE.Cooldown.RecastTimeRemainOneCharge;

	// Determine whether Starry Muse will be ready within 3 seconds during burst.
	private bool StarryWithin3 =>
		!HasStarryMuse && StarryIn <= 3f && IsBurst;

	// Determine whether Starry Muse will be ready within 20 seconds but not within 3 seconds during burst.
	private bool StarryWithin20 =>
		!HasStarryMuse && StarryIn <= 20f && StarryIn > 3f && IsBurst;

	// Determine whether Paint should be reserved for Holy/Comet when Starry Muse is approaching.
	private bool ShouldReservePaintForHolyComet =>
		StarryWithin20 && Paint <= 2 && IsBurst;

	// Determine whether Holy/Comet spending is allowed under the Paint reserve rule.
	private bool HolyCometAllowedByPaintReserve =>
		!ShouldReservePaintForHolyComet && IsBurst;

	// Determine whether Striking Muse should be used to rescue movement when the next GCD is unsafe.
	private bool NeedsStrikingMovementRescue(IAction nextGCD) =>
		InCombat
		&& !NextIsMovementSafeGcd(nextGCD)
		&& !HasInstantCast
		&& !HasHammerTime
		&& MovingTime > 1.5f;

	private long _starPrismUsedAtMs = 0;

	// Determine whether actions should be blocked within the delayed window after Star Prism.
	private bool InPostPrismDelayedBlockWindow
	{
		get
		{
			if (_starPrismUsedAtMs == 0) return false;

			long elapsed = Environment.TickCount64 - _starPrismUsedAtMs;

			if (elapsed >= 3500)
			{
				_starPrismUsedAtMs = 0;
				return false;
			}

			return elapsed >= 1000;
		}
	}

	// Determine whether Striking Muse is likely to overcap soon.
	private bool StrikingOvercapSoon30 =>
		StrikingMusePvE.Cooldown.CurrentCharges == 1
		&& StrikingMusePvE.Cooldown.WillHaveOneCharge(30f);

	private long _holyUsedInOpenerAtMs = 0;
	private long _fangedUsedInStarryAtMs = 0;
	private long _prepStrikingUsedAtMs = 0;
	private long _starryUsedAtMs = 0;

	// Determine whether the Starry Muse burst status is currently active.
	private static bool InBurstStatus =>
		StatusHelper.PlayerHasStatus(true, StatusID.StarryMuse);

	// Determine whether Inspiration is currently active.
	private static bool HasInspiration =>
		StatusHelper.PlayerHasStatus(true, StatusID.Inspiration);

	#endregion

	#region Countdown logic

	// Select the appropriate action to use during the countdown before combat begins.
	protected override IAction? CountDownAction(float remainTime)
	{
		IAction act;

		if (remainTime < RainbowDripPvE.Info.CastTime + 0.4f + CountDownAhead)
		{
			if (RainbowDripPvE.CanUse(out act))
			{
				return act;
			}
		}

		if (remainTime < FireInRedPvE.Info.CastTime + CountDownAhead && DataCenter.PlayerSyncedLevel() < 92)
		{
			if (FireInRedPvE.CanUse(out act))
			{
				return act;
			}
		}

		if (remainTime is < 1f && StrikingMusePvE.CanUse(out act))
			return act;

		return base.CountDownAction(remainTime);
	}

	#endregion

	#region Emergency & Defense Abilities

	protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
	{
		act = null;

		// Apply the opener timing adjustment based on synced level.
		int adjustCombatTimeForOpener = DataCenter.PlayerSyncedLevel() < 92 ? 2 : 5;

		if (CombatTime < adjustCombatTimeForOpener
			&& StrikingMusePvE.CanUse(out act, skipCastingCheck: true))
		{
			return true;
		}

		if (IsBurst
			&& CombatTime > adjustCombatTimeForOpener
			&& StarryMusePvE.CanUse(out act, skipCastingCheck: true))
		{
			_starryUsedAtMs = Environment.TickCount64;
			return true;
		}

		// Use Swiftcast as a fallback instant tool if Dualcast is dropped
		if (InCombat && !HasInstantCast && SwiftcastPvE.CanUse(out act))
		{
			return true;
		}

		return base.EmergencyAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.SmudgePvE)]
	protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
	{
		if (SmudgePvE.CanUse(out act))
		{
			return true;
		}

		return base.MoveForwardAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.TemperaCoatPvE, ActionID.TemperaGrassaPvE, ActionID.AddlePvE)]
	protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
	{
		// Use mitigations when not prevented by burst defense rules.
		if ((!BurstDefense || (BurstDefense && !InBurstStatus)) && TemperaCoatPvE.CanUse(out act))
		{
			return true;
		}

		if ((!BurstDefense || (BurstDefense && !InBurstStatus)) && TemperaGrassaPvE.CanUse(out act))
		{
			return true;
		}

		if ((!BurstDefense || (BurstDefense && !InBurstStatus)) && AddlePvE.CanUse(out act))
		{
			return true;
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	[RotationDesc(ActionID.TemperaCoatPvE)]
	protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
	{
		// Use single-target mitigation when not prevented by burst defense rules.
		if ((!BurstDefense || (BurstDefense && !InBurstStatus)) && TemperaCoatPvE.CanUse(out act))
		{
			return true;
		}

		return base.DefenseAreaAbility(nextGCD, out act);
	}

	#endregion

	#region oGCD Attack & General Abilities

	protected override bool AttackAbility(IAction nextGCD, out IAction? act)
	{
		int adjustCombatTimeForOpener = DataCenter.PlayerSyncedLevel() < 92 ? 2 : 5;
		long nowMs = Environment.TickCount64;

		bool madeenAvailable = RetributionOfTheMadeenPvE.CanUse(out _);

		// Determine whether Mog usage is restricted by the Fanged Muse overwrite window.
		bool mogRestrictedWindow =
			_fangedUsedInStarryAtMs != 0
			&& (nowMs - _fangedUsedInStarryAtMs) < 160_000;

		bool mogReady = MogOfTheAgesPvE.CanUse(out _);
		bool mogAllowedNow = mogReady && (!mogRestrictedWindow || HasStarryMuse);

		// Calculate Starry Muse remaining time for burst alignment logic.
		float starryIn = HasStarryMuse ? 0f : StarryMusePvE.Cooldown.RecastTimeRemainOneCharge;

		bool starryWithin60 = !HasStarryMuse && starryIn <= 60f && IsBurst;
		bool starryWithin40 = !HasStarryMuse && starryIn <= 40f && IsBurst;
		bool starryReadySoon15 = !HasStarryMuse && starryIn <= 3f && IsBurst;
		bool starryReadySoon10 = !HasStarryMuse && starryIn <= 10f && IsBurst;

		bool starryJustUsed1s =
			_starryUsedAtMs != 0
			&& (nowMs - _starryUsedAtMs) < 1500;

		bool starryJustUsed5s =
			_starryUsedAtMs != 0
			&& (nowMs - _starryUsedAtMs) < 9000;

		// Determine whether the last Striking Muse charge should be preserved for an upcoming Starry window.
		float strikingNeededIn = MathF.Max(0f, starryIn - 5f);

		bool preserveStrikingForStarry =
			starryWithin60
			&& StrikingMusePvE.Cooldown.CurrentCharges == 1
			&& StrikingMusePvE.Cooldown.RecastTimeRemainOneCharge > strikingNeededIn;

		// Determine whether Striking Muse is approaching overcap.
		bool strikingOvercapSoon30 =
			StrikingMusePvE.Cooldown.CurrentCharges == 1
			&& StrikingMusePvE.Cooldown.RecastTimeRemainOneCharge <= 30f;

		// Determine whether Living Muse charges should be preserved for an upcoming burst.
		bool preserveLivingForBurst =
			CombatTime > 5f
			&& !HasStarryMuse
			&& starryWithin40
			&& LivingMusePvE.Cooldown.CurrentCharges <= 1;

		// Force Striking Muse inside Starry if HammerTime is missing.
		if (HasStarryMuse
			&& !HasHammerTime
			&& InCombat
			&& StrikingMusePvE.Cooldown.CurrentCharges > 0
			&& StrikingMusePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		// Maintain Subtractive Palette when Starry is not about to begin.
		if (!starryReadySoon15
			&& !starryJustUsed1s
			&& !HasMonochromeTones
			&& !HasSubtractivePalette
			&& SubtractivePalettePvE.CanUse(out act))
		{
			return true;
		}

		// Use Striking Muse as burst preparation shortly before Starry comes up.
		if (starryReadySoon10
			&& CombatTime > adjustCombatTimeForOpener
			&& IsBurst
			&& StrikingMusePvE.CanUse(out act, usedUp: true))
		{
			_prepStrikingUsedAtMs = nowMs;
			return true;
		}

		// Spend Striking Muse to prevent overcap when not preserving for Starry.
		if (strikingOvercapSoon30
			&& CombatTime > adjustCombatTimeForOpener
			&& !preserveStrikingForStarry
			&& IsBurst
			&& StrikingMusePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		// Spend Striking Muse for movement rescue when not preserving for Starry.
		if (NeedsStrikingMovementRescue(nextGCD)
			&& StrikingMusePvE.Cooldown.CurrentCharges > 0
			&& !preserveStrikingForStarry
			&& IsBurst
			&& StrikingMusePvE.CanUse(out act, usedUp: true))
		{
			return true;
		}

		// Use Madeen during Starry burst when allowed by timing gates.
		if (HasStarryMuse
			&& !starryJustUsed5s
			&& IsBurst
			&& !InPostPrismDelayedBlockWindow
			&& RetributionOfTheMadeenPvE.CanUse(out act))
		{
			return true;
		}

		// Use Mog during burst when allowed by overwrite rules and timing gates.
		if (!starryJustUsed5s
			&& mogAllowedNow
			&& IsBurst
			&& !HasHyperphantasia
			&& !InPostPrismDelayedBlockWindow
			&& MogOfTheAgesPvE.CanUse(out act))
		{
			return true;
		}

		// Use Living Muse actions when allowed by preservation and timing rules.
		if (!preserveLivingForBurst && !starryJustUsed5s && !InPostPrismDelayedBlockWindow && IsBurst)
		{
			if (!madeenAvailable
				&& !(InCombat && CombatTime < 2f && !HasHammerTime)
				&& PomMusePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}

			if (WingedMusePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}

			if (!mogReady && ClawedMusePvE.CanUse(out act, usedUp: true))
			{
				return true;
			}

			if (FangedMusePvE.CanUse(out act, usedUp: true))
			{
				if (HasStarryMuse)
					_fangedUsedInStarryAtMs = nowMs;

				return true;
			}
		}

		return base.AttackAbility(nextGCD, out act);
	}

	protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
	{
		if ((MergedStatus.HasFlag(AutoStatus.DefenseArea)
			|| StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.TemperaCoat))
			&& TemperaGrassaPvE.CanUse(out act))
		{
			return true;
		}

		// Use opener potion within the first 5 seconds when HammerTime is active.
		if (InCombat && CombatTime <= 5f && HasHammerTime && UseBurstMedicine(out act))
		{
			return true;
		}

		bool isMedicated = StatusHelper.PlayerHasStatus(true, StatusID.Medicated);

		float starryIn = HasStarryMuse ? 0f : StarryMusePvE.Cooldown.RecastTimeRemainOneCharge;
		bool starryReadySoon5 = !HasStarryMuse && starryIn <= 5f && IsBurst;

		// Use pre-potion shortly before Starry becomes available when not already Medicated.
		if (InCombat && !isMedicated && starryReadySoon5 && UseBurstMedicine(out act))
		{
			return true;
		}

		if (HasStarryMuse && InCombat && UseBurstMedicine(out act))
		{
			return true;
		}

		return base.GeneralAbility(nextGCD, out act);
	}

	#endregion

	#region GCD Logic

	protected override bool GeneralGCD(out IAction? act)
	{
		act = null;

		if (!InCombat)
			_holyUsedInOpenerAtMs = 0;

		bool isMedicated = StatusHelper.PlayerHasStatus(true, StatusID.Medicated);

		#region Out of Combat Preparation
		if (!InCombat)
		{
			// All motifs are instant out of combat; ensure all canvases are painted
			if (PomMotifPvE.CanUse(out act)) return true;
			if (WingMotifPvE.CanUse(out act)) return true;
			if (ClawMotifPvE.CanUse(out act)) return true;
			if (MawMotifPvE.CanUse(out act)) return true;

			if (!isMedicated && HammerMotifPvE.CanUse(out act)) return true;

			if (StarrySkyMotifPvE.CanUse(out act)
				&& !StatusHelper.PlayerHasStatus(true, StatusID.Hyperphantasia)
				&& !StatusHelper.PlayerHasStatus(true, StatusID.Medicated))
			{
				return true;
			}

			if (RainbowDripPvE.CanUse(out act)) return true;
		}
		#endregion

		#region Opener GCD Sequence
		bool blockEarlyFire = InCombat && CombatTime < 2f;
		bool blockEarlyHammerStamp = InCombat && CombatTime < 10f && !HasHyperphantasia;
		bool blockEarlyHolyAndLivingMotif = InCombat && CombatTime < 2f && !HasHammerTime;

		if (CombatTime < 5f)
		{
			if (!blockEarlyHolyAndLivingMotif && HolyInWhitePvE.CanUse(out act))
			{
				if (InCombat && CombatTime < 5f && _holyUsedInOpenerAtMs == 0)
					_holyUsedInOpenerAtMs = Environment.TickCount64;

				return true;
			}

			// Pre-combat or early opener motifs
			if (PomMotifPvE.CanUse(out act)) return true;
			if (WingMotifPvE.CanUse(out act)) return true;
			if (ClawMotifPvE.CanUse(out act)) return true;
			if (MawMotifPvE.CanUse(out act)) return true;
		}

		long nowMs = Environment.TickCount64;

		bool fireHardLockout =
			InCombat
			&& _holyUsedInOpenerAtMs != 0
			&& (nowMs - _holyUsedInOpenerAtMs) < 8000;

		if (fireHardLockout)
		{
			act = null;
			return false;
		}
		#endregion

		bool starryReadySoon2 = HasStarryMuse || StarryMusePvE.Cooldown.WillHaveOneCharge(0f);
		bool starryReadySoon10 = !HasStarryMuse && StarryMusePvE.Cooldown.WillHaveOneCharge(12f) && IsBurst;

		// Block hammer chain after preparation Striking Muse until Starry is ready.
		bool blockPrepHammerChain = _prepStrikingUsedAtMs != 0 && InCombat && !starryReadySoon2;

		int hyperStacks = StatusHelper.PlayerStatusStack(true, StatusID.Hyperphantasia);
		bool reserveHyperForPrism = HasStarstruck && hyperStacks == 1;

		bool starryWithin30 = !HasStarryMuse && StarryMusePvE.Cooldown.RecastTimeRemainOneCharge <= 30f;
		bool allowHammerDumpFor30sLead = starryWithin30 && !starryReadySoon10;

		// Clear the preparation marker when it is no longer relevant.
		if (!InCombat || starryReadySoon2)
		{
			_prepStrikingUsedAtMs = 0;
		}

		#region High Priority Burst Spells (Star Prism & Comet under Inspiration)

		// Spend Starstruck on Star Prism
		if (StarPrismPvE.CanUse(out act) && HasStarstruck)
		{
			_starPrismUsedAtMs = Environment.TickCount64;
			return true;
		}

		// Hammer Stamp in Starry if no Subtractive Palette
		if (!HasSubtractivePalette && HasStarryMuse && HammerStampPvE.CanUse(out act, skipComboCheck: true))
		{
			return true;
		}

		// Comet in Black under Inspiration
		if (HasStarryMuse && HasInspiration && !reserveHyperForPrism)
		{
			if (CometInBlackPvE.CanUse(out act, skipCastingCheck: true))
			{
				return true;
			}
		}

		// Subtractive Inks under Inspiration
		if (HasInspiration && HasSubtractivePalette && !reserveHyperForPrism && !StarryWithin3)
		{
			if (ThunderInMagentaPvE.CanUse(out act)) return true;
			if (StoneInYellowPvE.CanUse(out act)) return true;
			if (BlizzardInCyanPvE.CanUse(out act)) return true;
		}

		#endregion

		#region Hammer Combo Logic

		bool canCommitGcdNow = NextAbilityToNextGCD < 0.6f;
		float hammerRemain = HasHammerTime ? StatusHelper.PlayerStatusTime(true, StatusID.HammerTime) : 0f;

		int earlyHoldSec = (int)HammerEarlyHold;
		float earlyRemainThreshold = 30f - earlyHoldSec;

		bool hammerEarlyWindow = HasHammerTime && hammerRemain >= earlyRemainThreshold;
		bool hammerAfterWindow = HasHammerTime && hammerRemain > 0f && hammerRemain < earlyRemainThreshold;

		bool hammerAllowedByInspirationRule =
			HasStarryMuse
				? (IsMoving || !(HasInspiration && HasSubtractivePalette))
				: !(HasInspiration && HasSubtractivePalette);

		// Use hammer chain inside Starry Muse
		if (HasStarryMuse && InCombat && !HasSwift && !blockPrepHammerChain && hammerAllowedByInspirationRule)
		{
			if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true)) return true;
			if (HammerBrushPvE.CanUse(out act, skipComboCheck: true)) return true;
			if (!blockEarlyHammerStamp && HammerStampPvE.CanUse(out act, skipComboCheck: true)) return true;
		}

		// Use hammer chain for movement rescue during early HammerTime window
		if (!HasStarryMuse && hammerEarlyWindow && InCombat && MovingTime > 1.5f && canCommitGcdNow && !HasSwift && !blockPrepHammerChain && hammerAllowedByInspirationRule)
		{
			if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true)) return true;
			if (HammerBrushPvE.CanUse(out act, skipComboCheck: true)) return true;
			if (!blockEarlyHammerStamp && HammerStampPvE.CanUse(out act, skipComboCheck: true)) return true;
		}

		// Spend hammer chain outside Starry when permitted by dump rules
		if (!HasStarryMuse && InCombat && !HasSwift && !blockPrepHammerChain && hammerAllowedByInspirationRule
			&& (hammerAfterWindow || StrikingOvercapSoon30 || allowHammerDumpFor30sLead))
		{
			if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true)) return true;
			if (HammerBrushPvE.CanUse(out act, skipComboCheck: true)) return true;
			if (!blockEarlyHammerStamp && HammerStampPvE.CanUse(out act, skipComboCheck: true)) return true;
		}

		#endregion

		#region Rainbow Bright (Instant Rainbow Drip)
		if (RainbowDripPvE.CanUse(out act) && HasRainbowBright)
		{
			return true;
		}
		#endregion

		#region DUALCAST ACTIVE (HasInstantCast == true)
		// When Dualcast or Swiftcast is active, spend this instant cast on the highest value / longest cast spells.
		// Primary focus: Dualcasting Motifs whenever available before regular spells!
		if (HasInstantCast)
		{
			if (TryDualcastMotifs(out act, isMedicated))
			{
				return true;
			}

			// Dualcast Rainbow Drip (4.0s cast becomes instant, 1000 potency) if motifs are already drawn
			bool allowRawRainbowDrip = DualcastRainbowDrip && (!RawRainbowDripInBurstOnly || HasStarryMuse || !InCombat);
			if (allowRawRainbowDrip && RainbowDripPvE.CanUse(out act, skipCastingCheck: true))
			{
				return true;
			}

			// Dualcast Subtractive Inks (2.3s cast becomes instant, 860-940 potency)
			if (!StarryWithin3)
			{
				if (ThunderIiInMagentaPvE.CanUse(out act, skipCastingCheck: true)) return true;
				if (StoneIiInYellowPvE.CanUse(out act, skipCastingCheck: true)) return true;
				if (BlizzardIiInCyanPvE.CanUse(out act, skipCastingCheck: true)) return true;

				if (ThunderInMagentaPvE.CanUse(out act, skipCastingCheck: true)) return true;
				if (StoneInYellowPvE.CanUse(out act, skipCastingCheck: true)) return true;
				if (BlizzardInCyanPvE.CanUse(out act, skipCastingCheck: true)) return true;
			}

			// Dualcast Base Aetherhue Spells
			if (WaterIiInBluePvE.CanUse(out act, skipCastingCheck: true)) return true;
			if (AeroIiInGreenPvE.CanUse(out act, skipCastingCheck: true)) return true;
			if (FireIiInRedPvE.CanUse(out act, skipCastingCheck: true)) return true;

			if (WaterInBluePvE.CanUse(out act, skipCastingCheck: true)) return true;
			if (AeroInGreenPvE.CanUse(out act, skipCastingCheck: true)) return true;
			if (!blockEarlyFire && !fireHardLockout && FireInRedPvE.CanUse(out act, skipCastingCheck: true))
			{
				return true;
			}
		}
		#endregion

		#region Movement Rescue (Holy/Comet)
		if (HolyCometMoving && InCombat && MovingTime > 1.5f && canCommitGcdNow && !HasInstantCast && !HasHammerTime && HolyCometAllowedByPaintReserve)
		{
			if (CometInBlackPvE.CanUse(out act)) return true;
			if (HolyInWhitePvE.CanUse(out act)) return true;
		}
		#endregion

		#region Paint Overcap Protection
		if (Paint == HolyCometMax && !HasStarryMuse && (UseCapCometHoly || UseCapCometOnly))
		{
			if (CometInBlackPvE.CanUse(out act))
			{
				return true;
			}

			if (HolyInWhitePvE.CanUse(out act) && !UseCapCometOnly)
			{
				return true;
			}
		}
		#endregion

		#region Pre-Starry Paint Dumping (Final 3s before Starry)
		if (StarryWithin3 && InCombat && CombatTime > 5f)
		{
			if (CometInBlackPvE.CanUse(out act))
			{
				return true;
			}

			if (HolyInWhitePvE.CanUse(out act) && !UseCapCometOnly)
			{
				return true;
			}
		}
		#endregion

		#region DUALCAST PRIMERS (HasInstantCast == false)
		// When Dualcast is absent, hardcast fast 1.5s spells (Fire/Aero/Water) as primers to proc Dualcast for the next GCD!

		// Fast 1.5s AoE Aetherhues
		if (WaterIiInBluePvE.CanUse(out act)) return true;
		if (AeroIiInGreenPvE.CanUse(out act)) return true;
		if (FireIiInRedPvE.CanUse(out act)) return true;

		// Fast 1.5s Single-target Aetherhues
		if (WaterInBluePvE.CanUse(out act)) return true;
		if (AeroInGreenPvE.CanUse(out act)) return true;
		if (!blockEarlyFire && !fireHardLockout && FireInRedPvE.CanUse(out act))
		{
			return true;
		}

		// Subtractive Inks if Subtractive Palette is active and no basic primer available
		if (!StarryWithin3)
		{
			if (ThunderIiInMagentaPvE.CanUse(out act)) return true;
			if (StoneIiInYellowPvE.CanUse(out act)) return true;
			if (BlizzardIiInCyanPvE.CanUse(out act)) return true;

			if (ThunderInMagentaPvE.CanUse(out act)) return true;
			if (StoneInYellowPvE.CanUse(out act)) return true;
			if (BlizzardInCyanPvE.CanUse(out act)) return true;
		}
		#endregion

		#region Fallback Motif Hardcasts (when no other actions are available)
		if (PomMotifPvE.CanUse(out act)) return true;
		if (WingMotifPvE.CanUse(out act)) return true;
		if (ClawMotifPvE.CanUse(out act)) return true;
		if (MawMotifPvE.CanUse(out act)) return true;

		if (!isMedicated && HammerMotifPvE.CanUse(out act)) return true;
		if (StarrySkyMotifPvE.CanUse(out act)) return true;
		#endregion

		return base.GeneralGCD(out act);
	}

	#endregion

	#region Dualcast Motif Helper

	/// <summary>
	/// Attempts to dualcast motifs based on canvas availability and configured priority order.
	/// </summary>
	private bool TryDualcastMotifs(out IAction? act, bool isMedicated)
	{
		act = null;

		switch (MotifPriority)
		{
			case MotifPriorityOrder.CreatureWeaponLandscape:
				if (TryDualcastCreatureMotif(out act)) return true;
				if (TryDualcastWeaponMotif(out act, isMedicated)) return true;
				if (TryDualcastLandscapeMotif(out act)) return true;
				break;

			case MotifPriorityOrder.LandscapeCreatureWeapon:
				if (TryDualcastLandscapeMotif(out act)) return true;
				if (TryDualcastCreatureMotif(out act)) return true;
				if (TryDualcastWeaponMotif(out act, isMedicated)) return true;
				break;

			case MotifPriorityOrder.WeaponCreatureLandscape:
				if (TryDualcastWeaponMotif(out act, isMedicated)) return true;
				if (TryDualcastCreatureMotif(out act)) return true;
				if (TryDualcastLandscapeMotif(out act)) return true;
				break;
		}

		return false;
	}

	private bool TryDualcastCreatureMotif(out IAction? act)
	{
		act = null;
		if (CreatureMotifDrawn) return false;

		// Smart Charge Gating:
		// Only spend a 4.0s GCD to paint if Living Muse has a charge ready or almost ready (<= 4.0s),
		// OR we are preparing for upcoming Starry Muse burst (<= 30s) / early opener.
		bool allowCreaturePaint = !InCombat
			|| LivingMusePvE.Cooldown.HasOneCharge
			|| LivingMusePvE.Cooldown.RecastTimeRemainOneCharge <= 4.0f
			|| StarryMusePvE.Cooldown.RecastTimeRemainOneCharge <= 30f
			|| CombatTime < 5f;

		if (!allowCreaturePaint) return false;

		if (PomMotifPvE.CanUse(out act, skipCastingCheck: true)) return true;
		if (WingMotifPvE.CanUse(out act, skipCastingCheck: true)) return true;
		if (ClawMotifPvE.CanUse(out act, skipCastingCheck: true)) return true;
		if (MawMotifPvE.CanUse(out act, skipCastingCheck: true)) return true;

		return false;
	}

	private bool TryDualcastWeaponMotif(out IAction? act, bool isMedicated)
	{
		act = null;
		if (WeaponMotifDrawn || HasHammerTime || isMedicated) return false;

		// Smart Charge Gating:
		// Only spend a 4.0s GCD to paint hammer if Steel Muse has a charge ready or almost ready (<= 4.0s),
		// OR we are preparing for upcoming Starry Muse burst (<= 30s) / early opener.
		bool allowHammerPaint = !InCombat
			|| SteelMusePvE.Cooldown.HasOneCharge
			|| SteelMusePvE.Cooldown.RecastTimeRemainOneCharge <= 4.0f
			|| StarryMusePvE.Cooldown.RecastTimeRemainOneCharge <= 30f
			|| CombatTime < 5f;

		if (!allowHammerPaint) return false;

		if (HammerMotifPvE.CanUse(out act, skipCastingCheck: true)) return true;

		return false;
	}

	private bool TryDualcastLandscapeMotif(out IAction? act)
	{
		act = null;
		if (LandscapeMotifDrawn || HasStarryMuse || HasHyperphantasia) return false;

		// Smart Landscape Gating:
		// Only paint Starry Sky Motif if Scenic Muse is ready or coming off cooldown within 40s (or out of combat/opener).
		bool allowLandscapePaint = !InCombat
			|| ScenicMusePvE.Cooldown.HasOneCharge
			|| ScenicMusePvE.Cooldown.RecastTimeRemainOneCharge <= 40f
			|| CombatTime < 5f;

		if (!allowLandscapePaint) return false;

		if (StarrySkyMotifPvE.CanUse(out act, skipCastingCheck: true)) return true;

		return false;
	}

	#endregion
}
