using ECommons.GameHelpers;
using RotationSolver.Basic.Traits;

namespace RotationSolver.Basic.Configuration.Conditions;

[Description("Trait Condition")]
internal class TraitCondition : DelayCondition
{
    public uint TraitID { get; set; } = 0;
    internal IBaseTrait? _trait;

    public override bool CheckBefore(ICustomRotation rotation)
    {
        if (TraitID != 0 && (_trait == null || _trait.ID != TraitID))
        {
            _trait = null;
            var traits = rotation.AllTraits;
            for (int i = 0; i < traits.Length; i++)
            {
                if (traits[i].ID == TraitID)
                {
                    _trait = traits[i];
                    break;
                }
            }
        }
        return base.CheckBefore(rotation);
    }

    protected override bool IsTrueInside(ICustomRotation rotation)
    {
        if (_trait == null || !Player.Available)
        {
            return false;
        }

        bool result = _trait.EnoughLevel;
        return result;
    }
}
