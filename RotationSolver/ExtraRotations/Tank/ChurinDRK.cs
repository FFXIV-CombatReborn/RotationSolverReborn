using System.ComponentModel;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace RotationSolver.ExtraRotations.Tank;

[Rotation("ChurinDRK", CombatType.PvE, GameVersion = "7.4", Description = "Find it in your heart. You'll need to break past the ribs and then scoop it out, but it's in there, and you need to find it. Quickly.")]
[SourceCode(Path = "main/ExtraRotations/Tank/ChurinDRK.cs")]
[ExtraRotation]
public sealed class ChurinDRK : DarkKnightRotation
{
    #region Properties
    private static bool HasDisesteem => StatusHelper.PlayerHasStatus(true, StatusID.Scorn);
    private bool CanBurst => MergedStatus.HasFlag(AutoStatus.Burst) && LivingShadowPvE.IsEnabled;
    private bool InBurstWindow => LivingShadowPvE.EnoughLevel && ShadowTime is > 2.5f and <= 20f || !LivingShadowPvE.EnoughLevel || HasBuffs;
    private static bool InOddWindow(IBaseAction action) => action.Cooldown.IsCoolingDown && action.Cooldown.ElapsedAfter(30f) && !action.Cooldown.ElapsedAfter(90f);
    private static bool IsMedicated => StatusHelper.PlayerHasStatus(true, StatusID.Medicated) && !StatusHelper.PlayerWillStatusEnd(0, true, StatusID.Medicated);
    private static bool NoCombo => !IsLastGCD(ActionID.HardSlashPvE, ActionID.SyphonStrikePvE);

    #region Enums
    private enum MpStrategy
    {
        [Description("Optimal")] Optimal,
        [Description("Auto at 3000+ MP")] Auto3K,
        [Description("Auto at 6000+ MP")] Auto6K,
        [Description("Auto at 9000+ MP")] Auto9K,
        [Description("Auto when about to cap")] AutoRefresh,
        [Description("Force Edge of Darkness")] ForceEdge,
        [Description("Force Flood of Darkness")] ForceFlood
    }

    private enum BloodStrategy
    {
        [Description("Automatic")] Automatic,
        [Description("Use ASAP")] Asap,
        [Description("Conserve for burst")] Conserve,
        [Description("Only Bloodspiller")] OnlyBloodspiller,
        [Description("Only Quietus")] OnlyQuietus
    }

    #endregion
    
