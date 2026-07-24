using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Automaton.AutomatonCode.Potions;

[Pool(typeof(AutomatonPotionPool))]
public class KiosCleverConcoctionPotion : AutomatonPotionModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Chimedragon>();

	public KiosCleverConcoctionPotion()
		: base((PotionRarity)3, (PotionUsage)1, (TargetType)1)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		WithTip(AutomatonTip.Encode);
	}

	protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		Rng rng = ((PotionModel)this).Owner.RunState.Rng.CombatCardSelection;
		List<CardModel> cards = CardFactory.FilterForCombat(from c in ((PotionModel)this).Owner.Character.CardPool.GetUnlockedCards(((PotionModel)this).Owner.UnlockState, ((PotionModel)this).Owner.RunState.CardMultiplayerConstraint)
			where AutomatonCmd.IsEncodable(c) && (int)c.Rarity != 7
			select c).ToList();
		FunctionCard functionCard = null;
		while (functionCard == null)
		{
			List<CardModel> list = CardFactory.GetDistinctForCombat(((PotionModel)this).Owner, (IEnumerable<CardModel>)cards, 3, rng).ToList();
			CardModel val = await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((PotionModel)this).Owner, false);
			if (val != null)
			{
				functionCard = await AutomatonCmd.EncodeCard(val, ctx);
				continue;
			}
			break;
		}
	}
}
