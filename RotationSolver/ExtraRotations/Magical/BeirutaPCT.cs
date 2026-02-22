namespace RotationSolver.ExtraRotations.Magical;

[Rotation("BeirutaPCT", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/ExtraRotations/Magical/BeirutaPCT.cs")]


public sealed class BeirutaPCT : PictomancerRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name =
"Please note that this rotation is optimised for combats that start with a countdown Rainbow Drip cast.\n" +
"• Enable Spell Intercept to manually use Rainbow Drip before the boss becomes untargetable.\n" +
"• This rotation is designed to align Madeen within burst windows.\n" +
"• During burst, it attempts to use two Comet in Black casts by skipping one Hammer action.\n" +
"• Hyperphantasia is prioritised early in burst to allow earlier movement flexibility.\n" +
"• Intercept Rainbow Drip automatically uses Swiftcast when Rainbow Drip is queued.\n" +
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
    #endregion
    

    private long _holyUsedInOpenerAtMs = 0;
    private long _fangedUsedInStarryAtMs = 0;
    private long _prepStrikingUsedAtMs = 0;
    private static bool InBurstStatus => StatusHelper.PlayerHasStatus(true, StatusID.StarryMuse);
    private static bool HasInspiration =>
    StatusHelper.PlayerHasStatus(true, StatusID.Inspiration);

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

    if (RainbowDripSwift && !HasRainbowBright && nextGCD.IsTheSameTo(false, RainbowDripPvE) && SwiftcastPvE.CanUse(out act))
    {
        return true;
    }

    bool isMedicated = StatusHelper.PlayerHasStatus(true, StatusID.Medicated);

    // If Medicated: Swiftcast any creature motif if it is the next GCD.
    if (isMedicated)
    {
        bool isCreatureMotif =
            nextGCD.IsTheSameTo(false, PomMotifPvE) ||
            nextGCD.IsTheSameTo(false, WingMotifPvE) ||
            nextGCD.IsTheSameTo(false, ClawMotifPvE) ||
            nextGCD.IsTheSameTo(false, MawMotifPvE);

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
    if (InCombat
    && CombatTime <= 5f
    && StrikingMusePvE.CanUse(out act, usedUp: true, skipCastingCheck: true))
{
    return true;
}

  // Opener pot — absolute priority first 10s
if (InCombat
    && CombatTime <= 10f
    && CombatTime >= 1f
    && HasHammerTime
    && UseBurstMedicine(out act))
{
    return true;
}


        bool starryReadySoon10 =
    !HasStarryMuse &&
    StarryMusePvE.Cooldown.WillHaveOneCharge(10f);

        bool burstTimingCheckerStriking = !ScenicMusePvE.Cooldown.WillHaveOneCharge(60) || HasStarryMuse || !StarryMusePvE.EnoughLevel;
        // Bursts
        int adjustCombatTimeForOpener = DataCenter.PlayerSyncedLevel() < 92 ? 2 : 5;

bool madeenAvailable = RetributionOfTheMadeenPvE.CanUse(out _);

long nowMs = Environment.TickCount64;
bool mogRestrictedWindow =
    _fangedUsedInStarryAtMs != 0 &&
    (nowMs - _fangedUsedInStarryAtMs) < 160_000;

bool mogReady = MogOfTheAgesPvE.CanUse(out _); // "ready" for overwrite-blocking
bool mogAllowedNow = mogReady && (!mogRestrictedWindow || HasStarryMuse);
bool starrySoon = StarryMusePvE.Cooldown.WillHaveOneCharge(40f);
bool starryReadySoon60 =
    !HasStarryMuse &&
    StarryMusePvE.Cooldown.WillHaveOneCharge(60f);

bool starryReadySoon5 =
    !HasStarryMuse &&
    StarryMusePvE.Cooldown.WillHaveOneCharge(5f);

// Hold the last Striking charge until ~5s before Starry
bool preserveStrikingForStarry =
    starryReadySoon60 &&
    !starryReadySoon5 &&
    StrikingMusePvE.Cooldown.CurrentCharges <= 1;

// Keep at least 1 Living Muse charge if burst is within 40s (and we are not already in Starry)
bool preserveLivingForBurst =
    CombatTime > 5f &&
    !HasStarryMuse
    && starrySoon
    && LivingMusePvE.Cooldown.CurrentCharges <= 1;
    
        if (IsBurst
    && CombatTime > adjustCombatTimeForOpener
    && StarryMusePvE.CanUse(out act, skipCastingCheck: true))
{
    return true;
}

        if (!starryReadySoon10
    && SubtractivePalettePvE.CanUse(out act)
    && !HasSubtractivePalette)
{
    return true;
}
// Deliberately spend the LAST Striking charge at ~5s before Starry
if (starryReadySoon5
    && StrikingMusePvE.Cooldown.CurrentCharges == 1
    && CombatTime > adjustCombatTimeForOpener
    && StrikingMusePvE.CanUse(out act, usedUp: true))
{
    _prepStrikingUsedAtMs = nowMs;
    return true;
}

        if (!preserveStrikingForStarry
    && CombatTime > adjustCombatTimeForOpener
    && StrikingMusePvE.CanUse(out act, usedUp: true)
    && burstTimingCheckerStriking)
{
    return true;
}


        if (HasStarryMuse)
        {
            if (RetributionOfTheMadeenPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (mogAllowedNow && MogOfTheAgesPvE.CanUse(out act))
{
    return true;
}
// else: Mog is ready but intentionally held

if (!preserveLivingForBurst)
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
        CanvasFlags.Pom       => nextGCD.IsTheSameTo(false, PomMotifPvE),
        CanvasFlags.Wing      => nextGCD.IsTheSameTo(false, WingMotifPvE),
        CanvasFlags.Claw      => nextGCD.IsTheSameTo(false, ClawMotifPvE),
        CanvasFlags.Maw       => nextGCD.IsTheSameTo(false, MawMotifPvE),
        CanvasFlags.Weapon    => nextGCD.IsTheSameTo(false, HammerMotifPvE),
        CanvasFlags.Landscape => nextGCD.IsTheSameTo(false, StarrySkyMotifPvE),
        _ => false
    };

    if (shouldSwiftMotif && SwiftcastPvE.CanUse(out act))
        return true;
}


        if ((MergedStatus.HasFlag(AutoStatus.DefenseArea) || StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.TemperaCoat)) && TemperaGrassaPvE.CanUse(out act))
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
    if (!InCombat)
    _holyUsedInOpenerAtMs = 0;    
    bool isMedicated =
    StatusHelper.PlayerHasStatus(true, StatusID.Medicated);
    bool blockEarlyFire = InCombat && CombatTime < 2f;
    bool blockEarlyHammerStamp = InCombat && CombatTime < 10f && !HasHyperphantasia; 
    bool blockEarlyHolyAndLivingMotif = InCombat && CombatTime < 2f && !HasHammerTime;

        //Opener requirements
if (CombatTime < 5)
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
    InCombat &&
    _holyUsedInOpenerAtMs != 0 &&
    (nowMs - _holyUsedInOpenerAtMs) < 8000; 
    if (fireHardLockout)
{
    act = null;
    return false;
}

// Starry <2s gate
bool starryReadySoon2 =
    HasStarryMuse || StarryMusePvE.Cooldown.WillHaveOneCharge(1f);
bool starryReadySoon10 =
    !HasStarryMuse &&
    StarryMusePvE.Cooldown.WillHaveOneCharge(12f);

// Block hammer chain ONLY after we did the ~5s prep Striking, until Starry <2s
bool blockPrepHammerChain =
    _prepStrikingUsedAtMs != 0
    && InCombat
    && !starryReadySoon2;

// Clear marker once it’s no longer relevant
if (!InCombat || starryReadySoon2)
{
    _prepStrikingUsedAtMs = 0;
}


        // some gcd priority
       if (RainbowDripPvE.CanUse(out act) && HasRainbowBright)
{
    return true;
}


        if (HasStarryMuse)
        {
            if (CometInBlackPvE.CanUse(out act, skipCastingCheck: true))
            {
                return true;
            }
        }
        if (StarPrismPvE.CanUse(out act) && HasStarstruck)
        {
            return true;
        }

        if (!blockPrepHammerChain && !(HasInspiration && HasSubtractivePalette))
{
    if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true) ||
        HammerBrushPvE.CanUse(out act, skipComboCheck: true) ||
        (!blockEarlyHammerStamp && HammerStampPvE.CanUse(out act, skipComboCheck: true)))
    {
        return true;
    }
}


        if (!InCombat)
        {
            if (PomMotifPvE.CanUse(out act))
            {
                return true;
            }

            if (WingMotifPvE.CanUse(out act))
            {
                return true;
            }

            if (ClawMotifPvE.CanUse(out act))
            {
                return true;
            }

            if (MawMotifPvE.CanUse(out act))
            {
                return true;
            }

            if (!isMedicated && HammerMotifPvE.CanUse(out act))
            {
                return true;
            }

            if (StarrySkyMotifPvE.CanUse(out act)
    && !StatusHelper.PlayerHasStatus(true, StatusID.Hyperphantasia)
    && !StatusHelper.PlayerHasStatus(true, StatusID.Medicated))
{
    return true;
}


            if (RainbowDripPvE.CanUse(out act))
            {
                return true;
            }
        }

        // timings for motif casting
