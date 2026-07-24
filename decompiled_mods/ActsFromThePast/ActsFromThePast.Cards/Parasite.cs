using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace ActsFromThePast.Cards;

[Pool(typeof(CurseCardPool))]
public sealed class Parasite : CustomCardModel
{
	public override IEnumerable<CardKeyword> CanonicalKeywords => (IEnumerable<CardKeyword>)(object)new CardKeyword[1] { (CardKeyword)4 };

	public override int MaxUpgradeLevel => 0;

	public Parasite()
		: base(-1, (CardType)5, (CardRarity)9, (TargetType)0, true, true)
	{
	}

	public override async Task BeforeCardRemoved(CardModel card)
	{
		if ((object)card == this)
		{
			Player owner = ((CardModel)this).Owner;
			if (((owner != null) ? owner.Creature : null) != null)
			{
				await CreatureCmd.LoseMaxHp((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((CardModel)this).Owner.Creature, 3m, false);
				AFTPModAudio.Play("general", "blood_swish");
			}
		}
	}
}
