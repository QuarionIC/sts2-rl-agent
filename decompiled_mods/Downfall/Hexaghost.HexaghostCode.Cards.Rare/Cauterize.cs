using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class Cauterize : HexaghostCardModel
{
	protected override bool HasEnergyCostX => true;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	public Cauterize()
		: base(0, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(7, 2);
		((ConstructedCardModel)(object)this).WithTip<SoulBurnPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Target != null)
		{
			int num = ((CardModel)this).ResolveEnergyXValue();
			int num2 = (await CommonActions.CardAttack((CardModel)(object)this, cardPlay, num, (string)null, (string)null, (string)null).Execute(ctx)).Results.SelectMany((List<DamageResult> r) => r).Sum((DamageResult x) => x.TotalDamage);
			await CommonActions.Apply<SoulBurnPower>(ctx, cardPlay.Target, (CardModel)(object)this, (decimal)num2, false);
		}
	}
}