if (ScenicMusePvE.Cooldown.RecastTimeRemainOneCharge <= 30 && !HasStarryMuse && !HasHyperphantasia)
{
    if (StarrySkyMotifPvE.CanUse(out act) && !HasHyperphantasia)
        return true;

    // Also prep Weapon motif in the same window
    if (!isMedicated && !WeaponMotifDrawn && HammerMotifPvE.CanUse(out act))
        return true;
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

        if ((SteelMusePvE.Cooldown.HasOneCharge || SteelMusePvE.Cooldown.RecastTimeRemainOneCharge <= WeaponMotifPvE.Info.CastTime) && !HasStarryMuse && !HasHyperphantasia)
        {
            if (!isMedicated && HammerMotifPvE.CanUse(out act))
            {
                return true;
            }
        }

        // white/black paint use while moving
        if (IsMoving && !HasSwift)
{
    if (!blockPrepHammerChain && !(HasInspiration && HasSubtractivePalette))
    {
        if (PolishingHammerPvE.CanUse(out act)) return true;
        if (HammerBrushPvE.CanUse(out act)) return true;
        if (!blockEarlyHammerStamp && HammerStampPvE.CanUse(out act)) return true;
    }

            if (HolyCometMoving)
{
    if (!starryReadySoon10 && CometInBlackPvE.CanUse(out act))
    {
        return true;
    }

    if (HolyInWhitePvE.CanUse(out act))
    {
        return true;
    }
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

            if (StarrySkyMotifPvE.CanUse(out act, skipCastingCheck: MotifSwiftCast is CanvasFlags.Landscape) && !HasHyperphantasia && MotifSwiftCast is CanvasFlags.Landscape)
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

        //AOE Subtractive Inks
        if (ThunderIiInMagentaPvE.CanUse(out act))
        {
            return true;
        }

        if (StoneIiInYellowPvE.CanUse(out act))
        {
            return true;
        }

        if (BlizzardIiInCyanPvE.CanUse(out act))
        {
            return true;
        }

        //AOE Additive Inks
        if (WaterIiInBluePvE.CanUse(out act))
        {
            return true;
        }

        if (AeroIiInGreenPvE.CanUse(out act))
        {
            return true;
        }

        if (FireIiInRedPvE.CanUse(out act))
{
    return true;
}




        //ST Subtractive Inks
        if (ThunderInMagentaPvE.CanUse(out act))
        {
            return true;
        }

        if (StoneInYellowPvE.CanUse(out act))
        {
            return true;
        }

        if (BlizzardInCyanPvE.CanUse(out act))
        {
            return true;
        }

        //ST Additive Inks
        if (WaterInBluePvE.CanUse(out act))
        {
            return true;
        }

        if (AeroInGreenPvE.CanUse(out act))
        {
            return true;
        }

        if (!blockEarlyFire && !fireHardLockout && FireInRedPvE.CanUse(out act))
{
    return true;
}




        // In comabt fallback in case of no target, allow GCD to roll on motif refresh
        if (PomMotifPvE.CanUse(out act))
        {
            return true;
        }

        if (WingMotifPvE.CanUse(out act))
        {
            return true;
        }

        if (ClawMotifPvE.CanUse(out act))
        {
            return true;
        }

        if (MawMotifPvE.CanUse(out act))
        {
            return true;
        }

        if (!isMedicated && HammerMotifPvE.CanUse(out act))
        {
            return true;
        }

        if (StarrySkyMotifPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }

    #endregion
}