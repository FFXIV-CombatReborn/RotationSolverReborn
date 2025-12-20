using System.ComponentModel;

namespace RotationSolver.RebornRotations.Melee;

[Rotation("Reborn", CombatType.PvE, GameVersion = "7.4")]
[SourceCode(Path = "main/RebornRotations/Melee/SAM_Reborn.cs")]

public sealed class SAM_Reborn : SamuraiRotation
{
    #region Config Options

    public enum STtoAOEStrategy : byte
    {
        [Description("Hagakure")] Hagakure,

        [Description("Setsugekka")] Setsugekka,
    }

    [RotationConfig(CombatType.PvE, Name = "Prevent Higanbana use if theres more than one target")]
    public bool HiganbanaTargets { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Enable TEA Checker.")]
    public bool EnableTEAChecker { get; set; } = false;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Health threshold needed to use Tengentsu/ThirdEye outside of AOE mit scenarios.")]
    public float TengentsuHealth { get; set; } = 0.5f;

    [RotationConfig(CombatType.PvE, Name = "Use Hagakure or Midare/Tendo Setsugekka when going from single target to AOE scenarios")]
    public STtoAOEStrategy STtoAOE { get; set; } = STtoAOEStrategy.Hagakure;
    #endregion

    #region Countdown Logic

    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime <= 14 && MeikyoShisuiPvE.CanUse(out IAction? act))
        {
            return act;
        }

        if (remainTime <= 5 && TrueNorthPvE.CanUse(out act))
        {
            return act;
        }

