namespace RotationSolver.RebornRotations.Magical;

[Rotation("Default", CombatType.PvE, GameVersion = "7.41")]
[SourceCode(Path = "main/BasicRotations/Magical/BLM_Default.cs")]

public class BLM_Default : BlackMageRotation
{
    #region Config Options
    [RotationConfig(CombatType.PvE, Name = "Use Transpose to Astral Fire before Paradox")]
    public bool UseTransposeForParadox { get; set; } = true;

    [RotationConfig(CombatType.PvE, Name = "Extend Astral Fire time more conservatively (3 GCDs) (Default is 2 GCDs)")]
    public bool ExtendTimeSafely { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = @"Use ""Double Paradox"" rotation [N15]")]
    public bool UseN15 { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Leylines in combat when standing still")]
    public bool LeylineMadness { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use both stacks of Leylines automatically")]
    public bool Leyline2Madness { get; set; } = false;

    [RotationConfig(CombatType.PvE, Name = "Use Retrace when out of Leylines in combat and standing still")]
    public bool UseRetrace { get; set; } = false;
    #endregion

    #region Tracking Properties
    public override void DisplayRotationStatus()
    {
        ImGui.Text($"In InFireOrIce Logic: {InFireOrIce(out _, out _)}");
        ImGui.Text($"In GoFire Logic: {GoFire(out _)}");
        ImGui.Text($"In MaintainIce Logic: {MaintainIce(out _)}");
        ImGui.Text($"In DoIce Logic: {DoIce(out _)}");
        ImGui.Text($"In GoFire Logic: {GoFire(out _)}");
        ImGui.Text($"In MaintainFire Logic: {MaintainFire(out _)}");
        ImGui.Text($"In DoFire Logic: {DoFire(out _)}");
        ImGui.Text($"In UseInstanceSpell Logic: {UseInstanceSpell(out _)}");
        ImGui.Text($"In AddThunder Logic: {AddThunder(out _)}");
        ImGui.Text($"In AddElementBase Logic: {AddElementBase(out _)}");
        ImGui.Text($"In UsePolyglot Logic: {UsePolyglot(out _)}");
        ImGui.Text($"In MaintainStatus Logic: {MaintainStatus(out _)}");
    }
    #endregion

    #region Additional oGCD Logic

    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < FireIiiPvE.Info.CastTime + CountDownAhead)
        {
            if (FireIiiPvE.CanUse(out IAction act))
            {
                return act;
            }
        }
        //if (remainTime <= 12 && SharpcastPvE.CanUse(out act, usedUp: true)) return act;
        return base.CountDownAction(remainTime);
    }

    [RotationDesc]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        if (UseRetrace && RetracePvE.CanUse(out act))
        {
            return true;
        }
        //To Fire
        if (CurrentMp >= 7200 && UmbralIceStacks == 2 && ParadoxPvE.EnoughLevel)
        {
            if ((HasFire || HasSwift) && TransposePvE.CanUse(out act))
            {
                return true;
            }
        }
        if (nextGCD.IsTheSameTo(false, FireIiiPvE) && HasFire)
        {
            if (TransposePvE.CanUse(out act))
            {
                return true;
            }
        }

        //Using Manafont
        if (InAstralFire)
        {
            if (CurrentMp == 0 && ManafontPvE.CanUse(out act))
            {
                return true;
            }
            //To Ice
            if (NeedToTransposeGoIce(true) && TransposePvE.CanUse(out act))
            {
                return true;
            }
        }

        return base.EmergencyAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.AetherialManipulationPvE)]
    protected override bool MoveForwardAbility(IAction nextGCD, out IAction? act)
    {
        if (AetherialManipulationPvE.CanUse(out act))
        {
            return true;
        }
        return base.MoveForwardAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.BetweenTheLinesPvE)]
    protected override bool MoveBackAbility(IAction nextGCD, out IAction? act)
    {
        if (BetweenTheLinesPvE.CanUse(out act))
        {
            return true;
        }
        return base.MoveBackAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ManawardPvE)]
    protected override bool DefenseSingleAbility(IAction nextGCD, out IAction? act)
    {
        if (ManawardPvE.CanUse(out act))
        {
            return true;
        }
        return base.DefenseSingleAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.ManawardPvE, ActionID.AddlePvE)]
    protected sealed override bool DefenseAreaAbility(IAction nextGCD, out IAction? act)
    {
        if (ManawardPvE.CanUse(out act))
        {
            return true;
        }

        if (AddlePvE.CanUse(out act))
        {
            return true;
        }

        return base.DefenseAreaAbility(nextGCD, out act);
    }
    #endregion

    #region oGCD Logic
    [RotationDesc(ActionID.TransposePvE, ActionID.LeyLinesPvE, ActionID.RetracePvE)]
    protected override bool GeneralAbility(IAction nextGCD, out IAction? act)
    {
        if (IsMoving && HasHostilesInRange && InCombat && TriplecastPvE.CanUse(out act, usedUp: true))
        {
            return true;
        }

        if (LeylineMadness && InCombat && HasHostilesInRange && LeyLinesPvE.CanUse(out act, usedUp: Leyline2Madness))
        {
            return true;
        }

        if (!IsLastAbility(ActionID.LeyLinesPvE) && UseRetrace && InCombat && HasHostilesInRange && RetracePvE.CanUse(out act))
        {
            return true;
        }

        return base.GeneralAbility(nextGCD, out act);
    }

    [RotationDesc(ActionID.RetracePvE, ActionID.SwiftcastPvE, ActionID.TriplecastPvE, ActionID.AmplifierPvE)]
    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        if (InUmbralIce)
        {
            if (UmbralIceStacks == 2 && !HasFire
                && !IsLastGCD(ActionID.ParadoxPvE))
            {
                if (SwiftcastPvE.CanUse(out act))
                {
                    return true;
                }

                if (TriplecastPvE.CanUse(out act, usedUp: true))
                {
                    return true;
                }
            }

            if (UmbralIceStacks < MaxSoulCount && LucidDreamingPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (InAstralFire)
        {
            if (TriplecastPvE.CanUse(out act, gcdCountForAbility: 5))
            {
                return true;
            }
        }

        if (AmplifierPvE.CanUse(out act))
        {
            return true;
        }

        if (TriplecastPvE.CanUse(out act))
        {
            if (nextGCD.Equals(ActionID.OccultCometPvE))
            {
                return true;
            }
        }

        if (HasBuffs && UseBurstMedicine(out act))
        {
            return true;
        }

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic
    protected override bool GeneralGCD(out IAction? act)
    {
        if (FlareStarPvE.CanUse(out act))
        {
            return true;
        }

        if (InFireOrIce(out act, out bool mustGo))
        {
            return true;
        }

        if (mustGo)
        {
            return false;
        }

        if (AddElementBase(out act))
        {
            return true;
        }

        if (ScathePvE.CanUse(out act))
        {
            return true;
        }

        if (MaintainStatus(out act))
        {
            return true;
        }

        return base.GeneralGCD(out act);
    }

    private bool InFireOrIce(out IAction? act, out bool mustGo)
    {
        act = null;
        mustGo = false;
        if (InUmbralIce)
        {
            if (GoFire(out act))
            {
                return true;
            }

            if (MaintainIce(out act))
            {
                return true;
            }

            if (DoIce(out act))
            {
                return true;
            }
        }
        if (InAstralFire)
        {
            if (GoIce(out act))
            {
                return true;
            }

            if (MaintainFire(out act))
            {
                return true;
            }

            if (DoFire(out act))
            {
                return true;
            }
        }
        return false;
    }

    private bool GoIce(out IAction? act)
    {
        act = null;

        if (!NeedToGoIce)
        {
            return false;
        }

        //Use Manafont or transpose.
        if ((!ManafontPvE.Cooldown.IsCoolingDown || NeedToTransposeGoIce(false))
            && UseInstanceSpell(out act))
        {
            return true;
        }

        //Go to Ice.
        if (BlizzardIiPvE.CanUse(out act))
        {
            return true;
        }

        if (BlizzardIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (TransposePvE.CanUse(out act))
        {
            return true;
        }

        if (BlizzardPvE.CanUse(out act))
        {
            return true;
        }

        return false;
    }

    private bool MaintainIce(out IAction? act)
    {
        act = null;
        if (UmbralIceStacks == 1 && MaxSoulCount == 3)
        {
            if (BlizzardIiPvE.CanUse(out act))
            {
                return true;
            }

            if (DataCenter.PlayerSyncedLevel() == 90 && BlizzardPvE.CanUse(out act))
            {
                return true;
            }

            if (BlizzardIiiPvE.CanUse(out act))
            {
                return true;
            }
        }
        if (UmbralIceStacks == 2 && DataCenter.PlayerSyncedLevel() < 90 && MaxSoulCount == 3)
        {
            if (BlizzardIiPvE.CanUse(out act))
            {
                return true;
            }

            if (BlizzardPvE.CanUse(out act))
            {
                return true;
            }
        }
        return false;
    }

    private bool DoIce(out IAction? act)
    {
        if (IsLastAction(ActionID.UmbralSoulPvE, ActionID.TransposePvE)
            && IsParadoxActive && BlizzardPvE.CanUse(out act))
        {
            return true;
        }

        if (UmbralIceStacks == MaxSoulCount && UsePolyglot(out act))
        {
            return true;
        }

        //Add Hearts
        if (UmbralIceStacks == MaxSoulCount && !IsLastGCD
            (ActionID.BlizzardIvPvE, ActionID.FreezePvE))
        {
            if (FreezePvE.CanUse(out act, skipAoeCheck: !BlizzardIvPvE.EnoughLevel))
            {
                return true;
            }

            if (BlizzardIvPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (AddThunder(out act, 5))
        {
            return true;
        }

        if (UmbralIceStacks == 2 && UsePolyglot(out act, 0))
        {
            return true;
        }

        if (IsParadoxActive)
        {
            if (BlizzardPvE.CanUse(out act))
            {
                return true;
            }
        }

        if (BlizzardIiPvE.CanUse(out act))
        {
            return true;
        }

        if (BlizzardIvPvE.CanUse(out act))
        {
            return true;
        }

        if (BlizzardPvE.CanUse(out act))
        {
            return true;
        }

        return false;
    }

    private bool GoFire(out IAction? act)
    {
        act = null;

        //Transpose line
        if (UmbralIceStacks < MaxSoulCount)
        {
            return false;
        }

        //Need more MP
        if (CurrentMp < 9600)
        {
            return false;
        }

        if (IsParadoxActive)
        {
            if (BlizzardPvE.CanUse(out act))
            {
                return true;
            }
        }

        //Go to Fire.
        if (FireIiPvE.CanUse(out act))
        {
            return true;
        }

        if (FireIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (TransposePvE.CanUse(out act))
        {
            return true;
        }

        if (FirePvE.CanUse(out act))
        {
            return true;
        }

        return false;
    }

    private bool MaintainFire(out IAction? act)
    {
        act = null;
        switch (AstralFireStacks)
        {
            case 1:
                if (FireIiPvE.CanUse(out act))
                {
                    return true;
                }

                if (UseN15)
                {
                    if (HasFire && FireIiiPvE.CanUse(out act))
                    {
                        return true;
                    }

                    if (IsParadoxActive && FirePvE.CanUse(out act))
                    {
                        return true;
                    }
                }
                if (FireIiiPvE.CanUse(out act))
                {
                    return true;
                }

                break;
            case 2:
                if (FireIiPvE.CanUse(out act))
                {
                    return true;
                }

                if (FirePvE.CanUse(out act))
                {
                    return true;
                }

                break;
        }

        //if (ElementTimeEndAfterGCD(ExtendTimeSafely ? 3u : 2u))
        //{
        //    if (CurrentMp >= FirePvE.Info.MPNeed * 2 + 800 && FirePvE.CanUse(out act)) return true;
        //    if (FlarePvE.CanUse(out act)) return true;
        //    if (DespairPvE.CanUse(out act)) return true;
        //}

        return false;
    }

    private bool DoFire(out IAction? act)
    {
        if (UsePolyglot(out act))
        {
            return true;
        }

        // Add thunder only at combat start.
        if (CombatElapsedLess(5))
        {
            if (AddThunder(out act, 0))
            {
                return true;
            }
        }

        if (TriplecastPvE.CanUse(out act))
        {
            return true;
        }

        if (AddThunder(out act, 0) && StatusHelper.PlayerWillStatusEndGCD(1, 0, true,
            StatusID.Thundercloud))
        {
            return true;
        }

        if (UmbralHearts < 2 && AstralSoulStacks <= 3 && FlarePvE.CanUse(out act))
        {
            return true;
        }

        if (FireIiPvE.CanUse(out act))
        {
            return true;
        }

        if (CurrentMp >= FirePvE.Info.MPNeed + 800)
        {
            if (FireIvPvE.EnoughLevel)
            {
                if (FireIvPvE.CanUse(out act))
                {
                    return true;
                }
            }
            else if (HasFire)
            {
                if (FireIiiPvE.CanUse(out act))
                {
                    return true;
                }
            }
            if (FirePvE.CanUse(out act))
            {
                return true;
            }
        }

        if (DespairPvE.CanUse(out act))
        {
            return true;
        }

        return false;
    }

    private bool UseInstanceSpell(out IAction? act)
    {
        if (UsePolyglot(out act))
        {
            return true;
        }

        if (HasThunder && AddThunder(out act, 1))
        {
            return true;
        }

        return UsePolyglot(out act, 0);
    }

    private bool AddThunder(out IAction? act, uint gcdCount = 3)
    {
        act = null;
        //Return if just used.
        if (IsLastGCD(ActionID.ThunderPvE, ActionID.ThunderIiPvE, ActionID.ThunderIiiPvE, ActionID.ThunderIvPvE))
        {
            return false;
        }

        //So long for thunder.
        if (ThunderIiiPvE.EnoughLevel && ThunderIiiPvE.CanUse(out _) && (!ThunderIiiPvE.Target.Target?.WillStatusEndGCD(gcdCount, 0, true,
            StatusID.Thunder, StatusID.ThunderIi, StatusID.ThunderIii, StatusID.ThunderIv, StatusID.HighThunder_3872) ?? false))
        {
            return false;
        }

        if (!ThunderIiiPvE.Info.EnoughLevelAndQuest() && ThunderPvE.CanUse(out _) && (!ThunderPvE.Target.Target?.WillStatusEndGCD(gcdCount, 0, true,
            StatusID.Thunder, StatusID.ThunderIi, StatusID.ThunderIii, StatusID.ThunderIv, StatusID.HighThunder_3872) ?? false))
        {
            return false;
        }

        if (ThunderIiPvE.CanUse(out act))
        {
            return true;
        }

        if (ThunderIiiPvE.CanUse(out act))
        {
            return true;
        }

        if (!ThunderIiiPvE.Info.EnoughLevelAndQuest() && ThunderPvE.CanUse(out act))
        {
            return true;
        }

        return false;
    }

    private bool AddElementBase(out IAction? act)
    {
        if (CurrentMp >= 7200)
        {
            if (FireIiPvE.CanUse(out act))
            {
                return true;
            }

            if (FireIiiPvE.CanUse(out act))
            {
                return true;
            }

            if (FirePvE.CanUse(out act))
            {
                return true;
            }
        }
        else
        {
            if (BlizzardIiPvE.CanUse(out act))
            {
                return true;
            }

            if (BlizzardIiiPvE.CanUse(out act))
            {
                return true;
            }

            if (BlizzardPvE.CanUse(out act))
            {
                return true;
            }
        }
        return false;
    }

    private bool UsePolyglot(out IAction? act, uint gcdCount = 3)
    {
        act = null;

        if (gcdCount == 0 || (IsPolyglotStacksMaxed && (EnochianEndAfterGCD(gcdCount) || AmplifierPvE.Cooldown.WillHaveOneChargeGCD(gcdCount))))
        {
            if (FoulPvE.CanUse(out act, skipAoeCheck: !XenoglossyPvE.EnoughLevel))
            {
                return true;
            }

            if (XenoglossyPvE.CanUse(out act))
            {
                return true;
            }
        }
        return false;
    }

    private bool MaintainStatus(out IAction? act)
    {
        act = null;
        if (CombatElapsedLess(6))
        {
            return false;
        }

        if (UmbralSoulPvE.CanUse(out act))
        {
            return true;
        }

        if (InAstralFire && TransposePvE.CanUse(out act))
        {
            return true;
        }

        if (UseTransposeForParadox &&
            InUmbralIce && !IsParadoxActive && UmbralIceStacks == MaxSoulCount
            && TransposePvE.CanUse(out act))
        {
            return true;
        }

        return false;
    }

    private bool NeedToGoIce
    {
        get
        {
            //Can use Despair.
            if (DespairPvE.EnoughLevel && CurrentMp >= DespairPvE.Info.MPNeed)
            {
                return false;
            }

            //Can use Fire1
            return !FirePvE.EnoughLevel || CurrentMp < FirePvE.Info.MPNeed;
        }
    }

    private bool NeedToTransposeGoIce(bool usedOne)
    {
        if (!NeedToGoIce)
        {
            return false;
        }

        if (!ParadoxPvE.EnoughLevel)
        {
            return false;
        }

        int compare = usedOne ? -1 : 0;
        byte count = PolyglotStacks;
        if (count == compare++)
        {
            return false;
        }

        if (count == compare++ && !EnochianEndAfterGCD(2))
        {
            return false;
        }

        if (count >= compare && (HasFire || SwiftcastPvE.Cooldown.WillHaveOneChargeGCD(2) || TriplecastPvE.Cooldown.WillHaveOneChargeGCD(2)))
        {
            return true;
        }

        if (!HasFire && !SwiftcastPvE.Cooldown.WillHaveOneChargeGCD(2) && !TriplecastPvE.CanUse(out _, gcdCountForAbility: 8))
        {
            return false;
        }

        return true;
    }
    #endregion
}