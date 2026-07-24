using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Common;

[Pool(typeof(ChampCardPool))]
public class Piledriver : ChampCardModel
{
	public Piledriver()
		: base(2, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(8, 4);
		((ConstructedCardModel)this).WithPower<VulnerablePower>(2, 0);
		((ConstructedCardModel)this).WithPower<WeakPower>(2, 0);
		((ConstructedCardModel)(object)this).WithFinisher();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Target != null)
		{
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
			await CommonActions.Apply<VulnerablePower>(ctx, cardPlay.Target, (CardModel)(object)this, false);
			await CommonActions.Apply<WeakPower>(ctx, cardPlay.Target, (CardModel)(object)this, false);
		}
	}
}
