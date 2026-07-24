using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Powers;

public class GamblePower : SneckoPowerModel
{
	public GamblePower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(SneckoKeywords.Muddle);
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((PowerModel)this).Owner.Player && ((PowerModel)this).AmountOnTurnStart != 0)
		{
			await SneckoCmd.Muddle(ctx, await CardPileCmd.Draw(ctx, (decimal)((PowerModel)this).Amount, player, true), (AbstractModel?)(object)this);
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
