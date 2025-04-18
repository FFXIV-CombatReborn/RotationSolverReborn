using System.ComponentModel;

namespace RebornRotations.Melee;

[Rotation("Default", CombatType.PvE, GameVersion = "7.2")]
[SourceCode(Path = "main/BasicRotations/Melee/SAM_Default.cs")]
[Api(4)]
public sealed class SAM_Default : SamuraiRotation
{
    #region Config Options

    public enum STtoAOEStrategy : byte
    {
        [Description("Hagakure")] Hagakure,

        [Description("Setsugekka")] Setsugekka,
    }

    [Range(0, 20, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "When during countdown to use Meikyo Shisui")]
    public int MeikyoCD { get; set; } = 5;

    [Range(0, 100, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Kenki needed to use Shinten")]
    public int ShintenKenki { get; set; } = 75;

    [Range(0, 100, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Kenki needed to use Kyuten")]
    public int KyutenKenki { get; set; } = 75;

    [Range(0, 100, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Kenki needed to use Senei")]
    public int SeneiKenki { get; set; } = 25;

    [Range(0, 100, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Kenki needed to use Guren")]
    public int GurenKenki { get; set; } = 25;

    [Range(0, 100, ConfigUnitType.None, 1)]
    [RotationConfig(CombatType.PvE, Name = "Kenki needed to use Zanshin")]
    public int ZanshinKenki { get; set; } = 50;

    [RotationConfig(CombatType.PvE, Name = "Prioritize Zanshin use over other Kenki abilties when available.")]
    public bool ZanshinPrio { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Prevent Higanbana use if theres more than one target")]
    public bool HiganbanaTargets { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Enable TEA Checker.")]
    public bool EnableTEAChecker { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Hagakure or Midare/Tendo Setsugekka when going from single target to AOE scenarios")]
    public STtoAOEStrategy STtoAOE { get; set; } = STtoAOEStrategy.Hagakure;
    #endregion

    #region Countdown Logic

    protected override IAction? CountDownAction(float remainTime)
    {
        // pre-pull: can be changed to -9 and -5 instead of 5 and 2, but it's hard to be universal !!! check later !!!
        if (remainTime <= MeikyoCD && MeikyoShisuiPvE.CanUse(out var act)) return act;
        if (remainTime <= 2 && TrueNorthPvE.CanUse(out act)) return act;
        return base.CountDownAction(remainTime);
    }

    #endregion

    #region Additional oGCD Logic

    [RotationDesc(ActionID.HissatsuGyotenPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (HissatsuGyotenPvE.CanUse(out act)) return true;
        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.FeintPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (FeintPvE.CanUse(out act)) return true;
        return base.DefenseAreaAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ThirdEyePvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (TengentsuPvE.CanUse(out act)) return true;
        if (ThirdEyePvE.CanUse(out act)) return true;
        return base.DefenseSingleAbility(nextGCD, out act);
    }

    #endregion

    #region oGCD Logic

    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (EnableTEAChecker && Target.Name.ToString() == "Jagd Doll" && Target.GetHealthRatio() < 0.25)
        {
            return false;
        }

        var isTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
        var isTargetDying = CurrentTarget?.IsDying() ?? false;

        // from old version - didn't touch this, didn't test this, personally i doubt it's working !!! check later !!!
        if (HasHostilesInRange && IsLastGCD(true, YukikazePvE, MangetsuPvE, OkaPvE) &&
            (!isTargetBoss || (CurrentTarget?.HasStatus(true, StatusID.Higanbana) ?? false) && !(CurrentTarget?.WillStatusEnd(40, true, StatusID.Higanbana) ?? false) || !HasMoon && !HasFlower || isTargetBoss && isTargetDying))
        {
            if (MeikyoShisuiPvE.CanUse(out act, usedUp: true)) return true;
        }
        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        act = null;
        if (EnableTEAChecker && Target.Name.ToString() == "Jagd Doll" && Target.GetHealthRatio() < 0.25)
        {
            return false;
        }

        var isTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
        var isTargetDying = CurrentTarget?.IsDying() ?? false;

        // IkishotenPvE logic combined with the delayed opener:
        // you should weave the tincture in manually after rsr lands the first gcd (usually Gekko)
        // and that's the only chance for tincture weaving during opener
        if (!CombatElapsedLessGCD(2) && IkishotenPvE.CanUse(out act)) return true;
        if (ShohaPvE.CanUse(out act)) return true;
        // from old version - didn't touch this, didn't test this, never saw Hagakure button pressed personally !!! check later !!!
        if ((CurrentTarget?.HasStatus(true, StatusID.Higanbana) ?? false) &&
            (CurrentTarget?.WillStatusEnd(32, true, StatusID.Higanbana) ?? false) &&
            !(CurrentTarget?.WillStatusEnd(28, true, StatusID.Higanbana) ?? false) &&
            SenCount == 1 && IsLastAction(true, YukikazePvE) && !HaveMeikyoShisui)
        {
            if (HagakurePvE.CanUse(out act)) return true;
        }

        if (Kenki >= GurenKenki && ZanshinPvE.CanUse(out act)) return true;

        // ensures pooling Kenki for Zanshin if it's available
        bool hasZanshinReady = Player.HasStatus(true, StatusID.ZanshinReady_3855) && ZanshinPrio;

        if (!hasZanshinReady && Kenki >= GurenKenki && HissatsuGurenPvE.CanUse(out act, skipAoeCheck: !HissatsuSeneiPvE.EnoughLevel)) return true;
        if (!hasZanshinReady && Kenki >= SeneiKenki && HissatsuSeneiPvE.CanUse(out act)) return true;
        if (!hasZanshinReady && Kenki >= KyutenKenki && HissatsuKyutenPvE.CanUse(out act)) return true;
        if (!hasZanshinReady && Kenki >= ShintenKenki && HissatsuShintenPvE.CanUse(out act)) return true;

        return base.AttackAbility(nextGCD, out act);
    }

    #endregion

    #region GCD Logic

    StatusID[] SamBuffs = { StatusID.Fugetsu, StatusID.Fuka };

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;
        var isTargetBoss = CurrentTarget?.IsBossFromTTK() ?? false;
        var isTargetDying = CurrentTarget?.IsDying() ?? false;

        if (EnableTEAChecker && Target.Name.ToString() == "Jagd Doll" && Target.GetHealthRatio() < 0.25)
        {
            return false;
        }

        if ((!HiganbanaTargets || (HiganbanaTargets && NumberOfAllHostilesInRange < 2)) && (HostileTarget?.WillStatusEnd(18, true, StatusID.Higanbana) ?? false) && HiganbanaPvE.CanUse(out act, skipStatusProvideCheck: true, skipComboCheck: true)) return true;
        if (KaeshiNamikiriPvE.CanUse(out act)) return true;

        if (NumberOfHostilesInRange >= 3)
        {
            switch (STtoAOE)
            {
                case STtoAOEStrategy.Hagakure:
                default:
                    if (MidareSetsugekkaPvE.CanUse(out _) && HagakurePvE.CanUse(out act)) return true;
                    break;

                case STtoAOEStrategy.Setsugekka:
                    if (TendoSetsugekkaPvE.CanUse(out act)) return true;
                    if (MidareSetsugekkaPvE.CanUse(out act)) return true;
                    break;
            }
        }

        if (!HagakurePvE.EnoughLevel && NumberOfHostilesInRange >= 3 && MidareSetsugekkaPvE.CanUse(out act)) return true;

        if (TendoKaeshiGokenPvE.CanUse(out act)) return true;
        if (TendoGokenPvE.CanUse(out act, skipComboCheck: true)) return true;
        if (KaeshiGokenPvE.CanUse(out act, skipComboCheck: true)) return true;
        if (TenkaGokenPvE.CanUse(out act, skipComboCheck: true)) return true;

        // aoe 12 combo's 2
        if ((!HasMoon || IsMoonTimeLessThanFlower || !OkaPvE.EnoughLevel) && MangetsuPvE.CanUse(out act, skipComboCheck: HaveMeikyoShisui && !HasGetsu)) return true;
        if ((!HasFlower || !IsMoonTimeLessThanFlower) && OkaPvE.CanUse(out act, skipComboCheck: HaveMeikyoShisui && !HasKa)) return true;

        // initiate aoe
        if (FukoPvE.CanUse(out act)) return true;
        if (!FukoPvE.EnoughLevel && FugaPvE.CanUse(out act)) return true;

        if (TendoSetsugekkaPvE.CanUse(out act, skipComboCheck: true)) return true;
        if (MidareSetsugekkaPvE.CanUse(out act, skipComboCheck: true)) return true;

        // use 2nd finisher combo spell first
        if (!KaeshiNamikiriReady && KaeshiSetsugekkaPvE.CanUse(out act, usedUp: true)) return true;
        if (!KaeshiNamikiriReady && TendoKaeshiSetsugekkaPvE.CanUse(out act, usedUp: true)) return true;

        // burst finisher
        if ((!isTargetBoss || (HostileTarget?.HasStatus(true, StatusID.Higanbana) ?? false)) && HasMoon && HasFlower
            && OgiNamikiriPvE.CanUse(out act)) return true;

        if (!HasSetsu && SamBuffs.All(buff => Player.HasStatus(true, buff)) &&
            YukikazePvE.CanUse(out act, skipComboCheck: HaveMeikyoShisui && HasGetsu && HasKa)) return true;

        // single target 123 combo's 3 or used 3 directly during burst when MeikyoShisui is active, while also trying to start with the one that player is in position for extra DMG
        if (GekkoPvE.CanUse(out act, skipComboCheck: HaveMeikyoShisui && !HasGetsu) && GekkoPvE.Target.Target != null && CanHitPositional(EnemyPositional.Rear, GekkoPvE.Target.Target)) return true;
        if (KashaPvE.CanUse(out act, skipComboCheck: HaveMeikyoShisui && !HasKa) && KashaPvE.Target.Target != null && CanHitPositional(EnemyPositional.Flank, KashaPvE.Target.Target)) return true;

        if (GekkoPvE.CanUse(out act, skipComboCheck: HaveMeikyoShisui && !HasGetsu)) return true;
        if (KashaPvE.CanUse(out act, skipComboCheck: HaveMeikyoShisui && !HasKa)) return true;

        // single target 123 combo's 2, while also trying to start with the one that player is in position for extra DMG
        if (!HasGetsu && JinpuPvE.CanUse(out act) && JinpuPvE.Target.Target != null && (CanHitPositional(EnemyPositional.Rear, JinpuPvE.Target.Target) || (!HasMoon && HasFlower))) return true;
        if (!HasKa && ShifuPvE.CanUse(out act) && ShifuPvE.Target.Target != null && (CanHitPositional(EnemyPositional.Flank, ShifuPvE.Target.Target) || (!HasFlower && HasMoon))) return true;

        if ((!HasMoon || IsMoonTimeLessThanFlower || !ShifuPvE.EnoughLevel) && JinpuPvE.CanUse(out act)) return true;
        if ((!HasFlower || !IsMoonTimeLessThanFlower) && ShifuPvE.CanUse(out act)) return true;

        // MeikyoShisui buff is not active - not bursting - single target 123 combo's 1
        if (!HaveMeikyoShisui)
        {
            // target in range
            if (HakazePvE.CanUse(out act)) return true;

            // target out of range
            if (EnpiPvE.CanUse(out act, skipComboCheck: true)) return true;
        }

        return base.GeneralGCD(out act);
    }

    #endregion
}