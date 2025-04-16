namespace RebornRotations.PVPRotations.Healer;

[Rotation("Default PVP", CombatType.PvP, GameVersion = "7.2")]
[SourceCode(Path = "main/RebornRotations/PVPRotations/Healer/SGE_Default.PVP.cs")]
[Api(4)]
public class SGE_DefaultPVP : SageRotation
{
    #region Configurations

    [RotationConfig(CombatType.PvP, Name = "Use Purify")]
    public bool UsePurifyPvP { get; set; } = true;

    [RotationConfig(CombatType.PvP, Name = "Stop attacking while in Guard.")]
    public bool RespectGuard { get; set; } = true;
    #endregion

    #region Standard PVP Utilities
    private bool DoPurify(out IAction? action)
    {
        action = null;
        if (!UsePurifyPvP) return false;

        var purifiableStatusesIDs = new List<int>
        {
            // Stun, DeepFreeze, HalfAsleep, Sleep, Bind, Heavy, Silence
            1343, 3219, 3022, 1348, 1345, 1344, 1347
        };

        if (purifiableStatusesIDs.Any(id => Player.HasStatus(false, (StatusID)id)))
        {
            return PurifyPvP.CanUse(out action);
        }

        return false;
    }
    #endregion

    #region oGCDs
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;
        if (DoPurify(out action)) return true;

        if (!Player.HasStatus(true, StatusID.Kardia_2871) && KardiaPvP.CanUse(out action)) return true;

        return base.EmergencyAbility(nextGCD, out action);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (!Target.HasStatus(true, StatusID.Toxikon) && ToxikonPvP.CanUse(out action, usedUp: true)) return true;

        return base.AttackAbility(nextGCD, out action);
    }
    #endregion

    #region GCDs
    protected override bool GeneralGCD(out IAction? action)
    {
        action = null;
        if (RespectGuard && Player.HasStatus(true, StatusID.Guard)) return false;

        if (PneumaPvP.CanUse(out action)) return true;

        if (PsychePvP.CanUse(out action)) return true;
        if (PhlegmaIiiPvP.CanUse(out action)) return true;

        if (!IsLastAction(ActionID.EukrasiaPvP) && InCombat && !Target.HasStatus(true, StatusID.EukrasianDosisIii_3108) && !Player.HasStatus(true, StatusID.Eukrasia) && EukrasiaPvP.CanUse(out action, usedUp: true)) return true;

        if (DosisIiiPvP.CanUse(out action)) return true;

        return base.GeneralGCD(out action);
    }
    #endregion
}