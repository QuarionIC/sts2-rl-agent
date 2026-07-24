using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Events;

namespace Snecko.SneckoCode.Powers;

public class MudShieldPower : SneckoPowerModel, IAfterCardMuddled
{
	public MudShieldPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		WithTip((StaticHoverTip)5);
		WithTip(SneckoKeywords.Muddle);
	}

	public async Task AfterCardMuddled(PlayerChoiceContext ctx, CardModel card, AbstractModel? source)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner)
		{
			await CreatureCmd.GainBlock(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, (ValueProp)4, (CardPlay)null, false);
		}
	}
}
