namespace RotationSolver.RebornRotations.Magical;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/RebornRotations/Magical/PCT_Reborn.cs")]

public sealed class PCT_Reborn : PictomancerRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use HolyInWhite or CometInBlack while moving")]
    public bool HolyCometMoving { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Paint overcap protection.")]
    public bool UseCapCometHoly { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use the paint overcap protection (will still use comet while moving if the setup is on)")]
    public bool UseCapCometOnly { get; set; } = false;

    [Range(1, 5, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Paint overcap protection limit. How many paint you need to be at for it to use Holy out of burst (Setting is ignored when you have Hyperphantasia)")]
    public int HolyCometMax { get; set; } = 5;

    [RotationConfig(CombatType.PvE, Name = "Use swiftcast on Rainbow Drip (Priority over below settings)")]
    public bool RainbowDripSwift { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use swiftcast on Motif")]
    public bool MotifSwiftCastSwift { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Which Motif to use swiftcast on")]
    public CanvasFlags MotifSwiftCast { get; set; } = CanvasFlags.Weapon;

    [RotationConfig(CombatType.PvE, Name = "Prevent the use of defense abilties during burst")]
    private bool BurstDefense { get; set; } = true;

    #endregion

    private static bool InBurstStatus => StatusHelper.PlayerHasStatus(true, StatusID.StarryMuse);

    #region Countdown logic
    // Defines logic for actions to take during the countdown before combat starts.
    protected override IAction? CountDownAction(float remainTime)
    {
        IAction act;
        if (remainTime < RainbowDripPvE.Info.CastTime + CountDownAhead)
        {
            if (StrikingMusePvE.CanUse(out act) && WeaponMotifDrawn)
            {
                return act;
            }
        }
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
        if (InCombat)
        {
            if (RainbowDripSwift && nextGCD.IsTheSameTo(false, RainbowDripPvE) && SwiftcastPvE.CanUse(out act))
            {
                return true;
            }

            if (MotifSwiftCastSwift)
            {
                if (MotifSwiftCast switch
                {
                    CanvasFlags.Pom => nextGCD.IsTheSameTo(false, PomMotifPvE),
                    CanvasFlags.Wing => nextGCD.IsTheSameTo(false, WingMotifPvE),
                    CanvasFlags.Claw => nextGCD.IsTheSameTo(false, ClawMotifPvE),
                    CanvasFlags.Maw => nextGCD.IsTheSameTo(false, MawMotifPvE),
                    CanvasFlags.Weapon => nextGCD.IsTheSameTo(false, HammerMotifPvE),
                    CanvasFlags.Landscape => nextGCD.IsTheSameTo(false, StarrySkyMotifPvE),
                    _ => false
                } && SwiftcastPvE.CanUse(out act))
                {
                    return true;
                }
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
        bool burstTimingCheckerStriking = !ScenicMusePvE.Cooldown.WillHaveOneCharge(60) || HasStarryMuse || !StarryMusePvE.EnoughLevel;
        // Bursts
        int adjustCombatTimeForOpener = DataCenter.PlayerSyncedLevel() < 92 ? 2 : 5;
        if (StarryMusePvE.CanUse(out act) && CombatTime > adjustCombatTimeForOpener && IsBurst)
        {
            return true;
        }

        if (CombatTime > adjustCombatTimeForOpener && StrikingMusePvE.CanUse(out act, usedUp: true) && burstTimingCheckerStriking)
        {
            return true;
        }

        if (SubtractivePalettePvE.CanUse(out act) && !HasSubtractivePalette)
        {
            return true;
        }

        if (HasStarryMuse)
        {
            if (FangedMusePvE.CanUse(out act, usedUp: true))
            {
                return true;
            }

            if (RetributionOfTheMadeenPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (RetributionOfTheMadeenPvE.CanUse(out act))
        {
            return true;
        }

        if (MogOfTheAgesPvE.CanUse(out act))
        {
            return true;
        }

        if (StrikingMusePvE.CanUse(out act, usedUp: true) && burstTimingCheckerStriking)
        {
            return true;
        }

        if (PomMusePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (WingedMusePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (ClawedMusePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        //Basic Muses - not real actions
        //if (ScenicMusePvE.CanUse(out act)) return true;
        //if (SteelMusePvE.CanUse(out act, usedUp: true)) return true;
        //if (LivingMusePvE.CanUse(out act, usedUp: true)) return true;
        return base.AttackAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if ((MergedStatus.HasFlag(AutoStatus.DefenseArea) || StatusHelper.PlayerWillStatusEndGCD(2, 0, true, StatusID.TemperaCoat)) && TemperaGrassaPvE.CanUse(out act))
        {
            return true;
        }

        if (HasStarryMuse && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic

    protected override bool GeneralGCD(out IAction? act)
    {
        //Opener requirements
        if (CombatTime < 5)
        {
            if (HolyInWhitePvE.CanUse(out act))
            {
                return true;
            }

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

        if (PolishingHammerPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        if (HammerBrushPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        if (HammerStampPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
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

            if (HammerMotifPvE.CanUse(out act))
            {
                return true;
            }

            if (StarrySkyMotifPvE.CanUse(out act) && !StatusHelper.PlayerHasStatus(true, StatusID.Hyperphantasia))
            {
                return true;
            }

            if (RainbowDripPvE.CanUse(out act))
            {
                return true;
            }
        }

        // timings for motif casting
        if (ScenicMusePvE.Cooldown.RecastTimeRemainOneCharge <= 15 && !HasStarryMuse && !HasHyperphantasia)
        {
            if (StarrySkyMotifPvE.CanUse(out act) && !HasHyperphantasia)
            {
                return true;
            }
        }
        if ((LivingMusePvE.Cooldown.HasOneCharge || LivingMusePvE.Cooldown.RecastTimeRemainOneCharge <= CreatureMotifPvE.Info.CastTime * 1.7) && !HasStarryMuse && !HasHyperphantasia)
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
        }
        if ((SteelMusePvE.Cooldown.HasOneCharge || SteelMusePvE.Cooldown.RecastTimeRemainOneCharge <= WeaponMotifPvE.Info.CastTime) && !HasStarryMuse && !HasHyperphantasia)
        {
            if (HammerMotifPvE.CanUse(out act))
            {
                return true;
            }
        }

        // white/black paint use while moving
        if (IsMoving && !HasSwift)
        {
            if (PolishingHammerPvE.CanUse(out act))
            {
                return true;
            }

            if (HammerBrushPvE.CanUse(out act))
            {
                return true;
            }

            if (HammerStampPvE.CanUse(out act))
            {
                return true;
            }

            if (HolyCometMoving)
            {
                if (CometInBlackPvE.CanUse(out act))
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

            if (HammerMotifPvE.CanUse(out act, skipCastingCheck: MotifSwiftCast is CanvasFlags.Weapon) && MotifSwiftCast is CanvasFlags.Weapon)
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

        if (FireInRedPvE.CanUse(out act))
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

        if (HammerMotifPvE.CanUse(out act))
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