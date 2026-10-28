using FFXIVClientStructs.FFXIV.Client.Game;

namespace RotationSolver.Basic.Helpers
{
	internal static class ActionManagerHelper
	{
		private static unsafe ActionManager* GetActionManager()
		{
			return ActionManager.Instance();
		}

		/// <summary>
		/// Gets the recast time for a specific action.
		/// </summary>
		/// <param name="type">The type of the action.</param>
		/// <param name="id">The ID of the action.</param>
		/// <returns>The recast time for the specified action.</returns>
		public static unsafe float GetRecastTime(ActionType type, uint id)
		{
			var actionManager = GetActionManager();
			return actionManager == null ? 0 : actionManager->GetRecastTime(type, id);
		}

		/// <summary>
		/// Gets the default recast time.
		/// </summary>
		/// <returns>The default recast time.</returns>
		public static unsafe float GetDefaultRecastTime()
		{
			return GetRecastTime(ActionType.Action, (uint)ActionID.HeatedSplitShotPvE);
		}

		/// <summary>
		/// Gets the elapsed recast time for a specific action.
		/// </summary>
		/// <param name="type">The type of the action.</param>
		/// <param name="id">The ID of the action.</param>
		/// <returns>The elapsed recast time for the specified action.</returns>
		public static unsafe float GetRecastTimeElapsed(ActionType type, uint id)
		{
			var actionManager = GetActionManager();
			return actionManager == null ? 0 : actionManager->GetRecastTimeElapsed(type, id);
		}

		/// <summary>
		/// Gets the elapsed recast time for the default action.
		/// </summary>
		/// <returns>The elapsed recast time for the default action.</returns>
		public static unsafe float GetDefaultRecastTimeElapsed()
		{
			return GetRecastTimeElapsed(ActionType.Action, (uint)ActionID.HeatedSplitShotPvE);
		}
	}
}