using Dalamud.Interface.Colors;

namespace RotationSolver.Basic.Rotations.Basic;

partial class BlackMageRotation
{
    /// <inheritdoc/>
    public override MedicineType MedicineType => MedicineType.Intelligence;

    // Umbral Soul level 35 now
    #region Job Gauge

    /// <summary>
    /// 
    /// </summary>
    public static byte UmbralIceStacks => JobGauge.UmbralIceStacks;

    /// <summary>
    /// 
    /// </summary>
    public static byte AstralFireStacks => JobGauge.AstralFireStacks;

    /// <summary>
    /// 
    /// </summary>
    public static int AstralSoulStacks => JobGauge.AstralSoulStacks;

    /// <summary>
    /// 
    /// </summary>
    public static byte PolyglotStacks => JobGauge.PolyglotStacks;

    /// <summary>
    /// 
    /// </summary>
    public static byte UmbralHearts => JobGauge.UmbralHearts;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsParadoxActive => JobGauge.IsParadoxActive;

    /// <summary>
    /// 
    /// </summary>
    public static bool InUmbralIce => JobGauge.InUmbralIce;

    /// <summary>
    /// 
    /// </summary>
    public static bool InAstralFire => JobGauge.InAstralFire;

    /// <summary>
    /// 
    /// </summary>
    public static bool IsEnochianActive => JobGauge.IsEnochianActive;

    /// <summary>
    /// 
    /// </summary>
    static float EnochianTimeRaw => JobGauge.EnochianTimer / 1000f;

    /// <summary>
    /// 
    /// </summary>
    public static float EnochianTime => EnochianTimeRaw - DataCenter.DefaultGCDRemain;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    protected static bool EnochianEndAfter(float time) => EnochianTime <= time;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="gcdCount"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    protected static bool EnochianEndAfterGCD(uint gcdCount = 0, float offset = 0)
        => EnochianEndAfter(GCDTime(gcdCount, offset));

