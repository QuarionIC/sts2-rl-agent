using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Events;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class Cower : SneckoCardModel, IAfterCardMuddled
{
	public Cower()
		: base(2, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(14, 4);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoKeywords.Muddle));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}

	public async Task AfterCardMuddled(PlayerChoiceContext ctx, CardModel card, AbstractModel? source)
	{
		if ((object)card == this)
		{
			await CommonActions.ApplySelf<WeakPower>(ctx, (CardModel)(object)this, false);
		}
	}
}
