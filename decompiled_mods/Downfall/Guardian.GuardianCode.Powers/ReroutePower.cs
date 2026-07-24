using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Compatibility;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Piles;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Powers;

public class ReroutePower : GuardianPowerModel, IModifyCardPlayResultLocation
{
	private CardModel? _cardSource;

	public CardLocationCompatiblity ModifyCardPlayResultLocationCompability(CardModel card, bool isAutoPlay, ResourceInfo resources, CardLocationCompatiblity cardLocation)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		Player owner = card.Owner;
		bool flag = _cardSource == card || card.Keywords.Contains((CardKeyword)1);
		bool flag2;
		if (!flag)
		{
			if (card != null)
			{
				CardType type = card.Type;
				if (type - 1 <= 1)
				{
					flag2 = true;
					goto IL_0039;
				}
			}
			flag2 = false;
			goto IL_0039;
		}
		goto IL_003e;
		IL_0039:
		flag = !flag2;
		goto IL_003e;
		IL_003e:
		if (flag || owner.Creature != ((PowerModel)this).Owner)
		{
			return cardLocation;
		}
		if (((CardPile)GuardianCombatModel.GetOrInitStasis(owner)).Cards.Count < GuardianCmd.GetMaxStasisSlots(owner))
		{
			return new CardLocationCompatiblity(card.Owner, GuardianPile.Stasis, (CardPilePosition)1);
		}
		return cardLocation;
	}

	public async Task AfterModifyingCardPlayResultLocationCompability(CardModel card, CardLocationCompatiblity cardLocation)
	{
		GuardianCmd.SetStasisCounter(card);
		card.EnergyCost.AfterCardPlayedCleanup();
		await PowerCmd.Decrement((PowerModel)(object)this);
	}

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		_cardSource = cardSource;
		return Task.CompletedTask;
	}

	public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		return PowerCmd.Remove((PowerModel)(object)this);
	}

	public ReroutePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
