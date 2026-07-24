using System.Globalization;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Events;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Powers;

public class RisingChorusPower : AwakenedPowerModel, IOnChant, IHasSecondAmount
{
	public RisingChorusPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithVar("UsesLeft", 0m);
	}

	public string GetSecondAmount()
	{
		return ((decimal)((PowerModel)this).Amount - ((PowerModel)this).DynamicVars["UsesLeft"].BaseValue).ToString(CultureInfo.InvariantCulture);
	}

	public async Task OnCardChanted(CardModel card, PlayerChoiceContext ctx, CardPlay cardPlay, bool firstTime)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner && card is IChantable && ((PowerModel)this).DynamicVars["UsesLeft"].BaseValue < (decimal)((PowerModel)this).Amount)
		{
			DynamicVar obj = ((PowerModel)this).DynamicVars["UsesLeft"];
			decimal baseValue = obj.BaseValue;
			obj.BaseValue = baseValue + 1m;
			((PowerModel)this).InvokeDisplayAmountChanged();
			((PowerModel)this).Flash();
			await AwakenedCmd.Chant(ctx, card, cardPlay);
		}
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player.Creature != ((PowerModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		((PowerModel)this).DynamicVars["UsesLeft"].BaseValue = 0m;
		((PowerModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}
}
