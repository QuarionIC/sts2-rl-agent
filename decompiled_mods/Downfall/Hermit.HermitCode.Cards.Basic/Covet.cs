using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Basic;

public sealed class Covet : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Covet()
		: base(0, (CardType)2, (CardRarity)1, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCards(1, 1);
		((ConstructedCardModel)this).WithVar("Discard", 1, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.DiscardSelectionPrompt, ((CardModel)this).DynamicVars["Discard"].IntValue);
		CardModel val2 = (await CardSelectCmd.FromHandForDiscard(ctx, ((CardModel)this).Owner, val, (Func<CardModel, bool>)null, (AbstractModel)(object)this)).FirstOrDefault();
		if (val2 != null)
		{
			if ((int)val2.Type != 5)
			{
				await CardCmd.Discard(ctx, val2);
			}
			else
			{
				await CardCmd.Exhaust(ctx, val2, false, false);
			}
		}
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}
}
