using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class ForceShield : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public ForceShield()
		: base(4, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(11, 3);
		((ConstructedCardModel)this).WithEnergy(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		await CreatureCmd.GainBlock(((CardModel)this).Owner.Creature, ((CardModel)this).DynamicVars.Block, cardPlay, false);
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if ((object)card != this || ((CardModel)this).IsClone)
		{
			return Task.CompletedTask;
		}
		int num = CombatManager.Instance.History.Entries.OfType<CardGeneratedEntry>().Count((CardGeneratedEntry c) => c.Creator == card.Owner);
		((CardModel)this).EnergyCost.AddThisCombat(-num * ((DynamicVar)((CardModel)this).DynamicVars.Energy).IntValue, false);
		return Task.CompletedTask;
	}

	public override Task AfterCardGeneratedForCombat(CardModel card, Player? creator)
	{
		if (card.Owner != ((CardModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		((CardModel)this).EnergyCost.AddThisCombat(-((DynamicVar)((CardModel)this).DynamicVars.Energy).IntValue, false);
		return Task.CompletedTask;
	}
}
