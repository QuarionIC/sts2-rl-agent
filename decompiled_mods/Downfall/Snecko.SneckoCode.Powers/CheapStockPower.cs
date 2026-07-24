using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Powers;

public class CheapStockPower : SneckoPowerModel
{
	public CheapStockPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(SneckoKeywords.Muddle);
	}

	protected override async Task AfterSideTurnStart(PlayerChoiceContext ctx, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (side == ((PowerModel)this).Owner.Side && ((PowerModel)this).Owner.Player != null)
		{
			IEnumerable<CardModel> cards = (from e in ((PowerModel)this).Owner.Player.GetHand()
				orderby e.EnergyCost.GetResolved() descending
				select e).Take(((PowerModel)this).Amount);
			await SneckoCmd.Muddle(ctx, cards, (AbstractModel?)(object)this);
		}
	}
}
