using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Powers;

public class SerpentineSleuthPower : SneckoPowerModel
{
	protected override async Task BeforeCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner.Creature == ((PowerModel)this).Owner && (int)cardPlay.Card.Type == 3)
		{
			await PlayerCmd.GainEnergy((decimal)((PowerModel)this).Amount, cardPlay.Card.Owner);
		}
	}

	public SerpentineSleuthPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
