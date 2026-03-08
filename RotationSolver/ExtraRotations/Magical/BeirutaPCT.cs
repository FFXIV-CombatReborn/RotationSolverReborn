namespace RotationSolver.ExtraRotations.Magical;

[Rotation("BeirutaPCT", CombatType.PvE, GameVersion = "7.45")]
[SourceCode(Path = "main/ExtraRotations/Magical/BeirutaPCT.cs")]
public sealed class BeirutaPCT : PictomancerRotation
{
    #region Config Options

    public enum HammerEarlyHoldSeconds
    {
        Sec0 = 0,
        Sec5 = 5,
        Sec10 = 10,
        Sec15 = 15,
    }

    [RotationConfig(CombatType.PvE, Name =
        "Please note that this rotation is optimised for combats that start with a countdown Rainbow Drip cast.\n" +
        "• Recommended gcd is 2.48/2.49/2.50 depends on your ping\n" +
        "• 2.48gcd will have higher chance of fitting rainbowdrip inside starry muse\n" +
        "• Ideally do not incert defence ability during first 5s of the fights or burst\n" +
        "• Enable Spell Intercept to manually use Rainbow Drip before the boss becomes untargetable.\n" +
        "• This rotation is designed to align Madeen within burst windows.\n" +
        "• Hyperphantasia is prioritised early in burst to allow earlier movement flexibility.\n" +
        "• Intercept Rainbow Drip automatically uses Swiftcast when Rainbow Drip is queued (May fail if pressed too late or casting sub inks/motifs).\n" +
        "• Manual Swiftcast input will be spent on Motif (creature -> weapon -> landscape)."
    )]
    public bool Info_DoNotChange { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use HolyInWhite or CometInBlack while moving")]
    public bool HolyCometMoving { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Paint overcap protection.")]
    public bool UseCapCometHoly { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use the paint overcap protection (will still use comet while moving if the setup is on)")]
    public bool UseCapCometOnly { get; set; } = false;

    [Range(1, 5, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Paint overcap protection limit. How many paint you need to be at for it to use Holy out of burst (Setting is ignored when you have Hyperphantasia)")]
    public int HolyCometMax { get; set; } = 5;

    [RotationConfig(CombatType.PvE, Name = "Use swiftcast on Intercepted Rainbow Drip before Boss Untargetable")]
    public bool RainbowDripSwift { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use swiftcast on Motif")]
    public bool MotifSwiftCastSwift { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Which Motif to use swiftcast on")]
    public CanvasFlags MotifSwiftCast { get; set; } = CanvasFlags.Claw;

    [RotationConfig(CombatType.PvE, Name = "Prevent the use of defense abilties during bursts")]
    private bool BurstDefense { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Hold hammer chain for movement time (0/5/10/15s).")]
    public HammerEarlyHoldSeconds HammerEarlyHold { get; set; } = HammerEarlyHoldSeconds.Sec10;

    #endregion

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
        && !HasSwift
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

    // Determine whether the Starry Muse burst status is currently active.
    private static bool InBurstStatus =>
        StatusHelper.PlayerHasStatus(true, StatusID.StarryMuse);

    // Determine whether Inspiration is currently active.
    private static bool HasInspiration =>
        StatusHelper.PlayerHasStatus(true, StatusID.Inspiration);

    private long _starryUsedAtMs = 0;

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

    #region Additional oGCD Logic

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

        if (RainbowDripSwift
            && !HasRainbowBright
            && nextGCD.IsTheSameTo(false, RainbowDripPvE)
            && SwiftcastPvE.CanUse(out act))
        {
            return true;
        }

        bool isMedicated = StatusHelper.PlayerHasStatus(true, StatusID.Medicated);

        // Apply Swiftcast to creature motifs during Medicated when a motif is queued.
        if (isMedicated)
        {
            bool isCreatureMotif =
                nextGCD.IsTheSameTo(false, PomMotifPvE)
                || nextGCD.IsTheSameTo(false, WingMotifPvE)
                || nextGCD.IsTheSameTo(false, ClawMotifPvE)
                || nextGCD.IsTheSameTo(false, MawMotifPvE);

            if (isCreatureMotif && SwiftcastPvE.CanUse(out act))
                return true;

            return base.EmergencyAbility(nextGCD, out act);
        }

        if (MotifSwiftCastSwift)
        {
            if ((MotifSwiftCast switch
                {
                    CanvasFlags.Pom => nextGCD.IsTheSameTo(false, PomMotifPvE),
                    CanvasFlags.Wing => nextGCD.IsTheSameTo(false, WingMotifPvE),
                    CanvasFlags.Claw => nextGCD.IsTheSameTo(false, ClawMotifPvE),
                    CanvasFlags.Maw => nextGCD.IsTheSameTo(false, MawMotifPvE),
                    CanvasFlags.Weapon => nextGCD.IsTheSameTo(false, HammerMotifPvE),
                    CanvasFlags.Landscape => nextGCD.IsTheSameTo(false, StarrySkyMotifPvE),
                    _ => false
                }) && SwiftcastPvE.CanUse(out act))
            {
                return true;
            }
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
        // Use mitigations when not prevented by burst defence rules.
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
        // Use single-target mitigation when not prevented by burst defence rules.
        if ((!BurstDefense || (BurstDefense && !InBurstStatus)) && TemperaCoatPvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }

    #endregion

    #region oGCD Logic

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        // Prepare burst- and opener-timing values used by the priority logic.
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
        bool starryWithin30 = !HasStarryMuse && starryIn <= 30f && IsBurst;

        // Determine whether hammer dumping is allowed during the 30-second lead-in window.
        bool allowHammerDumpFor30sLead = starryWithin30 && !starryReadySoon10;

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

        //Basic Muses - not real actions
        //if (ScenicMusePvE.CanUse(out act)) return true;
        //if (SteelMusePvE.CanUse(out act, usedUp: true)) return true;
        //if (LivingMusePvE.CanUse(out act, usedUp: true)) return true;
        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        // Prioritise Swiftcast for Rainbow Drip when it is intercepted and queued.
        if (RainbowDripSwift
            && !HasRainbowBright
            && nextGCD.IsTheSameTo(false, RainbowDripPvE)
            && SwiftcastPvE.CanUse(out act))
        {
            return true;
        }

        // Apply Swiftcast only to the configured Motif when enabled.
        if (MotifSwiftCastSwift)
        {
            bool shouldSwiftMotif = MotifSwiftCast switch
            {
                CanvasFlags.Pom => nextGCD.IsTheSameTo(false, PomMotifPvE),
                CanvasFlags.Wing => nextGCD.IsTheSameTo(false, WingMotifPvE),
                CanvasFlags.Claw => nextGCD.IsTheSameTo(false, ClawMotifPvE),
                CanvasFlags.Maw => nextGCD.IsTheSameTo(false, MawMotifPvE),
                CanvasFlags.Weapon => nextGCD.IsTheSameTo(false, HammerMotifPvE),
                CanvasFlags.Landscape => nextGCD.IsTheSameTo(false, StarrySkyMotifPvE),
                _ => false
            };

            if (shouldSwiftMotif && SwiftcastPvE.CanUse(out act))
                return true;
        }

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
        if (HasStarryMuse && HammerStampPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        if (!InCombat)
            _holyUsedInOpenerAtMs = 0;

        bool isMedicated = StatusHelper.PlayerHasStatus(true, StatusID.Medicated);

        bool blockEarlyFire = InCombat && CombatTime < 2f;
        bool blockEarlyHammerStamp = InCombat && CombatTime < 10f && !HasHyperphantasia;
        bool blockEarlyHolyAndLivingMotif = InCombat && CombatTime < 2f && !HasHammerTime;

        // Apply opener GCD priorities during the initial combat window.
        if (CombatTime < 5f)
        {
            if (!blockEarlyHolyAndLivingMotif && HolyInWhitePvE.CanUse(out act))
            {
                if (InCombat && CombatTime < 5f && _holyUsedInOpenerAtMs == 0)
                    _holyUsedInOpenerAtMs = Environment.TickCount64;

                return true;
            }

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

        bool starryReadySoon2 = HasStarryMuse || StarryMusePvE.Cooldown.WillHaveOneCharge(0f);
        bool starryReadySoon10 = !HasStarryMuse && StarryMusePvE.Cooldown.WillHaveOneCharge(12f) && IsBurst;

        // Block hammer chain after the preparation Striking Muse until Starry is almost ready.
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

        if (HasStarryMuse && HasInspiration && !reserveHyperForPrism)
        {
            if (CometInBlackPvE.CanUse(out act, skipCastingCheck: true))
            {
                return true;
            }
        }

        // Use Subtractive Inks under Inspiration when not too close to Starry.
        if (HasInspiration && HasSubtractivePalette && !reserveHyperForPrism && !StarryWithin3)
        {
            if (ThunderInMagentaPvE.CanUse(out act)) return true;
            if (StoneInYellowPvE.CanUse(out act)) return true;
            if (BlizzardInCyanPvE.CanUse(out act)) return true;
        }

        if (StarPrismPvE.CanUse(out act) && HasStarstruck)
        {
            _starPrismUsedAtMs = Environment.TickCount64;
            return true;
        }

        bool canCommitGcdNow = NextAbilityToNextGCD < 0.6f;

        float hammerRemain = HasHammerTime ? StatusHelper.PlayerStatusTime(true, StatusID.HammerTime) : 0f;

        int earlyHoldSec = (int)HammerEarlyHold;
        float earlyRemainThreshold = 30f - earlyHoldSec;

        bool hammerEarlyWindow = HasHammerTime && hammerRemain >= earlyRemainThreshold;
        bool hammerAfterWindow = HasHammerTime && hammerRemain > 0f && hammerRemain < earlyRemainThreshold;

        // Determine whether the hammer chain is permitted by the Inspiration restriction rule.
        bool hammerAllowedByInspirationRule =
            HasStarryMuse
                ? (IsMoving || !(HasInspiration && HasSubtractivePalette))
                : !(HasInspiration && HasSubtractivePalette);

        // Use the hammer chain during Starry when permitted.
        if (HasStarryMuse && InCombat && !HasSwift && !blockPrepHammerChain && hammerAllowedByInspirationRule)
        {
            if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (HammerBrushPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (!blockEarlyHammerStamp && HammerStampPvE.CanUse(out act, skipComboCheck: true)) return true;
        }

        // Use the hammer chain for movement rescue during the early HammerTime window.
        if (!HasStarryMuse && hammerEarlyWindow && InCombat && MovingTime > 1.5f && canCommitGcdNow && !HasSwift && !blockPrepHammerChain && hammerAllowedByInspirationRule)
        {
            if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (HammerBrushPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (!blockEarlyHammerStamp && HammerStampPvE.CanUse(out act, skipComboCheck: true)) return true;
        }

        // Spend the hammer chain outside Starry when permitted by timing and dump rules.
        if (!HasStarryMuse && InCombat && !HasSwift && !blockPrepHammerChain && hammerAllowedByInspirationRule
            && (hammerAfterWindow || StrikingOvercapSoon30 || allowHammerDumpFor30sLead))
        {
            if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (HammerBrushPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (!blockEarlyHammerStamp && HammerStampPvE.CanUse(out act, skipComboCheck: true)) return true;
        }

        if (RainbowDripPvE.CanUse(out act) && HasRainbowBright)
        {
            return true;
        }

        if (!InCombat)
        {
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

        // Cast motifs within the Scenic Muse preparation window.
        if (ScenicMusePvE.Cooldown.RecastTimeRemainOneCharge <= 30 && !HasStarryMuse && !HasHyperphantasia)
        {
            if (StarrySkyMotifPvE.CanUse(out act) && !HasHyperphantasia) return true;

            if (!isMedicated && !WeaponMotifDrawn && HammerMotifPvE.CanUse(out act)) return true;
        }

        // Cast creature motifs when Living Muse is available and not restricted by early combat rules.
        if (!blockEarlyHolyAndLivingMotif
            && (LivingMusePvE.Cooldown.HasOneCharge
                || LivingMusePvE.Cooldown.RecastTimeRemainOneCharge <= CreatureMotifPvE.Info.CastTime * 1.7)
            && !HasStarryMuse && !HasHyperphantasia)
        {
            if (PomMotifPvE.CanUse(out act)) return true;
            if (WingMotifPvE.CanUse(out act)) return true;
            if (ClawMotifPvE.CanUse(out act)) return true;
            if (MawMotifPvE.CanUse(out act)) return true;
        }

        // Cast weapon motif when Steel Muse is available and not restricted by Hyperphantasia.
        if ((SteelMusePvE.Cooldown.HasOneCharge || SteelMusePvE.Cooldown.RecastTimeRemainOneCharge <= WeaponMotifPvE.Info.CastTime)
            && !HasStarryMuse && !HasHyperphantasia)
        {
            if (!isMedicated && HammerMotifPvE.CanUse(out act))
            {
                return true;
            }
        }

        // Use Holy/Comet while moving only when the GCD commit timing window is available.
        {
            if (HolyCometMoving && InCombat && MovingTime > 1.5f && canCommitGcdNow && !HasSwift && !HasHammerTime && HolyCometAllowedByPaintReserve)
            {
                if (CometInBlackPvE.CanUse(out act)) return true;
                if (HolyInWhitePvE.CanUse(out act)) return true;
            }
        }

        // Spend Swiftcast on motif completion according to the selected motif target.
        if (HasSwift && (!LandscapeMotifDrawn || !CreatureMotifDrawn || !WeaponMotifDrawn))
        {
            if (PomMotifPvE.CanUse(out act, skipCastingCheck: MotifSwiftCast is CanvasFlags.Pom) && MotifSwiftCast is CanvasFlags.Pom)
            {
                return true;
            }

            if (WingMotifPvE.CanUse(out act, skipCastingCheck: MotifSwiftCast is CanvasFlags.Wing) && MotifSwiftCast is CanvasFlags.Wing)
            {
                return true;
            }

            if (ClawMotifPvE.CanUse(out act, skipCastingCheck: MotifSwiftCast is CanvasFlags.Claw) && MotifSwiftCast is CanvasFlags.Claw)
            {
                return true;
            }

            if (MawMotifPvE.CanUse(out act, skipCastingCheck: MotifSwiftCast is CanvasFlags.Maw) && MotifSwiftCast is CanvasFlags.Maw)
            {
                return true;
            }

            if (!isMedicated && HammerMotifPvE.CanUse(out act, skipCastingCheck: MotifSwiftCast is CanvasFlags.Weapon) && MotifSwiftCast is CanvasFlags.Weapon)
            {
                return true;
            }

            if (StarrySkyMotifPvE.CanUse(out act, skipCastingCheck: MotifSwiftCast is CanvasFlags.Landscape)
                && !HasHyperphantasia
                && MotifSwiftCast is CanvasFlags.Landscape)
            {
                return true;
            }
        }

        // Use Holy/Comet for Paint overcap protection when configured.
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

        // Use AOE Subtractive Inks when not too close to Starry.
        if (!StarryWithin3)
        {
            if (ThunderIiInMagentaPvE.CanUse(out act)) return true;
            if (StoneIiInYellowPvE.CanUse(out act)) return true;
            if (BlizzardIiInCyanPvE.CanUse(out act)) return true;
        }

        if (WaterIiInBluePvE.CanUse(out act)) return true;
        if (AeroIiInGreenPvE.CanUse(out act)) return true;
        if (FireIiInRedPvE.CanUse(out act)) return true;

        // Use single-target Subtractive Inks when not too close to Starry.
        if (!StarryWithin3)
        {
            if (ThunderInMagentaPvE.CanUse(out act)) return true;
            if (StoneInYellowPvE.CanUse(out act)) return true;
            if (BlizzardInCyanPvE.CanUse(out act)) return true;
        }

        if (WaterInBluePvE.CanUse(out act)) return true;
        if (AeroInGreenPvE.CanUse(out act)) return true;

        if (!blockEarlyFire && !fireHardLockout && FireInRedPvE.CanUse(out act))
        {
            return true;
        }

        // Force Holy/Comet usage during the final 3 seconds before Starry.
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

        if (PomMotifPvE.CanUse(out act)) return true;
        if (WingMotifPvE.CanUse(out act)) return true;
        if (ClawMotifPvE.CanUse(out act)) return true;
        if (MawMotifPvE.CanUse(out act)) return true;

        if (!isMedicated && HammerMotifPvE.CanUse(out act)) return true;
        if (StarrySkyMotifPvE.CanUse(out act)) return true;

        return base.GeneralGCD(out act);
    }

    #endregion
}