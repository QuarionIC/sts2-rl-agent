using System.Collections.Generic;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Displays;
using Awakened.AwakenedCode.Extensions;
using Awakened.AwakenedCode.Piles;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class Inscribe : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Inscribe()
		: base(0, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithConjure((CardModel e) => e.IsUpgraded);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).IsUpgraded)
		{
			await AwakenedCmd.Conjure(((CardModel)this).Owner);
		}
		ICombatState combatState = ((CardModel)this).Owner.Creature.CombatState;
		List<CardModel> list = new List<CardModel>
		{
			(CardModel)(object)combatState.CreateCard<BurningStudy>(((CardModel)this).Owner),
			(CardModel)(object)combatState.CreateCard<Cryostasis>(((CardModel)this).Owner),
			(CardModel)(object)combatState.CreateCard<Darkleech>(((CardModel)this).Owner),
			(CardModel)(object)combatState.CreateCard<Thunderbolt>(((CardModel)this).Owner)
		};
		CardModel val = await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((CardModel)this).Owner, false);
		if (val != null)
		{
			AwakenedPile orInitSpellbook = AwakenedModel.GetOrInitSpellbook(((CardModel)this).Owner);
			orInitSpellbook.AddPersistentType(val);
			orInitSpellbook.AddPersistentType(val);
			CardModel val2 = val.CreateClone();
			((CardPile)orInitSpellbook).AddInternal(val, -1, false);
			((CardPile)orInitSpellbook).AddInternal(val2, -1, false);
			AwakenedDisplay.Refresh(((CardModel)this).Owner);
		}
	}
}
