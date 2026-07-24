using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class RainOfEmbers : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	protected override bool HasEnergyCostX => true;

	public RainOfEmbers()
		: base(0, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int x = ((CardModel)this).ResolveEnergyXValue();
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, x, (string)null, (string)null, (string)null).Execute(ctx);
		for (int i = 0; i < x; i++)
		{
			await CommonActions.Apply<WeakPower>(ctx, (CardModel)(object)this, cardPlay, false);
		}
	}
}
