namespace RotationSolver.ExtraRotations.Magical;

[Rotation("BeirutaPCT", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/ExtraRotations/Magical/BeirutaPCT.cs")]
public sealed class BeirutaPCT : PictomancerRotation
{
    #region Config Options

    public enum HammerEarlyHoldSeconds
    {
        Sec0  = 0,
        Sec5  = 5,
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

    // Starry timing helpers (GCD gating)
    private float StarryIn =>
        HasStarryMuse ? 0f : StarryMusePvE.Cooldown.RecastTimeRemainOneCharge;

    private bool StarryWithin3 =>
        !HasStarryMuse && StarryIn <= 3f && IsBurst;

    private bool StarryWithin20 =>
        !HasStarryMuse && StarryIn <= 20f && StarryIn > 3f && IsBurst;

    // Reserve 2 Paint for Holy/Comet when Starry is soon.
    // Meaning: do not spend Paint on Holy/Comet if we'd end up at 2 or less.
    private bool ShouldReservePaintForHolyComet =>
        StarryWithin20 && Paint <= 2 && IsBurst;

    private bool HolyCometAllowedByPaintReserve =>
        !ShouldReservePaintForHolyComet && IsBurst;

    // you can also extend this with exceptions if needed
    private bool NeedsStrikingMovementRescue(IAction nextGCD) =>
        InCombat
        && IsMoving
        && !NextIsMovementSafeGcd(nextGCD)
        && !HasSwift
        && !HasHammerTime
        && NextAbilityToNextGCD < 0.6f;

    private long _starPrismUsedAtMs = 0;

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

    // Overcap protection: about to reach 2 charges within 5s
    private bool StrikingOvercapSoon30 =>
        StrikingMusePvE.Cooldown.CurrentCharges == 1
        && StrikingMusePvE.Cooldown.WillHaveOneCharge(30f);

    // (meaning: the next charge arrives within 20 seconds)
    private long _holyUsedInOpenerAtMs = 0;
    private long _fangedUsedInStarryAtMs = 0;
    private long _prepStrikingUsedAtMs = 0;

    private static bool InBurstStatus =>
        StatusHelper.PlayerHasStatus(true, StatusID.StarryMuse);

    private static bool HasInspiration =>
        StatusHelper.PlayerHasStatus(true, StatusID.Inspiration);

    private long _starryUsedAtMs = 0;

    #region Countdown logic

    // Defines logic for actions to take during the countdown before combat starts.
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

        return base.CountDownAction(remainTime);
    }

    #endregion

    #region Additional oGCD Logic

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;

        // Same opener timing gate used in AttackAbility()
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

        // If Medicated: Swiftcast any creature motif if it is the next GCD.
        if (isMedicated)
        {
            bool isCreatureMotif =
                nextGCD.IsTheSameTo(false, PomMotifPvE)
                || nextGCD.IsTheSameTo(false, WingMotifPvE)
                || nextGCD.IsTheSameTo(false, ClawMotifPvE)
                || nextGCD.IsTheSameTo(false, MawMotifPvE);

            if (isCreatureMotif && SwiftcastPvE.CanUse(out act))
                return true;

            // Important: do NOT run the normal motif-swift logic while medicated.
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
        // Mitigations
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
        // Mitigations
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
        // ---------- Burst / timing helpers ----------
        int adjustCombatTimeForOpener = DataCenter.PlayerSyncedLevel() < 92 ? 2 : 5;

        long nowMs = Environment.TickCount64;

        bool madeenAvailable = RetributionOfTheMadeenPvE.CanUse(out _);

        // Mog overwrite restriction (same as your logic, just grouped)
        bool mogRestrictedWindow =
            _fangedUsedInStarryAtMs != 0
            && (nowMs - _fangedUsedInStarryAtMs) < 160_000;

        bool mogReady = MogOfTheAgesPvE.CanUse(out _);
        bool mogAllowedNow = mogReady && (!mogRestrictedWindow || HasStarryMuse);

        // Starry timing (single source of truth)
        float starryIn = HasStarryMuse ? 0f : StarryMusePvE.Cooldown.RecastTimeRemainOneCharge;

        bool starryWithin60 = !HasStarryMuse && starryIn <= 60f && IsBurst;
        bool starryWithin40 = !HasStarryMuse && starryIn <= 40f && IsBurst;
        bool starryReadySoon15 = !HasStarryMuse && starryIn <= 3f && IsBurst;
        bool starryReadySoon10 = !HasStarryMuse && starryIn <= 10f && IsBurst;
        bool starryWithin30 = !HasStarryMuse && starryIn <= 30f && IsBurst;

        // Use your existing prep definition to avoid interfering with the ~10s prep Striking logic.
        // (You already have starryReadySoon10 defined as WillHaveOneCharge(12f) && IsBurst)
        bool allowHammerDumpFor30sLead = starryWithin30 && !starryReadySoon10;

        bool starryJustUsed1s =
            _starryUsedAtMs != 0
            && (nowMs - _starryUsedAtMs) < 1500;

        bool starryJustUsed5s =
            _starryUsedAtMs != 0
            && (nowMs - _starryUsedAtMs) < 9000;

        // ---------- Striking Muse (HammerTime) reserve logic ----------
        // Requirement you stated:
        // "It is OK to be at 0 charges as long as Striking will have 1 charge
        // at least 10s BEFORE Starry is ready."
        //
        // So: we only preserve when spending the last charge would mean we *cannot*
        // regain a charge by (Starry - 10s).
        float strikingNeededIn = MathF.Max(0f, starryIn - 5f);

        bool preserveStrikingForStarry =
            starryWithin60
            && StrikingMusePvE.Cooldown.CurrentCharges == 1
            && StrikingMusePvE.Cooldown.RecastTimeRemainOneCharge > strikingNeededIn;

        // Overcap soon (2 charges) within 10s: spend one unless we're preserving
        bool strikingOvercapSoon30 =
            StrikingMusePvE.Cooldown.CurrentCharges == 1
            && StrikingMusePvE.Cooldown.RecastTimeRemainOneCharge <= 30f;

        // Keep at least 1 Living Muse charge if Starry is soon (your existing intent)
        bool preserveLivingForBurst =
            CombatTime > 5f
            && !HasStarryMuse
            && starryWithin40
            && LivingMusePvE.Cooldown.CurrentCharges <= 1;

        // SAFEGUARD: if we are in Starry but somehow have no HammerTime,
        // force Striking Muse ASAP to enable hammer chain.
        if (HasStarryMuse
            && !HasHammerTime
            && InCombat
            && StrikingMusePvE.Cooldown.CurrentCharges > 0
            && StrikingMusePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        // ---------- Palette upkeep ----------
        if (!starryReadySoon15
            && !starryJustUsed1s
            && !HasMonochromeTones
            && !HasSubtractivePalette
            && SubtractivePalettePvE.CanUse(out act))
        {
            return true;
        }

        // ---------- Striking usage priorities ----------
        // 1) Prep: deliberately spend Striking about 10s before Starry (to ensure HammerTime exists)
        if (starryReadySoon10
            && CombatTime > adjustCombatTimeForOpener
            && IsBurst
            && StrikingMusePvE.CanUse(out act, usedUp: true))
        {
            _prepStrikingUsedAtMs = nowMs;
            return true;
        }

        // 2) Overcap protection: spend if we would cap in ~15s (unless preserving for Starry)
        if (strikingOvercapSoon30
            && CombatTime > adjustCombatTimeForOpener
            && !preserveStrikingForStarry
            && IsBurst
            && StrikingMusePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        // 3) Movement rescue: spend Striking if moving and next GCD unsafe (unless preserving for Starry)
        if (NeedsStrikingMovementRescue(nextGCD)
            && StrikingMusePvE.Cooldown.CurrentCharges > 0
            && !preserveStrikingForStarry
            && IsBurst
            && StrikingMusePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        // Madeen (Starry burst): try first
        if (HasStarryMuse
            && !starryJustUsed5s
            && IsBurst
            && !InPostPrismDelayedBlockWindow
            && RetributionOfTheMadeenPvE.CanUse(out act))
        {
            return true;
        }

        // Mog: then try
        if (!starryJustUsed5s
            && mogAllowedNow
            && IsBurst
            && !HasHyperphantasia
            && !InPostPrismDelayedBlockWindow
            && MogOfTheAgesPvE.CanUse(out act))
        {
            return true;
        }

        // else: Mog is ready but intentionally held
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
                // Only start the 160s Mog restriction window if Fanged was used during Starry Muse.
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
        // 1) Swiftcast Rainbow Drip (highest priority)
        if (RainbowDripSwift
            && !HasRainbowBright
            && nextGCD.IsTheSameTo(false, RainbowDripPvE)
            && SwiftcastPvE.CanUse(out act))
        {
            return true;
        }

        // 2) Swiftcast only the configured Motif
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

        // Opener pot — absolute priority first 5s
        if (InCombat && CombatTime <= 5f && HasHammerTime && UseBurstMedicine(out act))
        {
            return true;
        }

        bool isMedicated = StatusHelper.PlayerHasStatus(true, StatusID.Medicated);

        // Define "Starry ready soon" similarly to your prep logic
        float starryIn = HasStarryMuse ? 0f : StarryMusePvE.Cooldown.RecastTimeRemainOneCharge;
        bool starryReadySoon5 = !HasStarryMuse && starryIn <= 5f && IsBurst;

        // PRE-POT: use tincture shortly before Starry comes up
        // (Skip if already Medicated so you don't waste checks / re-issue)
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

        //Opener requirements
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

        // Starry <2s gate
        bool starryReadySoon2 = HasStarryMuse || StarryMusePvE.Cooldown.WillHaveOneCharge(0f);
        bool starryReadySoon10 = !HasStarryMuse && StarryMusePvE.Cooldown.WillHaveOneCharge(12f) && IsBurst;

        // Block hammer chain ONLY after we did the ~5s prep Striking, until Starry <2s
        bool blockPrepHammerChain = _prepStrikingUsedAtMs != 0 && InCombat && !starryReadySoon2;

        int hyperStacks = StatusHelper.PlayerStatusStack(true, StatusID.Hyperphantasia);
        bool reserveHyperForPrism = HasStarstruck && hyperStacks == 1;

        bool starryWithin30 = !HasStarryMuse && StarryMusePvE.Cooldown.RecastTimeRemainOneCharge <= 30f;
        bool allowHammerDumpFor30sLead = starryWithin30 && !starryReadySoon10;

        // Clear marker once it’s no longer relevant
        if (!InCombat || starryReadySoon2)
        {
            _prepStrikingUsedAtMs = 0;
        }

        // some gcd priority
        if (HasStarryMuse && HasInspiration && !reserveHyperForPrism)
        {
            if (CometInBlackPvE.CanUse(out act, skipCastingCheck: true))
            {
                return true;
            }
        }

        // Extra: Subtractive Inks under Inspiration (before StarPrism)
        // Rule A: block Subtractive Inks when Starry is within 3s
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

        int earlyHoldSec = (int)HammerEarlyHold;         // 5 / 10 / 15
        float earlyRemainThreshold = 30f - earlyHoldSec; // 25 / 20 / 15

        // Early window = first X seconds after HammerTime starts
        // i.e., while remaining >= (30 - X)
        bool hammerEarlyWindow = HasHammerTime && hammerRemain >= earlyRemainThreshold;

        // After early window = the rest of HammerTime
        bool hammerAfterWindow = HasHammerTime && hammerRemain > 0f && hammerRemain < earlyRemainThreshold;

        // Helper: whether we are allowed to use hammer chain right now
        // - During Starry: if moving, ignore (HasInspiration && HasSubtractivePalette)
        // - Otherwise: keep the restriction
        bool hammerAllowedByInspirationRule =
            HasStarryMuse ? (IsMoving || !(HasInspiration && HasSubtractivePalette)) : !(HasInspiration && HasSubtractivePalette);

        // 1) During Starry: use hammer chain any time it’s allowed (moving ignores Inspiration rule)
        if (HasStarryMuse && InCombat && !HasSwift && !blockPrepHammerChain && hammerAllowedByInspirationRule)
        {
            if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (HammerBrushPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (!blockEarlyHammerStamp && HammerStampPvE.CanUse(out act, skipComboCheck: true)) return true;
        }

        // 2) Not Starry + first 5s: movement rescue ONLY (commit window), keep restriction
        if (!HasStarryMuse && hammerEarlyWindow && InCombat && IsMoving && canCommitGcdNow && !HasSwift && !blockPrepHammerChain && hammerAllowedByInspirationRule)
        {
            if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (HammerBrushPvE.CanUse(out act, skipComboCheck: true)) return true;
            if (!blockEarlyHammerStamp && HammerStampPvE.CanUse(out act, skipComboCheck: true)) return true;
        }

        // 3) Not Starry + remaining 30s: spend ASAP (like the old behaviour), keep restriction
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

        // timings for motif casting
        if (ScenicMusePvE.Cooldown.RecastTimeRemainOneCharge <= 30 && !HasStarryMuse && !HasHyperphantasia)
        {
            if (StarrySkyMotifPvE.CanUse(out act) && !HasHyperphantasia) return true;

            // Also prep Weapon motif in the same window
            if (!isMedicated && !WeaponMotifDrawn && HammerMotifPvE.CanUse(out act)) return true;
        }

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

        if ((SteelMusePvE.Cooldown.HasOneCharge || SteelMusePvE.Cooldown.RecastTimeRemainOneCharge <= WeaponMotifPvE.Info.CastTime)
            && !HasStarryMuse && !HasHyperphantasia)
        {
            if (!isMedicated && HammerMotifPvE.CanUse(out act))
            {
                return true;
            }
        }

        // moving Holy/Comet: only when about to commit a GCD (AST moving Combust analogue)
        {
            if (HolyCometMoving && InCombat && IsMoving && canCommitGcdNow && !HasSwift && !HasHammerTime && HolyCometAllowedByPaintReserve)
            {
                if (CometInBlackPvE.CanUse(out act)) return true;
                if (HolyInWhitePvE.CanUse(out act)) return true;
            }
        }

        // When in swift management
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

        //white paint over cap protection
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

        // AOE Subtractive Inks
        if (!StarryWithin3)
        {
            if (ThunderIiInMagentaPvE.CanUse(out act)) return true;
            if (StoneIiInYellowPvE.CanUse(out act)) return true;
            if (BlizzardIiInCyanPvE.CanUse(out act)) return true;
        }

        //AOE Additive Inks
        if (WaterIiInBluePvE.CanUse(out act)) return true;
        if (AeroIiInGreenPvE.CanUse(out act)) return true;
        if (FireIiInRedPvE.CanUse(out act)) return true;

        //ST Subtractive Inks
        if (!StarryWithin3)
        {
            if (ThunderInMagentaPvE.CanUse(out act)) return true;
            if (StoneInYellowPvE.CanUse(out act)) return true;
            if (BlizzardInCyanPvE.CanUse(out act)) return true;
        }

        //ST Additive Inks
        if (WaterInBluePvE.CanUse(out act)) return true;
        if (AeroInGreenPvE.CanUse(out act)) return true;

        if (!blockEarlyFire && !fireHardLockout && FireInRedPvE.CanUse(out act))
        {
            return true;
        }

        // Extra: Force Holy/Comet in the last 3s before Starry
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

        // In comabt fallback in case of no target, allow GCD to roll on motif refresh
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