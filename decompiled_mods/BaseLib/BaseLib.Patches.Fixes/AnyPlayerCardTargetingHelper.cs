using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace BaseLib.Patches.Fixes;

internal static class AnyPlayerCardTargetingHelper
{
	internal static bool IsAnyPlayerMultiplayer(CardModel? card)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Invalid comparison between Unknown and I4
		if (card != null && (int)card.TargetType == 5)
		{
			Player owner = card.Owner;
			int? obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				IRunState runState = owner.RunState;
				obj = ((runState == null) ? ((int?)null) : ((IPlayerCollection)runState).Players?.Count);
			}
			int? num = obj;
			return num.GetValueOrDefault() > 1;
		}
		return false;
	}
}
