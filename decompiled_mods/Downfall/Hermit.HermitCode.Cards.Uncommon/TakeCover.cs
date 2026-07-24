using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Cards.Basic;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class TakeCover : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	protected override bool HasEnergyCostX => true;

	public TakeCover()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithCardTip<DefendHermit>((Action<DefendHermit, CardModel>?)WithPreviewModifiers);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await DownfallCardCmd.GiveCard<DefendHermit>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<DefendHermit>?)delegate(DefendHermit card)
		{
			WithPlayModifiers(card, (CardModel)(object)this);
		}, (Player?)null);
	}

	private static void WithPreviewModifiers(DefendHermit defend, CardModel cardModel)
	{
		int num;
		if (cardModel == null || !((AbstractModel)cardModel).IsMutable || cardModel._owner == null)
		{
			num = 3;
		}
		else
		{
			PlayerCombatState playerCombatState = cardModel.Owner.PlayerCombatState;
			num = ((playerCombatState != null) ? playerCombatState.Energy : 0);
		}
		int num2 = num;
		if (cardModel.IsUpgraded)
		{
			num2++;
		}
		WithModifiers(defend, num2);
	}

	private static void WithPlayModifiers(DefendHermit defend, CardModel cardModel)
	{
		int num = cardModel.ResolveEnergyXValue();
		if (cardModel.IsUpgraded)
		{
			num++;
		}
		WithModifiers(defend, num);
	}

	private static void WithModifiers(DefendHermit defend, int nimble)
	{
		DownfallCardCmd.ForceUpgrade((CardModel)(object)defend, nimble);
		((CardModel)defend).SetToFreeThisTurn();
	}
}
