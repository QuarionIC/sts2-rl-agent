using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Powers;

public class FuturePlansPower : GuardianPowerModel
{
	public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((PowerModel)this).Owner.Side)
		{
			return;
		}
		Player player = ((PowerModel)this).Owner.Player;
		if (player == null || !GuardianCmd.CanPutIntoStasis(player))
		{
			return;
		}
		foreach (CardModel item in await DownfallCardCmd.SelectFromHand(ctx, DownfallCardSelectorPrefs.StasisSelectionPrompt, (PowerModel)(object)this, null, optional: true))
		{
			await GuardianCmd.PutIntoStasis(item, ctx, (AbstractModel)(object)this);
		}
	}

	public FuturePlansPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
