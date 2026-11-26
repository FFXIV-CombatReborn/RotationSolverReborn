using Lumina.Excel.Sheets;

namespace RotationSolver.Engine.Actions;

internal class PhoenixDownItem : BaseItem
{
	public PhoenixDownItem(Item item) : base(item)
	{
		ItemCheck = () =>
		{
			var t = DataCenter.DeathTarget;
			return t != null && ObjectHelper.CanBeRaised(t);
		};
	}

	private static bool AnyLivingRaiserInParty()
	{
		foreach (var member in DataCenter.PartyMembers)
		{
			if (member.IsDead)
			{
				continue;
			}

			if (member.IsJobCategory(JobRole.Healer) && member.Level >= 12)
			{
				return true;
			}

			if (member.IsJobs(ECommons.ExcelServices.Job.SMN) && member.Level >= 12)
			{
				return true;
			}

			if (member.IsJobs(ECommons.ExcelServices.Job.RDM) && member.Level >= 64)
			{
				return true;
			}
		}
		return false;
	}

	protected override bool CanUseThis => Service.Config.UsePhoenixDown && ((Service.Config.UsePhoenixDownHealerLogic && !AnyLivingRaiserInParty()) || !Service.Config.UsePhoenixDownHealerLogic) && DataCenter.DeathTarget != null;
}
