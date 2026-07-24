using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;

namespace Snecko.SneckoCode.Cards.Multiplayer;

[Pool(typeof(SneckoCardPool))]
public class SpreadTheChaos : SneckoCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public SpreadTheChaos()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)6)
	{
		((ConstructedCardModel)(object)this).WithMuddle(1m, 1m);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		Creature target = cardPlay.Target;
		IEnumerable<CardModel> enumerable = ((target == null) ? null : (from e in target.Player?.GetHand()
			orderby e.EnergyCost.GetResolved() descending
			select e).Take(((CardModel)this).DynamicVars["Muddle"].IntValue));
		if (enumerable != null)
		{
			await SneckoCmd.Muddle(ctx, enumerable, (AbstractModel?)(object)this);
		}
	}
}
