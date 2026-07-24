using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class SnakeRake : SneckoCardModel
{
	public SnakeRake()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(7, 2);
		((ConstructedCardModel)this).WithCards(1, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoKeywords.Muddle));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await SneckoCmd.Muddle(ctx, await CommonActions.Draw((CardModel)(object)this, ctx), (AbstractModel?)(object)this);
	}
}
