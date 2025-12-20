using ECommons.GameHelpers;
using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Actions;

internal class HpPotionItem : BaseItem
{
    private readonly float _percent;
    private readonly uint _maxHp;

	public uint MaxHp => !Player.Available || Player.Object == null ? 0 : Math.Min((uint)(Player.Object.MaxHp * _percent), _maxHp);

	protected override bool CanUseThis => Service.Config.UseHpPotions;

    public HpPotionItem(Item item) : base(item)
    {
        Lumina.Excel.Collection<ushort> data = _item.ItemAction.Value!.DataHQ;
        _percent = data[0] / 100f;
        _maxHp = data[1];
    }

    public override bool CanUse(out IAction item, bool clippingCheck)
    {
        item = this;

		if (Player.Object == null)
		{
			return false;
		}

		return Player.Available && ObjectHelper.GetPlayerHealthRatio() <= Service.Config.HealthSingleAbilityHot && Player.Object.MaxHp - Player.Object.CurrentHp >= MaxHp && base.CanUse(out item);
    }
}