    #region Tracking Properties
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"HasDisesteem: {HasDisesteem}");
        ImGui.Text($"CanBurst: {CanBurst}");
        ImGui.Text($"InBurstWindow: {InBurstWindow}");
        ImGui.Text($"InOddWindow: {InOddWindow(LivingShadowPvE)}");
        ImGui.Text($"IsMedicated: {IsMedicated}");
        ImGui.Text($"NoCombo: {NoCombo}");
        ImGui.Text($"Delirium Stacks: {DeliriumStacks}");
        ImGui.Text($"Next Potion Time: {_churinPotions.NextPotionTime}");
        ImGui.Text($"IsInHighEndDuty: {IsInHighEndDuty}");
    }
    #endregion

    #endregion

    #region Config Options

    [RotationConfig(CombatType.PvE, Name = "MP Spending Strategy")]
    private MpStrategy MpSpendingStrategy { get; set; } = MpStrategy.Optimal;

    [RotationConfig(CombatType.PvE, Name = "Blood Gauge Strategy")]
    private BloodStrategy BloodSpendingStrategy { get; set; } = BloodStrategy.Automatic;

    [RotationConfig(CombatType.PvE, Name = "Use The Blackest Night on lowest HP party member during AOE scenarios")]
    private bool BlackLantern { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Shadowstride in countdown")]
    private bool Facepull { get; set; } = true;

    [Range(0, 1, ConfigUnitType.Percent)]
    [RotationConfig(CombatType.PvE, Name = "Target health threshold needed to use Blackest Night with above option")]
    private float BlackLanternRatio { get; set; } = 0.5f;

    [RotationConfig(CombatType.PvE, Name = "Enable Potion Usage")]
    private static bool PotionUsageEnabled
    { get => _churinPotions.Enabled; set => _churinPotions.Enabled = value; }

    [RotationConfig(CombatType.PvE, Name = "Potion Usage Presets", Parent = nameof(PotionUsageEnabled))]
    private static PotionStrategy PotionUsagePresets
    { get => _churinPotions.Strategy; set => _churinPotions.Strategy = value; }

    [Range(0, 20, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use Opener Potion at minus time in seconds", Parent = nameof(PotionUsageEnabled))]
    private static float OpenerPotionTime { get => _churinPotions.OpenerPotionTime; set => _churinPotions.OpenerPotionTime = value; }
    
    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 1st Potion at (value in seconds - leave at 0 if using in opener)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float FirstPotionTiming
    {
        get => _firstPotionTiming;
        set
        {
            _firstPotionTiming = value;
            UpdateCustomTimings();
        }
    }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 2nd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float SecondPotionTiming
    {
        get => _secondPotionTiming;
        set
        {
            _secondPotionTiming = value;
            UpdateCustomTimings();
        }
    }

    [Range(0, 1200, ConfigUnitType.Seconds, 0)]
    [RotationConfig(CombatType.PvE, Name = "Use 3rd Potion at (value in seconds)", Parent = nameof(PotionUsagePresets), ParentValue = "Use custom potion timings")]
    private float ThirdPotionTiming
    {
        get => _thirdPotionTiming;
        set
        {
            _thirdPotionTiming = value;
            UpdateCustomTimings();
        }
    }

    #endregion

    #region Countdown Logic
    // Countdown logic to prepare for combat.
    // Includes logic for using Provoke, tank stances, and burst medicines.
    protected override IAction? CountDownAction(float remainTime)
    {
        if (_churinPotions.ShouldUsePotion(this, out var potionAct))
        {
            return potionAct;
        }
        if (!Facepull)
        {
            if (remainTime <= 3f && HasTankStance && TheBlackestNightPvE.CanUse(out var act)
                || remainTime <= 0.98f && CurrentTarget?.DistanceToPlayer() > 3f && UnmendPvE.CanUse(out act)
                || remainTime <= 0.58f && CurrentTarget?.DistanceToPlayer() <= 3f && HardSlashPvE.CanUse(out act))
            {
                return act;
            }
        }
        else
        {
            if (remainTime <= 0.7f && ShadowstridePvE.CanUse(out var act))
            {
                return act;
            }
            if (remainTime <= 0.58f && HardSlashPvE.CanUse(out act))
            {
                return act;
            }
        }
        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    [RotationDesc(ActionID.ShadowstridePvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        return ShadowstridePvE.CanUse(out act) || base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.DarkMissionaryPvE, ActionID.ReprisalPvE)]
    protected override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        switch (InBurstWindow)
        {
            case false when DarkMissionaryPvE.CanUse(out act):
            case false when ReprisalPvE.CanUse(out act, skipAoeCheck: true):
            case false when ShouldUseMp(MpSpendingStrategy) && BlackLantern && TheBlackestNightPvE.CanUse(out act) && TheBlackestNightPvE.Target.Target == LowestHealthPartyMember &&
                            TheBlackestNightPvE.Target.Target.GetHealthRatio() <= BlackLanternRatio:
                return true;
            default:
                return base.DefenseAreaAbility(nextGCD, out act);
        }
    }

    [RotationDesc(ActionID.OblationPvE, ActionID.TheBlackestNightPvE, ActionID.DarkMindPvE, ActionID.ShadowWallPvE, ActionID.ShadowedVigilPvE, ActionID.RampartPvE, ActionID.ReprisalPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        //10
        if (OblationPvE.CanUse(out act, usedUp: true, skipStatusProvideCheck: false))
        {
            return true;
        }

        if (ShouldUseMp(MpSpendingStrategy) && TheBlackestNightPvE.CanUse(out act))
        {
            return true;
        }

        if (ShouldUseMp(MpSpendingStrategy) &&
            TheBlackestNightPvE.CanUse(out act) && TheBlackestNightPvE.Target.Target == Player)
        {
            return true;
        }
        //20
        if (DarkMindPvE.CanUse(out act))
        {
            return true;
        }

        //30
        if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowWallPvE.CanUse(out act))
        {
            return true;
        }

        if ((!RampartPvE.Cooldown.IsCoolingDown || RampartPvE.Cooldown.ElapsedAfter(60)) && ShadowedVigilPvE.CanUse(out act))
        {
            return true;
        }

        //20
        if (ShadowWallPvE.Cooldown.IsCoolingDown && ShadowWallPvE.Cooldown.ElapsedAfter(60) && RampartPvE.CanUse(out act))
        {
            return true;
        }

        if (ShadowedVigilPvE.Cooldown.IsCoolingDown && ShadowedVigilPvE.Cooldown.ElapsedAfter(60) && RampartPvE.CanUse(out act))
        {
            return true;
        }

        return ReprisalPvE.CanUse(out act) || base.DefenseSingleAbility(nextGCD, out act);
    }
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        return _churinPotions.ShouldUsePotion(this, out act)
        || TryUseLivingShadow(out act)
        || TryUseDelirium(out act)
        || TryUseCarveAndSpit(out act)
        || base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        return  TryUseSaltedEarth(out act) 
        || TryUseShadowbringer(out act) 
        || TryUseEdgeOfShadow(out act) 
        || base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        return TryUseDisesteem(out act) ||
               TryUseBlood(out act) ||
               TryUseDeliriumCombo(out act) ||
               TryUseFiller(out act) ||
               base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    
    #region GCD Skills
    private bool TryUseDisesteem(out IAction? act)
    {
        act = null;
        if (!HasDisesteem) return false;

        if (InBurstWindow)
        {
            if ((NoCombo && IsLastGCD(ActionID.SouleaterPvE)) || (NoCombo && HasDelirium && CurrentMp >= 9600) || DisesteemPvE.Target.Target.DistanceToPlayer() > 3f)
            {
                return DisesteemPvE.CanUse(out act);
            }
        }

        return false;
    }
    private bool TryUseBlood(out IAction? act)
    {
        act = null;
        if (HasDelirium || Blood < 50) return false;

        if (ShouldUseBlood(BloodSpendingStrategy, CurrentTarget))
        {
            return BloodSpendingStrategy switch
            {
                BloodStrategy.Automatic => QuietusPvE.CanUse(out act) || BloodspillerPvE.CanUse(out act),
                BloodStrategy.Asap => QuietusPvE.CanUse(out act) || BloodspillerPvE.CanUse(out act),
                BloodStrategy.Conserve => QuietusPvE.CanUse(out act) || BloodspillerPvE.CanUse(out act),
                BloodStrategy.OnlyBloodspiller => BloodspillerPvE.CanUse(out act),
                BloodStrategy.OnlyQuietus => QuietusPvE.CanUse(out act),
                _ => false
            };
        }
        return false;
    }
    private bool TryUseDeliriumCombo(out IAction? act)
    {
        act = null;
        if ((CurrentMp >= 9600 || !HasDelirium) && DeliriumPvE.EnoughLevel) return false;

        if (!DeliriumPvE.EnoughLevel && BloodWeaponPvE.EnoughLevel)
        {
            if (BloodWeaponStacks > 0 && BloodspillerPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (HasDelirium)
        {
            if (CombatElapsedLessGCD(3) && NoCombo)
            {
                return ImpalementPvE.CanUse(out act) ||
                       TorcleaverPvE.CanUse(out act, skipComboCheck: true) ||
                       ComeuppancePvE.CanUse(out act, skipComboCheck: true) ||
                       ScarletDeliriumPvE.CanUse(out act, skipComboCheck: true);
            }
            if (!CombatElapsedLessGCD(3))
            {
                return ImpalementPvE.CanUse(out act) ||
                TorcleaverPvE.CanUse(out act, skipComboCheck: true) ||
                ComeuppancePvE.CanUse(out act, skipComboCheck: true) ||
                ScarletDeliriumPvE.CanUse(out act, skipComboCheck: true);
            }
        }

        return false;
    }
    private bool TryUseFiller(out IAction? act)
    {
        act = null;
        if (HasDelirium && CurrentMp < 9600 && !CombatElapsedLessGCD(3)) return false;

        return StalwartSoulPvE.CanUse(out act) ||
               UnleashPvE.CanUse(out act) ||
               SouleaterPvE.CanUse(out act) ||
               SyphonStrikePvE.CanUse(out act) ||
               HardSlashPvE.CanUse(out act) ||
               UnmendPvE.CanUse(out act);
    }

    #endregion
    
    #region oGCD Skills
    private bool TryUseEdgeOfShadow(out IAction? act)
    {
        act = null;
        if (ShouldUseMp(MpSpendingStrategy))
        {
            return FloodOfDarknessPvE.CanUse(out act) ||
                   EdgeOfDarknessPvE.CanUse(out act);
        }
        return false;
    }
    private bool TryUseLivingShadow(out IAction? act)
    {
        act = null;
        if (!CanBurst) return false;

        if (InCombat && DarkSideTime > 0 && !CombatElapsedLessGCD(1))
        {
            return LivingShadowPvE.CanUse(out act);
        }
        return false;
    }
    private bool TryUseShadowbringer(out IAction? act)
    {
        act = null;
        if (!ShadowbringerPvE.EnoughLevel || ShadowbringerPvE.Cooldown.CurrentCharges == 0)
        {
            return false;
        }

        if (InBurstWindow && ShadowbringerPvE.Cooldown.CurrentCharges == 2 && CurrentMp < 5000)
        {
            return ShadowbringerPvE.CanUse(out act, skipAoeCheck: true, usedUp: false);
        }
        if (InBurstWindow && ShadowbringerPvE.Cooldown.CurrentCharges > 0 && CurrentMp < 3000)
        {
            return ShadowbringerPvE.CanUse(out act, skipAoeCheck: true, usedUp: true);
        }
        if (ShadowbringerPvE.Cooldown.CurrentCharges == 1 && ShadowbringerPvE.Cooldown.WillHaveXChargesGCD(2, 1))
        {
            return ShadowbringerPvE.CanUse(out act, usedUp: true, skipAoeCheck: true);
        }
        return false;
    }
    private bool TryUseSaltedEarth(out IAction? act)
    {
        act = null;
        if (!SaltedEarthPvE.EnoughLevel || !SaltedEarthPvE.IsEnabled)
            return false;

        bool hasSaltedEarth = StatusHelper.PlayerHasStatus(true, StatusID.SaltedEarth);
        bool canUseSaltAndDarkness = SaltAndDarknessPvE.EnoughLevel && hasSaltedEarth;

        if (InBurstWindow && canUseSaltAndDarkness && CurrentMp < 6000) return SaltAndDarknessPvE.CanUse(out act);
        if (!InBurstWindow && canUseSaltAndDarkness) return SaltAndDarknessPvE.CanUse(out act);

        if (InBurstWindow) return (IsInHighEndDuty || !IsMoving) && SaltedEarthPvE.CanUse(out act);
#if DEBUG
        if (CurrentTarget != null && ObjectHelper.IsDummy(CurrentTarget))
        {
            if (_churinPotions.Enabled && _churinPotions.NextPotionTime <= 90)
            {
                return false;
            }
            if (!_churinPotions.Enabled && CombatElapsedLessGCD(2))
            {
                return false;
            }
            return !CombatElapsedLessGCD(2) && SaltedEarthPvE.CanUse(out act);
        }
#endif
        // Outside burst window
        if (IsInHighEndDuty)
        {
            if (_churinPotions.Enabled && _churinPotions.NextPotionTime <= 60f)
            {
                return false;
            }
            if (!_churinPotions.Enabled && CombatElapsedLessGCD(2))
            {
                return false;
            }
        }
        else
        {
            if (IsMoving || CombatElapsedLessGCD(3))
            {
                return false;
            }
        }

        return SaltedEarthPvE.CanUse(out act);
    }
    private bool TryUseDelirium (out IAction? act)
    {
        act = null;
        if (!BloodWeaponPvE.EnoughLevel && DeliriumPvE.EnoughLevel 
        || !DeliriumPvE.IsEnabled 
        || !BloodWeaponPvE.IsEnabled 
        || !CanBurst
        || Blood > 70)
        {
            return false;
        }

        if (!DeliriumPvE.EnoughLevel && BloodWeaponPvE.EnoughLevel && BloodWeaponPvE.CanUse(out act))
        {
            return true;
        }

        return !CombatElapsedLessGCD(1) && DeliriumPvE.CanUse(out act);
    }
    private bool TryUseCarveAndSpit(out IAction? act)
    {
        act = null;
        if (InBurstWindow && CurrentMp < 9000
        || !InBurstWindow && DeliriumPvE.Cooldown.IsCoolingDown && !DeliriumPvE.Cooldown.WillHaveOneCharge(20))
        {
            return AbyssalDrainPvE.CanUse(out act) ||
                   CarveAndSpitPvE.CanUse(out act);
        }

        return false;
    }

    #endregion
    
    #region Miscellaneous Methods

    #region Potions
    private static readonly ChurinDRKPotions _churinPotions = new();
    private float _firstPotionTiming = 0;
    private float _secondPotionTiming = 0;
    private float _thirdPotionTiming = 0;

    /// <summary>
    /// DRK-specific potion manager that extends base potion logic with job-specific conditions.
    /// </summary>
    private class ChurinDRKPotions : Potions
    {
        public float NextPotionTime
        {
            get
            {
                if (!Enabled) return 0;

                float[] timings = GetTimingsArray();
                float difference;

                for (int i = 0; i < timings.Length; i++)
                {
                    if (timings[i] > 0)
                    {
                        difference = timings[i] - DataCenter.CombatTimeRaw;
                        if (difference > 0)
                        {
                            return difference;
                        }
                    }
                }
                return 0;
            }
        }
        
        public override bool IsConditionMet()
        {
            float[] timings = GetTimingsArray();

            foreach (float timing in timings)
            {
                if (IsOpenerPotion(timing) && ChurinDRK.OpenerPotionTime == 0)
                {
                    if (WeaponElapsed > 0 && DarkSideTime <= 0)
                    {
                        return true;
                    }
                }
                else
                {
                    if (IsTimingValid(timing))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected override bool IsTimingValid(float timing)
        {
            if (timing > 0 && (DataCenter.CombatTimeRaw - timing) >= -5f && (DataCenter.CombatTimeRaw - timing) < 5f)
            {
                return IsLastGCD(ActionID.HardSlashPvE);
            }

            // Check opener timing: if it's an opener potion and countdown is within configured time
            float countDown = Service.CountDownTime;
            if (IsOpenerPotion(timing) && countDown > 0 && countDown <= ChurinDRK.OpenerPotionTime && ChurinDRK.OpenerPotionTime > 0)
            {
                return true;
            }

            if (IsOpenerPotion(timing) && ChurinDRK.OpenerPotionTime == 0 && CombatElapsedLessGCD(5))
            {
                return true;
            }

            return false;
        }
    }

    private void UpdateCustomTimings()
    {
        _churinPotions.CustomTimings = new Potions.CustomTimingsData
        {
            Timings = [FirstPotionTiming, SecondPotionTiming, ThirdPotionTiming]
        };
    }
    #endregion

    private bool ShouldUseMp(MpStrategy strategy)
    {
        var riskingMp = CurrentMp >= 8500;

        if (!FloodOfShadowPvE.EnoughLevel)
            return false;

        if (strategy == MpStrategy.Optimal)
        {
            if (riskingMp)
                return CurrentMp >= 3000 || HasDarkArts;

            if (DeliriumPvE.EnoughLevel)
            {
                // For Dark Arts
                if (TheBlackestNightPvE.EnoughLevel && HasDarkArts)
                {
                    if (HasDelirium || !DeliriumPvE.Cooldown.WillHaveOneCharge(DarkSideTime + WeaponTotal))
                        return CurrentMp >= 3000;
                }

                // 1m window - 2 uses expected
                if (!DeliriumPvE.Cooldown.WillHaveOneCharge(40f) && InOddWindow(LivingShadowPvE))
                    return CurrentMp >= 6000;

                // 2m window - 4 uses expected; 5 with Dark Arts
                if (InBurstWindow || !DeliriumPvE.Cooldown.WillHaveOneCharge(40f) && !InOddWindow(LivingShadowPvE))
                    return CurrentMp >= 3000 || HasDarkArts;
            }

            // If no Delirium, just use it whenever we have more than 3000 MP
            if (!DeliriumPvE.EnoughLevel)
                return CurrentMp >= 3000;
        }

        return strategy switch
        {
            MpStrategy.Auto3K => CurrentMp >= 3000,
            MpStrategy.Auto6K => CurrentMp >= 6000,
            MpStrategy.Auto9K => CurrentMp >= 9000,
            MpStrategy.AutoRefresh => riskingMp,
            MpStrategy.ForceEdge => EdgeOfDarknessPvE.EnoughLevel && (CurrentMp >= 3000 || HasDarkArts),
            MpStrategy.ForceFlood => FloodOfDarknessPvE.EnoughLevel && (CurrentMp >= 3000 || HasDarkArts),
            _ => false
        };
    }

    private bool ShouldUseBlood(BloodStrategy strategy, IBattleChara? target)
    {
        var riskingBlood = Blood >= 90;
        var minimum = (BloodspillerPvE.EnoughLevel || QuietusPvE.EnoughLevel) && (Blood >= 50 || HasDelirium);
        var inMeleeRange = target?.DistanceToPlayer() <= 3;
        var combatTarget = InCombat && target != null;
        var deliriumCooldown = DeliriumPvE.EnoughLevel && DeliriumPvE.Cooldown.IsCoolingDown && !DeliriumPvE.Cooldown.WillHaveOneCharge(40f);

        // Basic condition for using blood
        var condition = combatTarget && inMeleeRange && minimum && DarkSideTime > 0 && NoCombo && (deliriumCooldown || riskingBlood || PartyBuffDuration > WeaponTotal);

        return strategy switch
        {
            BloodStrategy.Automatic => condition,
            BloodStrategy.OnlyBloodspiller => condition && target!.DistanceToPlayer() <= 3,
            BloodStrategy.OnlyQuietus => condition && NumberOfAllHostilesInRange > 2,
            BloodStrategy.Asap => minimum,
            BloodStrategy.Conserve => riskingBlood || PartyBuffDuration > WeaponTotal,
            _ => false
        };
    }

    private List<IBattleChara> _currentParty = [];

	public List<IBattleChara> CurrentParty
	{
		get => _currentParty;
		set
		{
			_currentParty = value;
			var newParty = PartyMembers ?? [Player!];
			IEnumerable<IBattleChara> battleCharas = [.. newParty];
			var hasChanged = !_currentParty.ToHashSet().SetEquals(battleCharas);

			if (hasChanged)
			{
				_currentParty.Clear();
				foreach (var member in battleCharas)
				{
					if (member != null)
					{
						_currentParty.Add(member);
					}
				}
			}

			if (_currentParty.Count == 0 && Player != null)
			{
				_currentParty.Add(Player);
			}
		}
	}


	#endregion

	#endregion
}