using Lumina.Excel.Sheets;

namespace RotationSolver.Basic.Actions;

internal class PhoenixDownItem : BaseItem
{
	public PhoenixDownItem(Item item) : base(item)
	{
		// Only allow when the current DeathTarget is actually raisable (range/LoS/flags)
		ItemCheck = () =>
		{
			var t = DataCenter.DeathTarget;
			return t != null && RotationSolver.Basic.Helpers.ObjectHelper.CanBeRaised(t);
		};
	}

	private static bool AnyLivingRaiserInParty()
	{
		foreach (IBattleChara member in DataCenter.PartyMembers)
		{
			if (member.IsDead) continue;
			if (member.IsJobCategory(JobRole.Healer)) return true;
			if (member.IsJobs(ECommons.ExcelServices.Job.SMN)) return true;
			if (member.IsJobs(ECommons.ExcelServices.Job.RDM)) return true;
		}
		return false;
	}

	protected override bool CanUseThis => Service.Config.UsePhoenixDown && !AnyLivingRaiserInParty() && DataCenter.DeathTarget != null;
}
