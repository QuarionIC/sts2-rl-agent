using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Hermit.HermitCode.Cards.Rare;

public sealed class Gambit : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Gambit()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCards(2, 1);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		Rng combatCardSelection = ((CardModel)this).Owner.RunState.Rng.CombatCardSelection;
		List<CardModel> cards = IEnumerableExtensions.TakeRandom<CardModel>(from c in ((CardModel)this).Owner.GetDiscard()
			where (int)c.Type == 1
			select c, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, combatCardSelection).ToList();
		await CardPileCmd.Add((IEnumerable<CardModel>)cards, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		foreach (CardModel item in cards)
		{
			item.EnergyCost.AddThisTurn(-1, true);
		}
	}
}