    /// <summary>
    /// 
    /// </summary>
    protected static float Fire4Time { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    protected override void UpdateInfo()
    {
        if (Player.CastActionId == (uint)ActionID.FireIvPvE && Player.CurrentCastTime < 0.2)
        {
            Fire4Time = Player.TotalCastTime;
        }
        base.UpdateInfo();
    }

    /// <summary>
    /// Returns the higher value between Astral Fire stacks and Umbral Ice stacks.
    /// </summary>
    public static byte SoulStackCount => Math.Max(AstralFireStacks, UmbralIceStacks);

    /// <summary>
    /// A check with variable max stacks of Polyglot based on the trait level.
    /// </summary>
    public static bool IsSoulStacksMaxed
    {
        get
        {
            if (Player.Level >= 35)
            {
                return SoulStackCount == 3;
            }
            else if (Player.Level >= 20)
            {
                return SoulStackCount == 2;
            }
            else
            {
                return SoulStackCount == 1;
            }
        }
    }

    /// <summary>
    /// A check with variable max stacks of Polyglot based on the trait level.
    /// </summary>
    public static byte MaxSoulCount
    {
        get
        {
            if (Player.Level >= 35)
            {
                return 3;
            }
            else if (Player.Level >= 20)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }
    }

    /// <summary>
    /// A check with variable max stacks of Polyglot based on the trait level.
    /// </summary>
    public static bool IsPolyglotStacksMaxed
    {
        get
        {
            if (EnhancedPolyglotIiTrait.EnoughLevel)
            {
                return PolyglotStacks == 3;
            }
            else if (EnhancedPolyglotTrait.EnoughLevel)
            {
                return PolyglotStacks == 2;
            }
            else
            {
                return PolyglotStacks == 1;
            }
        }
    }
    #endregion

    #region Status Tracking
    /// <summary>
    /// 
    /// </summary>
    protected static bool HasPvPAstralFire => Player.HasStatus(true, StatusID.AstralFire_3212, StatusID.AstralFireIi_3213, StatusID.AstralFireIii_3381);

    /// <summary>
    /// 
    /// </summary>
    protected static bool HasPvPUmbralIce => Player.HasStatus(true, StatusID.UmbralIce_3214, StatusID.UmbralIceIi_3215, StatusID.UmbralIceIii_3382);

    /// <summary>
    /// 
    /// </summary>
    protected static bool HasFire => Player.HasStatus(true, StatusID.Firestarter);

    /// <summary>
    /// 
    /// </summary>
    protected static bool HasThunder => Player.HasStatus(true, StatusID.Thunderhead);

    /// <summary>
    /// Indicates whether the next GCD (Global Cooldown) action is instant.
    /// </summary>
    protected bool NextGCDisInstant => Player.HasStatus(true, StatusID.Triplecast, StatusID.Swiftcast);

    /// <summary>
    /// Determines if the player can make the next action instant by checking the availability of Triplecast or Swiftcast.
    /// </summary>
    protected bool CanMakeInstant => TriplecastPvE.Cooldown.CurrentCharges > 0 || !SwiftcastPvE.Cooldown.IsCoolingDown;

    /// <summary>
    /// Calculates the total number of instant casts available based on Triplecast charges, active Triplecast status, and Swiftcast charges.
    /// </summary>
    protected int ThisManyInstantCasts => (TriplecastPvE.Cooldown.CurrentCharges * 3) + Player.StatusStack(true, StatusID.Triplecast) + SwiftcastPvE.Cooldown.CurrentCharges;

    /// <summary>
    /// Calculates the deficit between the number of available instant casts and the current Astral Soul stacks.
    /// </summary>
    protected int AstralDefecit => ThisManyInstantCasts - AstralSoulStacks;
    #endregion

    #region PvE Actions Unassignable
    /// <summary>
    /// 
    /// </summary>
    public static bool ParadoxPvEReady => Service.GetAdjustedActionId(ActionID.FirePvE) == ActionID.ParadoxPvE;
    #endregion

    #region Debug
    /// <inheritdoc/>
    public override void DisplayStatus()
    {
        ImGui.Text("Is next GCD be instant: " + NextGCDisInstant.ToString());
        ImGui.Text("Can next GCD be instant: " + CanMakeInstant.ToString());
        ImGui.Text("Number of Instant Casts Available: " + ThisManyInstantCasts.ToString());
        ImGui.Text("AstralDefecit: " + AstralDefecit.ToString());
        ImGui.Text("HasFire: " + HasFire.ToString());
        ImGui.Text("HasThunder: " + HasThunder.ToString());
        ImGui.Separator();
        ImGui.Text("PolyglotStacks: " + PolyglotStacks.ToString());
        ImGui.Text("IsPolyglotStacksMaxed: " + IsPolyglotStacksMaxed.ToString());
        ImGui.Separator();
        ImGui.Text("InUmbralIce: " + InUmbralIce.ToString());
        ImGui.Text("InAstralFire: " + InAstralFire.ToString());
        ImGui.Separator();
        ImGui.Text("UmbralIceStacks: " + UmbralIceStacks.ToString());
        ImGui.Text("AstralFireStacks: " + AstralFireStacks.ToString());
        ImGui.Text("AstralSoulStacks: " + AstralSoulStacks.ToString());
        ImGui.Text("Soul Stack Count: " + SoulStackCount.ToString());
        ImGui.Text("Is Soul Stacks Maxed: " + IsSoulStacksMaxed.ToString());
        ImGui.Text("Max Soul Stacks: " + MaxSoulCount.ToString());
        ImGui.Separator();
        ImGui.Text("UmbralHearts: " + UmbralHearts.ToString());
        ImGui.Text("IsParadoxActive: " + IsParadoxActive.ToString());
        ImGui.Text("IsEnochianActive: " + IsEnochianActive.ToString());
        ImGui.Text("EnochianTimeRaw: " + EnochianTimeRaw.ToString());
        ImGui.Text("EnochianTime: " + EnochianTime.ToString());
        ImGui.TextColored(ImGuiColors.DalamudViolet, "PvE Actions");
        ImGui.Text("ParadoxPvEReady: " + ParadoxPvEReady.ToString());
        ImGui.TextColored(ImGuiColors.DalamudOrange, "PvP Actions");
        ImGui.Text("HasPvPAstralFire: " + HasPvPAstralFire.ToString());
        ImGui.Text("HasPvPUmbralIce: " + HasPvPUmbralIce.ToString());
    }
    #endregion

    #region PvE Actions

    static partial void ModifyBlizzardPvE(ref ActionSetting setting)
    {

    }

    static partial void ModifyFirePvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Firestarter];
    }

    static partial void ModifyTransposePvE(ref ActionSetting setting)
    {
        //setting.ActionCheck = () => DataCenter.DefaultGCDRemain <= ElementTimeRaw;
        setting.IsFriendly = true;
    }

    static partial void ModifyThunderPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.TargetStatusProvide = [StatusID.Thunder];
        setting.MPOverride = () => 0;
    }

    static partial void ModifyBlizzardIiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyScathePvE(ref ActionSetting setting)
    {
        setting.UnlockedByQuestID = 65886;
    }

    static partial void ModifyFireIiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyThunderIiPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.TargetStatusProvide = [StatusID.ThunderIi];
        setting.MPOverride = () => 0;
    }

    static partial void ModifyManawardPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Manaward];
        setting.UnlockedByQuestID = 65889;
        setting.IsFriendly = true;
    }

    static partial void ModifyManafontPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Thunderhead];
        setting.UnlockedByQuestID = 66609;
        setting.IsFriendly = true;
    }

    static partial void ModifyFireIiiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsLastGCD(ActionID.FireIiiPvE);
        setting.MPOverride = () => HasFire ? 0 : null;
    }

    static partial void ModifyBlizzardIiiPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsLastGCD(ActionID.BlizzardIvPvE, ActionID.BlizzardIiiPvE);
        setting.UnlockedByQuestID = 66610;
    }

    static partial void ModifyUmbralSoulPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InUmbralIce;
        setting.UnlockedByQuestID = 66609;
    }

    static partial void ModifyFreezePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InUmbralIce && UmbralHearts == 0;
        setting.UnlockedByQuestID = 66611;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyThunderIiiPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.ThunderIii];
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.UnlockedByQuestID = 66612;
        setting.MPOverride = () => 0;
    }

    static partial void ModifyAetherialManipulationPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyFlarePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InAstralFire && AstralSoulStacks <= 3;
        setting.UnlockedByQuestID = 66614;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 2,
        };
    }

    static partial void ModifyLeyLinesPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsMoving;
        setting.StatusProvide = [StatusID.LeyLines];
        setting.UnlockedByQuestID = 67215;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            TimeToKill = 15,
            AoeCount = 1,
        };
    }

    static partial void ModifyBlizzardIvPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InUmbralIce;
        setting.UnlockedByQuestID = 67218;
    }

    static partial void ModifyFireIvPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InAstralFire && AstralSoulStacks <= 5;
        setting.UnlockedByQuestID = 67219;
    }

    static partial void ModifyBetweenTheLinesPvE(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingBackward;
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyThunderIvPvE(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.ThunderIv];
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
        setting.MPOverride = () => 0;
    }

    static partial void ModifyTriplecastPvE(ref ActionSetting setting)
    {
        setting.StatusProvide = StatusHelper.SwiftcastStatus;
        setting.IsFriendly = true;
    }

    static partial void ModifyFoulPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PolyglotStacks > 0;
        setting.UnlockedByQuestID = 68128;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyDespairPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => InAstralFire;
    }

    static partial void ModifyXenoglossyPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => PolyglotStacks > 0;
    }

    static partial void ModifyHighFireIiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyHighBlizzardIiPvE(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
    }

    static partial void ModifyAmplifierPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => (InAstralFire || InUmbralIce) && !EnochianEndAfter(10) && !IsPolyglotStacksMaxed;
        setting.IsFriendly = true;
    }

    static partial void ModifyParadoxPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => IsParadoxActive;
        setting.StatusProvide = [StatusID.Firestarter];
    }

    static partial void ModifyHighThunderPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.TargetStatusProvide = [StatusID.HighThunder];
        setting.MPOverride = () => 0;
    }

    static partial void ModifyHighThunderIiPvE(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Thunderhead];
        setting.TargetStatusProvide = [StatusID.HighThunder_3872];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 3,
        };
        setting.MPOverride = () => 0;
    }

    static partial void ModifyRetracePvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => !IsMoving && !Player.HasStatus(true, StatusID.CircleOfPower);
        setting.StatusNeed = [StatusID.LeyLines];
        setting.IsFriendly = true;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyFlareStarPvE(ref ActionSetting setting)
    {
        setting.ActionCheck = () => AstralSoulStacks == 6;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }
    #endregion

    #region PvP Actions Unassignable
    /// <summary>
    /// 
    /// </summary>
    public static bool WreathOfFireReady => Service.GetAdjustedActionId(ActionID.ElementalWeavePvP) == ActionID.WreathOfFirePvP;

    /// <summary>
    /// 
    /// </summary>
    public static bool WreathOfIceReady => Service.GetAdjustedActionId(ActionID.ElementalWeavePvP) == ActionID.WreathOfIcePvP;
    #endregion

    #region PvP Actions
    static partial void ModifyFirePvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Paradox, StatusID.AstralFire_3212];
    }

    static partial void ModifyBlizzardPvP(ref ActionSetting setting)
    {
        setting.StatusProvide = [StatusID.Paradox, StatusID.UmbralIce_3214];
    }

    static partial void ModifyBurstPvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
    }

    static partial void ModifyParadoxPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.Paradox];
        setting.MPOverride = () => 0;
    }

    static partial void ModifyXenoglossyPvP(ref ActionSetting setting)
    {

    }

    static partial void ModifyLethargyPvP(ref ActionSetting setting)
    {
        setting.TargetStatusProvide = [StatusID.Lethargy_4333, StatusID.Heavy_1344];
    }

    static partial void ModifyAetherialManipulationPvP(ref ActionSetting setting)
    {
        setting.SpecialType = SpecialActionType.MovingForward;
    }

    static partial void ModifyElementalWeavePvP(ref ActionSetting setting)
    {
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.IsFriendly = true;
    }

    static partial void ModifyFireIiiPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.AstralFire_3212];
        setting.StatusProvide = [StatusID.AstralFireIi_3213];
    }

    static partial void ModifyFireIvPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.AstralFireIi_3213];
        setting.StatusProvide = [StatusID.AstralFireIii_3381];
    }

    static partial void ModifyHighFireIiPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.AstralFireIii_3381];
        setting.StatusProvide = [StatusID.AstralFire_3212];
    }

    static partial void ModifyFlarePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SoulResonance];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.MPOverride = () => 0;
    }

    static partial void ModifyBlizzardIiiPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.UmbralIce_3214];
        setting.StatusProvide = [StatusID.UmbralIceIi_3215];
    }

    static partial void ModifyBlizzardIvPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.UmbralIceIi_3215];
        setting.StatusProvide = [StatusID.UmbralIceIii_3382];
    }

    static partial void ModifyHighBlizzardIiPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.UmbralIceIii_3382];
        setting.StatusProvide = [StatusID.UmbralIce_3214];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
            // Removed ShouldCheckStatus = false
        };
    }

    static partial void ModifyFreezePvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.SoulResonance];
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.MPOverride = () => 0;
    }

    static partial void ModifyWreathOfFirePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => WreathOfFireReady;
    }

    static partial void ModifyWreathOfIcePvP(ref ActionSetting setting)
    {
        setting.ActionCheck = () => WreathOfIceReady;
        setting.IsFriendly = true;
    }

    static partial void ModifyFlareStarPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ElementalStar];
        setting.ActionCheck = () => HasPvPAstralFire;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.MPOverride = () => 0;
    }

    static partial void ModifyFrostStarPvP(ref ActionSetting setting)
    {
        setting.StatusNeed = [StatusID.ElementalStar];
        setting.ActionCheck = () => HasPvPUmbralIce;
        setting.CreateConfig = () => new ActionConfig()
        {
            AoeCount = 1,
        };
        setting.MPOverride = () => 0;
    }

    #endregion
}
