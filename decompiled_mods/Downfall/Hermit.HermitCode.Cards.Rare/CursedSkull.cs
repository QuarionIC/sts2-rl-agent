using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hermit.HermitCode.CustomEnums;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public class CursedSkull : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<DawnablesAwakened>();

	public override bool CanBeGeneratedInCombat => false;

	public CursedSkull()
		: base(2, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HermitKeywords.DeadOn));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)15));
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(((CardModel)this).SelectionScreenPrompt, 1);
		CardModel val2 = (await CardSelectCmd.FromHand(ctx, ((CardModel)this).Owner, val, (Func<CardModel, bool>)null, (AbstractModel)(object)this)).FirstOrDefault();
		if (val2 != null)
		{
			DeadOnReplay deadOnReplay = CardModifier.Modifiers(val2).OfType<DeadOnReplay>().FirstOrDefault();
			if (deadOnReplay == null)
			{
				CardModifier.AddModifier<DeadOnReplay>(val2);
			}
			else
			{
				deadOnReplay.Value++;
			}
		}
	}
}
