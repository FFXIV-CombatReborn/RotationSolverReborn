using ECommons.GameHelpers;
using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Actions;
internal class MpPotionItem : BaseItem
{
    public uint MaxMp { get; }

    protected override bool CanUseThis => Service.Config.UseMpPotions;

    public MpPotionItem(Item item) : base(item)
    {
        Lumina.Excel.Collection<ushort> data = _item.ItemAction.Value!.DataHQ;
        MaxMp = data[1];
    }

	public override bool CanUse(out IAction item, bool clippingCheck)
	{
		item = this;

		if (Player.Object == null)
		{
			return false;
		}
		return Player.Available && ObjectHelper.GetPlayerMPRatio() <= Service.Config.UseMpPotionsPercent && Player.Object.MaxMp - DataCenter.CurrentMp >= MaxMp && base.CanUse(out item);
	}
}
