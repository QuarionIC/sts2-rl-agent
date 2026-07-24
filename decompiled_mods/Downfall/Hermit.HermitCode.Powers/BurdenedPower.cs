using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Downfall.DownfallCode.Commands;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Hermit.HermitCode.Powers;

public class BurdenedPower : HermitPowerModel, IHasSecondAmount
{
	public BurdenedPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithCards(0);
	}

	public string GetSecondAmount()
	{
		return $"{((DynamicVar)((PowerModel)this).DynamicVars.Cards).IntValue}";
	}

	public override async Task BeforeHandDrawLate(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((PowerModel)this).Owner.Player)
		{
			((PowerModel)this).Flash();
			await DownfallCardCmd.GiveCards<Decay>(player, (PileType)2, ((DynamicVar)((PowerModel)this).DynamicVars.Cards).BaseValue, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Decay>?)null, (Player?)null);
			await PowerCmd.Apply<VigorPower>(ctx, ((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}

	public void IncrementSelfDamage()
	{
		((AbstractModel)this).AssertMutable();
		CardsVar cards = ((PowerModel)this).DynamicVars.Cards;
		decimal baseValue = ((DynamicVar)cards).BaseValue + 1m;
		((DynamicVar)cards).BaseValue = baseValue;
		PowerExtensions.InvokeSecondAmountChanged((IHasSecondAmount)(object)this);
	}
}
