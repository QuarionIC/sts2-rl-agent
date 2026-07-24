using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Extensions;

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class Teamwork : SlimeBossCardModel
{
	protected override bool HasEnergyCostX => true;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Teamwork()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithCommand(0m);
		((ConstructedCardModel)this).WithBlock(5, 3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int x = ((CardModel)this).ResolveEnergyXValue();
		await SlimeBossCmd.Command(ctx, ((CardModel)this).Owner, x, (ValueProp)8, (CardModel?)(object)this);
		for (int i = 0; i < x; i++)
		{
			await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		}
	}
}
