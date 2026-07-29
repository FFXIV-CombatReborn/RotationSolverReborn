namespace RotationSolver.ItemTemplates;


// RotationDesc here is used to display burst actions, indicating that if 'Auto Burst' is not turned on, the actions will not be released
[RotationDesc(ActionID.None)]
// Link to the Ratation source code
[SourceCode(Path = "%Branch/FilePath to your sourse code% eg. main/DefaultRotations/Melee/NIN_Default.cs%")]
// The detailed or extended description links of this Ratation, such as loop diagrams, recipe urls, teaching videos, etc.,
// can be written more than one
[LinkDescription("%Link to the pics or just a link%", "%Description about your rotation.%")]
[YoutubeLink(ID = "%If you got a youtube video link, please add here, just video id!%")]

//For the case your rotation is still beta.
[BetaRotation]

// Change this base class to your job's base class. It is named like XXX_Base.
internal class Full_Template : AST_Base
{
    // Change this to the game version right now.
    public override string GameVersion => "6.48";

    // Change this to your custom Rotation name
    public override string RotationName => "$safeitemname$";

    // Change this to your custom Rotation description
    public override string Description => "Your description about this rotation.";

    #region If you want to change the auto healing, please change these bools.
    public override bool CanHealAreaSpell => base.CanHealAreaSpell;
    public override bool CanHealAreaAbility => base.CanHealAreaAbility;
    public override bool CanHealSingleSpell => base.CanHealSingleSpell;
    public override bool CanHealSingleAbility => base.CanHealSingleAbility;
    #endregion

    #region GCD actions
    protected override bool GeneralGCD(out IAction act)
    {
        throw new NotImplementedException();
    }

    //For some gcds very important, even more than healing, defense, interrupt, etc.
    [RotationDesc("Optional description for Emergency GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool EmergencyGCD(IAction? nextGCD, out IAction act)
    {
        return base.EmergencyGCD(nextGCD, out act);
    }

    //For some gcds that moving forward.
    [RotationDesc("Optional description for Moving Forward GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool MoveForwardGCD(out IAction act)
    {
        return base.MoveForwardGCD(out act);
    }

    [RotationDesc("Optional description for Defense Area GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool DefenseAreaGCD(out IAction act)
    {
        return base.DefenseAreaGCD(out act);
    }

    [RotationDesc("Optional description for Defense Single GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool DefenseSingleGCD(out IAction act)
    {
        return base.DefenseSingleGCD(out act);
    }

    [RotationDesc("Optional description for Healing Area GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool HealAreaGCD(out IAction act)
    {
        return base.HealAreaGCD(out act);
    }

    [RotationDesc("Optional description for Healing Single GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool HealSingleGCD(out IAction act)
    {
        return base.HealSingleGCD(out act);
    }
    #endregion

    #region 0GCD actions
    //Some 0gcds that don't need to a hostile target in attack range.
    protected override bool GeneralAbility(out IAction act)
    {
        return base.GeneralAbility(out act);
    }

    protected override bool AttackAbility(out IAction act)
    {
        return base.AttackAbility(out act);
    }

    //For some 0gcds very important, even more than healing, defense, interrupt, etc.
    [RotationDesc("Optional description for Emergency 0GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool EmergencyAbility(IAction nextGCD, out IAction act)
    {
        return base.EmergencyAbility(nextGCD, out act);
    }

    //Some 0gcds that moving forward. In general, it doesn't need to be override.
    [RotationDesc("Optional description for Moving Forward 0GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool MoveForwardAbility(out IAction act)
    {
        return base.MoveForwardAbility(out act);
    }

    //Some 0gcds that moving back. In general, it doesn't need to be override.
    [RotationDesc("Optional description for Moving Back 0GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool MoveBackAbility(out IAction act)
    {
        return base.MoveBackAbility(out act);
    }

    //Some 0gcds that speed.For example sprint.
    [RotationDesc("Optional description for Speed 0GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool SpeedAbility(out IAction act)
    {
        return base.SpeedAbility(out act);
    }

    //Some 0gcds that defense area.
    [RotationDesc("Optional description for Defense Area 0GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool DefenseAreaAbility(out IAction act)
    {
        return base.DefenseAreaAbility(out act);
    }

    //Some 0gcds that defense single.
    [RotationDesc("Optional description for Defense Single 0GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool DefenseSingleAbility(out IAction act)
    {
        return base.DefenseSingleAbility(out act);
    }

    //Some 0gcds that healing area.
    [RotationDesc("Optional description for Healing Area 0GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool HealAreaAbility(out IAction act)
    {
        return base.HealAreaAbility(out act);
    }

    //Some 0gcds that healing single.
    [RotationDesc("Optional description for Healing Single 0GCD")]
    [RotationDesc(ActionID.None)]
    protected override bool HealSingleAbility(out IAction act)
    {
        return base.HealSingleAbility(out act);
    }
    #endregion

    #region Extra
    // Modify the type of Medicine, default is the most appropriate Medicine, generally do not need to modify
    public override MedicineType MedicineType => base.MedicineType;

    //For counting down action when pary counting down is active.
    protected override IAction CountDownAction(float remainTime)
    {
        return base.CountDownAction(remainTime);
    }

    //Extra configurations you want to show on your rotation config.
    protected override IRotationConfigSet CreateConfiguration()
    {
        return base.CreateConfiguration();
    }

    //This is the method to update all field you wrote, it is used first during one frame.
    protected override void UpdateInfo()
    {
        base.UpdateInfo();
    }

    //This method is used when player change the terriroty, such as go into one duty, you can use it to set the field.
    public override void OnTerritoryChanged()
    {
        base.OnTerritoryChanged();
    }

    //This method is used to debug. If you want to show some information in Debug panel, show something here.
    public override void DisplayStatus()
    {
        base.DisplayStatus();
    }

    // Modify this bool to display your DisplayStatus on the Formal Page.
    public override bool ShowStatus => base.ShowStatus;
    #endregion
}
