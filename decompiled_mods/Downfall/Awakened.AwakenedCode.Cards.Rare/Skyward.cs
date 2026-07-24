using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class Skyward : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	private int PowersPlayedThisCombat => CombatManager.Instance.History.Entries.OfType<CardPlayStartedEntry>().Count((CardPlayStartedEntry e) => (int)e.CardPlay.Card.Type == 3 && e.CardPlay.Card.Owner == ((CardModel)this).Owner);

	public Skyward()
		: base(7, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(18, 6);
		((ConstructedCardModel)this).WithCards(1, 0);
		((ConstructedCardModel)this).WithEnergyTip();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if ((object)card != this || ((CardModel)this).IsClone)
		{
			return Task.CompletedTask;
		}
		ReduceCostBy(PowersPlayedThisCombat);
		return Task.CompletedTask;
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (cardPlay.Card.Owner != ((CardModel)this).Owner || (int)cardPlay.Card.Type != 3)
		{
			return Task.CompletedTask;
		}
		ReduceCostBy(1);
		return Task.CompletedTask;
	}

	private void ReduceCostBy(int amount)
	{
		((CardModel)this).EnergyCost.AddThisCombat(-amount, false);
	}
}