        return base.CountDownAction(remainTime);
    }

    #endregion

    #region Additional oGCD Logic

    [RotationDesc(ActionID.HissatsuGyotenPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (HissatsuGyotenPvE.CanUse(out act))
        {
            return true;
        }
        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ThirdEyePvE, ActionID.TengentsuPvE, ActionID.FeintPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (!HasZanshinReady)
        {
            if (FeintPvE.CanUse(out act))
            {
                return true;
            }
            if (TengentsuPvE.CanUse(out act))
            {
                return true;
            }
            if (ThirdEyePvE.CanUse(out act))
            {
                return true;
            }
        }
        return base.DefenseAreaAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (!HasZanshinReady && Player?.GetHealthRatio() <= TengentsuHealth)
        {
            if (FeintPvE.CanUse(out act))
            {
                return true;
            }
            if (TengentsuPvE.CanUse(out act))
            {
                return true;
            }
            if (ThirdEyePvE.CanUse(out act))
            {
                return true;
            }
        }

        if ((HasOgiNamikiri || HasZanshinReady) && InCombat && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        bool MeleeMeditationcheck = nextGCD.IsTheSameTo(true, ActionID.OgiNamikiriPvE, ActionID.HiganbanaPvE, ActionID.TenkaGokenPvE, ActionID.MidareSetsugekkaPvE, ActionID.TendoGokenPvE, ActionID.TendoSetsugekkaPvE, ActionID.TendoGokenPvE);
        bool isTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
        bool isTargetDying = CurrentTarget?.IsDying() ?? false;

        if (EnableTEAChecker && Target.Name.ToString() == "Jagd Doll" && Target.GetHealthRatio() < 0.25)
        {
            return base.AttackAbility(nextGCD, out act);
        }

        if (MeikyoShisuiPvE.CanUse(out act, usedUp: !EnhancedMeikyoShisuiTrait.EnoughLevel || (EnhancedMeikyoShisuiTrait.EnoughLevel && MeikyoShisuiPvE.Cooldown.WillHaveXChargesGCD(2, 1)) || TsubamegaeshiActionReady)
            && HasHostilesInRange && (!HasFugetsuAndFuka || (isTargetBoss && isTargetDying) || (CurrentTarget?.HasStatus(true, StatusID.Higanbana) ?? false) && !(CurrentTarget?.WillStatusEndGCD(HiganbanaPvE.Config.StatusGcdCount, 0, true, StatusID.Higanbana) ?? false)))
        {
            if ((!EnhancedHissatsuTrait.EnoughLevel && SenCount == 0 && !TsubamegaeshiActionReady) || !HasFugetsuAndFuka)
            {
                return true;
            }

            if (!HasFugetsuAndFuka || TsubamegaeshiActionReady || IsLastAction(false, TendoKaeshiSetsugekkaPvE))
            {
                return true;
            }
        }

        if (ZanshinPvE.CanUse(out act))
        {
            return true;
        }

        if ((MeleeMeditationcheck || IkishotenPvE.Cooldown.RecastTimeElapsed < 30) && !HasZanshinReady && !HasOgiNamikiri)
        {
            if (ShohaPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (!HasZanshinReady)
        {
            if (!CombatElapsedLessGCD(2) && Kenki <= 50 && IkishotenPvE.CanUse(out act))
            {
                return true;
            }

            if (IkishotenPvE.Cooldown.IsCoolingDown && Kenki >= 25)
            {
                if (HissatsuGurenPvE.CanUse(out act, skipAoeCheck: !HissatsuSeneiPvE.EnoughLevel))
                {
                    return true;
                }

                if (HissatsuSeneiPvE.CanUse(out act))
                {
                    return true;
                }
            }

            if (Kenki >= 50 || (!IkishotenPvE.EnoughLevel && Kenki >= 25))
            {
                if (HissatsuKyutenPvE.CanUse(out act))
                {
                    return true;
                }

                if (HissatsuShintenPvE.CanUse(out act))
                {
                    return true;
                }
            }
        }

        return base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        bool isTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;

        if (!HiganbanaTargets || (HiganbanaTargets && NumberOfAllHostilesInRange < 2) && HasFugetsuAndFuka && !WillFugetsuEnd && !WillFukaEnd && !HasMeikyoShisui && !MidareSetsugekkaReady)
        {
            if (HiganbanaPvE.CanUse(out act, skipComboCheck: true, skipTTKCheck: isTargetBoss || IsInHighEndDuty))
            {
                return true;
            }
        }

        if (KaeshiNamikiriPvE.CanUse(out act))
        {
            return true;
        }

        if (NumberOfHostilesInRange >= 3)
        {
            switch (STtoAOE)
            {
                case STtoAOEStrategy.Hagakure:
                default:
                    if (MidareSetsugekkaPvE.CanUse(out _) && HagakurePvE.CanUse(out act))
                    {
                        return true;
                    }

                    break;

                case STtoAOEStrategy.Setsugekka:
                    if (TendoSetsugekkaPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (MidareSetsugekkaPvE.CanUse(out act))
                    {
                        return true;
                    }

                    break;
            }
        }

        if (!HagakurePvE.EnoughLevel && NumberOfHostilesInRange >= 3 && MidareSetsugekkaPvE.CanUse(out act))
        {
            return true;
        }

        if (NumberOfHostilesInRange >= 2 && OgiNamikiriPvE.CanUse(out act))
        {
            return true;
        }

        if (TendoKaeshiGokenPvE.CanUse(out act, skipAoeCheck: true))
        {
            return true;
        }

        if (TendoGokenPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        if (KaeshiGokenPvE.CanUse(out act, skipComboCheck: true, skipAoeCheck: true))
        {
            return true;
        }

        if (TenkaGokenPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        if (HasFugetsuAndFuka)
        {
            if (!OkaPvE.EnoughLevel && MangetsuPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: HasMeikyoShisui && !HasKa))
            {
                return true;
            }

            switch (FugetsuOrFukaEndsFirst)
            {
                case "Fugetsu":
                    if (MangetsuPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: HasMeikyoShisui && !HasKa))
                        return true;
                    break;
                case "Fuka":
                    if (OkaPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: HasMeikyoShisui && !HasKa))
                        return true;
                    break;
                case "Equal":
                case null:
                    if (MangetsuPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: HasMeikyoShisui && !HasKa))
                        return true;
                    if (OkaPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: HasMeikyoShisui && !HasKa))
                        return true;
                    break;
            }
        }
        if (!HasFugetsuAndFuka)
        {
            if (!OkaPvE.EnoughLevel && MangetsuPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: HasMeikyoShisui && !HasKa))
            {
                return true;
            }

            if (!HasFugetsu && MangetsuPvE.CanUse(out act))
            {
                return true;
            }

            if (!HasFuka && OkaPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (!HasMeikyoShisui)
        {
            if (FugaMasteryTrait.EnoughLevel)
            {
                if (FukoPvE.CanUse(out act))
                {
                    return true;
                }
            }
            else if (FugaPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (TendoSetsugekkaPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        if (MidareSetsugekkaPvE.CanUse(out act, skipComboCheck: true))
        {
            return true;
        }

        // use 2nd finisher combo spell first
        if (KaeshiSetsugekkaPvE.CanUse(out act))
        {
            return true;
        }

        if (TendoKaeshiSetsugekkaPvE.CanUse(out act))
        {
            return true;
        }

        if (OgiNamikiriPvE.CanUse(out act) && OgiNamikiriPvE.Target.Target != null)
        {
            if ((!isTargetBoss || (OgiNamikiriPvE.Target.Target?.HasStatus(true, StatusID.Higanbana) ?? false)) && HasFugetsuAndFuka)
            {
                return true;
            }
        }

        // single target 123 combo's 3 or used 3 directly during burst when MeikyoShisui is active, while also trying to start with the one that player is in position for extra DMG
        if (GekkoPvE.CanUse(out act, skipComboCheck: HasMeikyoShisui && !HasGetsu) && GekkoPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, GekkoPvE.Target.Target))
        {
            return true;
        }

        if (KashaPvE.CanUse(out act, skipComboCheck: HasMeikyoShisui && !HasKa) && KashaPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, KashaPvE.Target.Target))
        {
            return true;
        }

        if (!HasSetsu && HasFugetsuAndFuka &&
            YukikazePvE.CanUse(out act, skipComboCheck: HasMeikyoShisui))
        {
            return true;
        }

        if (GekkoPvE.CanUse(out act, skipComboCheck: HasMeikyoShisui && !HasGetsu))
        {
            return true;
        }

        if (KashaPvE.CanUse(out act, skipComboCheck: HasMeikyoShisui && !HasKa))
        {
            return true;
        }

        if (HasFugetsuAndFuka)
        {
            switch (FugetsuOrFukaEndsFirst)
            {
                case "Fugetsu":
                    if (JinpuPvE.CanUse(out act, skipStatusProvideCheck: true))
                        return true;
                    break;
                case "Fuka":
                    if (ShifuPvE.CanUse(out act, skipStatusProvideCheck: true))
                        return true;
                    break;
                case "Equal":
                case null:
                    if (JinpuPvE.CanUse(out act, skipStatusProvideCheck: true))
                        return true;
                    if (ShifuPvE.CanUse(out act, skipStatusProvideCheck: true))
                        return true;
                    break;
            }
        }
        if (!HasFugetsuAndFuka)
        {
            if (!HasFugetsu && JinpuPvE.CanUse(out act) && JinpuPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, JinpuPvE.Target.Target))
            {
                return true;
            }

            if (!HasFuka && ShifuPvE.CanUse(out act) && ShifuPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, ShifuPvE.Target.Target))
            {
                return true;
            }

            if (!HasFugetsu && JinpuPvE.CanUse(out act))
            {
                return true;
            }

            if (!HasFuka && ShifuPvE.CanUse(out act))
            {
                return true;
            }

            if (!HasFugetsu)
            {
                if (JinpuPvE.CanUse(out act))
                    return true;
            }

            if (!HasFuka)
            {
                if (ShifuPvE.CanUse(out act))
                    return true;
            }
        }

        if (!HasMeikyoShisui && !TsubamegaeshiActionReady)
        {
            if (HakazePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (EnpiPvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }

    #endregion
}