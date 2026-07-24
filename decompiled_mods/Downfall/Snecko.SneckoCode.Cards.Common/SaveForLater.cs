using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class SaveForLater : SneckoCardModel
{
	public SaveForLater()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)(object)this).WithPower<SaveForLaterPower>(1, 1, showTooltip: false);
		((ConstructedCardModel)this).WithDamage(8, 3);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)5));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await CommonActions.ApplySelf<SaveForLaterPower>(ctx, (CardModel)(object)this, false);
	}
}
