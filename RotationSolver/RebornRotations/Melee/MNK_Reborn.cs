using System.ComponentModel;

namespace RotationSolver.RebornRotations.Melee;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.35", Description = "Uses Lunar Solar Opener from The Balance")]
[SourceCode(Path = "main/RebornRotations/Melee/MNK_Reborn.cs")]


public sealed class MNK_Reborn : MonkRotation
{
    #region Config Options

    public enum RiddleOfFireFirst : byte
    {
        [Description("Brotherhood")] Brotherhood,

        [Description("Perfect Balance")] PerfectBalance,
    }

    public enum MasterfulBlitzUse : byte
    {
        [Description("Use Immediately")] UseAsAble,

        [Description("With ROF burst logic")] RiddleOfFireUse,
    }

    [RotationConfig(CombatType.PvE, Name = "Use Form Shift")]
    public bool AutoFormShift { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Auto Use Perfect Balance (single target full auto mode, turn me off if you want total control of PB)")]
    public bool AutoPB_Boss { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Auto Use Perfect Balance (aoe aggressive PB dump, turn me off if you don't want to waste PB in boss fight)")]
    public bool AutoPB_AOE { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Use Howling Fist/Enlightenment as a ranged attack verses single target enemies")]
    public bool HowlingSingle2 { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Enable TEA Checker.")]
    public bool EnableTEAChecker { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Masterful Blitz abilites as soon as they are available.")]
    public MasterfulBlitzUse MBAbilities { get; set; } = MasterfulBlitzUse.RiddleOfFireUse;

    [RotationConfig(CombatType.PvE, Name = "Use Riddle of Fire after this ability")]
    public RiddleOfFireFirst ROFFirst { get; set; } = RiddleOfFireFirst.Brotherhood;
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        // gap closer at the end of countdown
        if (remainTime <= 0.5 && ThunderclapPvE.CanUse(out IAction? act))
        {
            return act; // need to face target to trigger
        }
        // true north before pull
        if (remainTime <= 2 && TrueNorthPvE.CanUse(out act))
        {
            return act;
        }
        // turn on 5 chakra at -5 prepull 
        if (remainTime <= 5 && Chakra < 5)
        {
            if (EnlightenedMeditationPvE.CanUse(out act))
            {
                return act;
            }
            if (ForbiddenMeditationPvE.CanUse(out act))
            {
                return act;
            }
            if (InspiritedMeditationPvE.CanUse(out act))
            {
                return act;
            }
            if (!ForbiddenMeditationPvE.Info.EnoughLevelAndQuest() && SteeledMeditationPvE.CanUse(out act))
            {
                return act;
            }
        }

        // formShift to prep opening
        return remainTime < 15 && FormShiftPvE.CanUse(out act) ? act : base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (EnableTEAChecker && Target.Name.ToString() == "Jagd Doll" && Target.GetHealthRatio() < 0.25)
        {
            return base.EmergencyAbility(nextGCD, out act);
        }

        // PerfectBalancePvE after first gcd + TheForbiddenChakraPvE after second gcd
        // fail to weave both after first gcd - rsr doesn't have enough time to react to both spells
        // you pot -2s (real world -3s) prepull or after 2nd gcd!!! 
        // there is a small chance PB is not pressed in time if put in AttackAbility
        // start the fight 8 yarms away from boss for double weaving
        // 'The form shift and meditation prepull are implied. Prepull pot should win out, but choosing to press it in the first few weave slots shouldn¡¯t result in more than a single digit loss'
        // 'there may be a delay before it can be used. Pushing it to the 2nd weave slot should avoid this.'
        if (AutoPB_Boss && InCombat && CombatElapsedLess(3) && PerfectBalancePvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        // you need to position yourself in the centre of the mobs if they are large, that range is only 3 yarms
        if (AutoPB_AOE && NumberOfHostilesInRange >= 2)
        {
            if (PerfectBalancePvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        // opener 2nd burst
        if (AutoPB_Boss
            && HasRiddleOfFire && InBrotherhood
            && IsLastGCD(true, DragonKickPvE, LeapingOpoPvE, BootshinePvE) // PB must follow an Opo
            && !HasFormlessFist && !HasFiresRumination && !HasWindsRumination)
        {
            if (PerfectBalancePvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        // odd min burst
        if (AutoPB_Boss
            && HasRiddleOfFire
            && !PerfectBalancePvE.Cooldown.JustUsedAfter(20)
            && IsLastGCD(true, DragonKickPvE, LeapingOpoPvE, BootshinePvE)) // PB must follow an Opo 
        {
            if (PerfectBalancePvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }

        // even min burst
        if (AutoPB_Boss
            && !HasRiddleOfFire
            && RiddleOfFirePvE.Cooldown.WillHaveOneChargeGCD(3) && BrotherhoodPvE.Cooldown.WillHaveOneCharge(3)
            && IsLastGCD(true, DragonKickPvE, LeapingOpoPvE, BootshinePvE)) // PB must follow an Opo 
        {
            if (PerfectBalancePvE.CanUse(out act, usedUp: true))
            {
                return true;
            }
        }
        //if (CombatElapsedLessGCD(1) && TheForbiddenChakraPvE.CanUse(out act)) return true; // if it weaves one day in the future...

        if (HasBrotherhood && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (Player.WillStatusEnd(5, true, StatusID.EarthsRumination) && EarthsReplyPvE.CanUse(out act))
        {
            return true;
        }

        if (!HasHostilesInRange && EnlightenmentPvE.CanUse(out act, skipAoeCheck: HowlingSingle2))
        {
            return true; // Enlightment
        }

        if (!HasHostilesInRange && HowlingFistPvE.CanUse(out act, skipAoeCheck: HowlingSingle2))
        {
            return true; // Howling Fist
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ThunderclapPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (ThunderclapPvE.CanUse(out act))
        {
            return true;
        }
        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.FeintPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (FeintPvE.CanUse(out act))
        {
            return true;
        }
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.MantraPvE)]
    protected override bool HealAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (EarthsReplyPvE.CanUse(out act))
        {
            return true;
        }

        if (MantraPvE.CanUse(out act))
        {
            return true;
        }

        return base.HealAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RiddleOfEarthPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (RiddleOfEarthPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }
        return base.DefenseSingleAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (EnableTEAChecker && Target.Name.ToString() == "Jagd Doll" && Target.GetHealthRatio() < 0.25)
        {
            return base.AttackAbility(nextGCD, out act);
        }

        if (RiddleOfFirePvE.CanUse(out _))
        {
            switch (ROFFirst)
            {
                case RiddleOfFireFirst.Brotherhood:
                default:
                    if (IsLastAbility(true, BrotherhoodPvE) && RiddleOfFirePvE.CanUse(out act))
                    {
                        return true;
                    }

                    break;

                case RiddleOfFireFirst.PerfectBalance:
                    if (IsLastAbility(true, PerfectBalancePvE) && RiddleOfFirePvE.CanUse(out act))
                    {
                        return true;
                    }

                    break;
            }
        }

        if (InBrotherhood)
        {
            // 'If you are not high enough level for brotherhood, use it.'
            if (TheForbiddenChakraPvE.CanUse(out act))
            {
                return true;
            }
            // 'If you are not high enough level for TheForbiddenChakra, use immediately at 5 chakra.'
            if (!TheForbiddenChakraPvE.Info.EnoughLevelAndQuest() && SteelPeakPvE.CanUse(out act))
            {
                return true;
            }
        }
        else
        {
            // 'If you are not in brotherhood and brotherhood is about to be available, hold for burst.'
            if (BrotherhoodPvE.Cooldown.WillHaveOneChargeGCD(1) && TheForbiddenChakraPvE.CanUse(out act))
            {
                return true;
            }
            // 'If you are not high enough level for brotherhood, use it.'
            if (TheForbiddenChakraPvE.CanUse(out act))
            {
                return true;
            }
            // 'If you are not high enough level for TheForbiddenChakra, use immediately at 5 chakra.'
            if (!TheForbiddenChakraPvE.Info.EnoughLevelAndQuest() && SteelPeakPvE.CanUse(out act))
            {
                return true;
            }
        }
        if (!BrotherhoodPvE.EnoughLevel)
        {
            // 'If you are not high enough level for brotherhood, use it.'
            if (TheForbiddenChakraPvE.CanUse(out act))
            {
                return true;
            }
            // 'If you are not high enough level for TheForbiddenChakra, use immediately at 5 chakra.'
            if (!TheForbiddenChakraPvE.Info.EnoughLevelAndQuest() && SteelPeakPvE.CanUse(out act))
            {
                return true;
            }
        }

        // use bh when bh and rof are ready (opener) or ask bh to wait for rof's cd to be close and then use bh
        if (!CombatElapsedLessGCD(2)
            && ((!BrotherhoodPvE.Cooldown.IsCoolingDown && !RiddleOfFirePvE.Cooldown.IsCoolingDown) || Math.Abs(BrotherhoodPvE.Cooldown.CoolDownGroup - RiddleOfFirePvE.Cooldown.CoolDownGroup) < 3)
            && BrotherhoodPvE.CanUse(out act))
        {
            return true;
        }

        // rof needs to be used on cd or after x gcd in opener
        if (!CombatElapsedLessGCD(3) && RiddleOfFirePvE.CanUse(out act))
        {
            return true; // Riddle Of Fire
        }
        // 'Use on cooldown, unless you know your killtime. You should aim to get as many casts of RoW as you can, and then shift those usages to align with burst as much as possible without losing a use.'
        if (!CombatElapsedLessGCD(3) && RiddleOfWindPvE.CanUse(out act))
        {
            return true; // Riddle Of Wind
        }

        if (EnlightenmentPvE.CanUse(out act, skipAoeCheck: HowlingSingle2))
        {
            return true;
        }

        if (HowlingFistPvE.CanUse(out act, skipAoeCheck: HowlingSingle2))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    // 'More opos in the fight is better than... in lunar PBs'
    private bool OpoOpoForm(out IAction? act)
    {
        if (ArmOfTheDestroyerPvE.CanUse(out act, skipComboCheck: true))
        {
            return true; // Arm Of The Destoryer - aoe
        }

        if (LeapingOpoPvE.EnoughLevel)
        {
            if (LeapingOpoPvE.CanUse(out act, skipComboCheck: true))
            {
                return true; // Leaping Opo
            }
        }

        if (DragonKickPvE.CanUse(out act, skipComboCheck: true))
        {
            return true; // Dragon Kick
        }

        if (BootshinePvE.CanUse(out act, skipComboCheck: true))
        {
            return true; //Bootshine - low level
        }

        return false;
    }

    private bool RaptorForm(out IAction? act)
    {
        if (FourpointFuryPvE.CanUse(out act, skipComboCheck: true))
        {
            return true; //Fourpoint Fury - aoe
        }

        if (RisingRaptorPvE.EnoughLevel)
        {
            if (RisingRaptorPvE.CanUse(out act, skipComboCheck: true))
            {
                return true; //Rising Raptor
            }
        }

        if (TwinSnakesPvE.CanUse(out act, skipComboCheck: true))
        {
            return true; //Twin Snakes
        }

        if (TrueStrikePvE.CanUse(out act, skipComboCheck: true))
        {
            return true; //True Strike - low level
        }

        return false;
    }

    private bool CoerlForm(out IAction? act)
    {
        if (RockbreakerPvE.CanUse(out act, skipComboCheck: true))
        {
            return true; // Rockbreaker - aoe
        }

        if (PouncingCoeurlPvE.EnoughLevel)
        {
            if (PouncingCoeurlPvE.CanUse(out act, skipComboCheck: true))
            {
                return true; // Pouncing Coeurl
            }
        }

        if (DemolishPvE.CanUse(out act, skipComboCheck: true))
        {
            return true; // Demolish
        }

        if (SnapPunchPvE.CanUse(out act, skipComboCheck: true))
        {
            return true; // Snap Punch - low level
        }

        return false;
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        if (EnableTEAChecker && Target.Name.ToString() == "Jagd Doll" && Target.GetHealthRatio() < 0.25)
        {
            return base.GeneralGCD(out act);
        }

        if (!BeastChakrasContains(BeastChakra.None))
        {
            switch (MBAbilities)
            {
                case MasterfulBlitzUse.UseAsAble:
                default:
                    if (PhantomRushPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (TornadoKickPvE.CanUse(out act))
                    {
                        return true;
                    }

                    // Needing Solar Nadi and has 3 different beasts
                    if (RisingPhoenixPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (FlintStrikePvE.CanUse(out act))
                    {
                        return true;
                    }

                    // Needing Lunar Nadi and has 3 of the same beasts
                    if (ElixirBurstPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (ElixirFieldPvE.CanUse(out act))
                    {
                        return true;
                    }

                    // No Nadi and 3 beasts
                    if (CelestialRevolutionPvE.CanUse(out act))
                    {
                        return true;
                    }

                    break;

                case MasterfulBlitzUse.RiddleOfFireUse:
                    if (HasRiddleOfFire || RiddleOfFirePvE.Cooldown.JustUsedAfter(42))
                    {
                        // Both Nadi and 3 beasts
                        if (PhantomRushPvE.CanUse(out act))
                        {
                            return true;
                        }

                        if (TornadoKickPvE.CanUse(out act))
                        {
                            return true;
                        }

                        // Needing Solar Nadi and has 3 different beasts
                        if (RisingPhoenixPvE.CanUse(out act))
                        {
                            return true;
                        }

                        if (FlintStrikePvE.CanUse(out act))
                        {
                            return true;
                        }

                        // Needing Lunar Nadi and has 3 of the same beasts
                        if (ElixirBurstPvE.CanUse(out act))
                        {
                            return true;
                        }

                        if (ElixirFieldPvE.CanUse(out act))
                        {
                            return true;
                        }

                        // No Nadi and 3 beasts
                        if (CelestialRevolutionPvE.CanUse(out act))
                        {
                            return true;
                        }
                    }
                    break;
            }
        }

        // 'Because Fire¡¯s Reply grants formless, we have an imposed restriction that we prefer not to use it while under PB, or if we have a formless already.' + 'Cast Fire's Reply after an opo gcd'
        // need to test and see if IsLastGCD(false, ...) is better
        if (((!HasPerfectBalance && !HasFormlessFist && IsLastGCD(true, DragonKickPvE, LeapingOpoPvE, BootshinePvE)) || Player.WillStatusEnd(5, true, StatusID.FiresRumination)) && FiresReplyPvE.CanUse(out act))
        {
            return true; // Fires Reply
        }
        // 'Cast Wind's Reply literally anywhere in the window'
        if ((!HasPerfectBalance || Player.WillStatusEnd(5, true, StatusID.WindsRumination)) && WindsReplyPvE.CanUse(out act))
        {
            return true; // Winds Reply
        }

        // Opo needs to follow each PB
        // 'This means ¡°bookending¡± any PB usage with opos and spending formless on opos.'
        if (HasFormlessFist && OpoOpoForm(out act))
        {
            return true;
        }
        //if (Player.StatusStack(true, StatusID.PerfectBalance) == 3 && OpoOpoForm(out act)) return true;

        // Gain Solar Nadi through 3 different forms
        if (HasPerfectBalance && !HasSolar && EnhancedPerfectBalanceTrait.EnoughLevel)
        {
            if (!BeastChakrasContains(BeastChakra.Raptor) && RaptorForm(out act))
            {
                return true;
            }

            if (!BeastChakrasContains(BeastChakra.Coeurl) && CoerlForm(out act))
            {
                return true;
            }

            if (!BeastChakrasContains(BeastChakra.OpoOpo) && OpoOpoForm(out act))
            {
                return true;
            }
        }

        // Gain Lunar Nadi through 3 opopo form actions
        if (HasPerfectBalance && HasSolar && EnhancedPerfectBalanceTrait.EnoughLevel)
        {
            if (OpoOpoForm(out act))
            {
                return true;
            }
        }

        // only allow free usage of forms if you dont have perfect balance/it was not the last ability used
        if ((!HasPerfectBalance && !IsLastAction(true, PerfectBalancePvE) && EnhancedPerfectBalanceTrait.EnoughLevel) || !EnhancedPerfectBalanceTrait.EnoughLevel)
        {
            // whatever you have, press it from left to right
            if (CoerlForm(out act))
            {
                return true;
            }

            if (RaptorForm(out act))
            {
                return true;
            }

            if (OpoOpoForm(out act))
            {
                return true;
            }
        }

        // out of range or nothing to do, recharge chakra first
        if (!HasHostilesInRange)
        {
            if (EnlightenedMeditationPvE.CanUse(out act))
            {
                return true;
            }
            if (ForbiddenMeditationPvE.CanUse(out act))
            {
                return true;
            }
            if (InspiritedMeditationPvE.CanUse(out act))
            {
                return true;
            }
            if (!ForbiddenMeditationPvE.Info.EnoughLevelAndQuest() && SteeledMeditationPvE.CanUse(out act))
            {
                return true;
            }
        }

        //// out of range or nothing to do, refresh buff second, but dont keep refreshing or it draws too much attention
        //if (AutoFormShift && !HasPerfectBalance && !HasFormlessFist && FormShiftPvE.CanUse(out act))
        //{
        //    return true; // Form Shift GCD use
        //}

        return base.GeneralGCD(out act);
    }
    #endregion
}
