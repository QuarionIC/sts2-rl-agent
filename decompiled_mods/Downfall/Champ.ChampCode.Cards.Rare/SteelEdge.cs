using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class SteelEdge : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	protected override bool HasEnergyCostX => true;

	public SteelEdge()
		: base(0, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)(object)this).WithFinisher();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = ((CardModel)this).ResolveEnergyXValue();
		if (num > 0)
		{
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, num, (string)null, (string)null, (string)null).Execute(ctx);
		}
	}

	public override async Task FinisherEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await ChampCmd.PlayFinisher(ctx, cardPlay, skipClear: false, Math.Max(1, ((CardModel)this).ResolveEnergyXValue()));
	}
}
