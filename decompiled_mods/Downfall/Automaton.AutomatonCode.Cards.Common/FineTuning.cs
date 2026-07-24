using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Common;

[Pool(typeof(AutomatonCardPool))]
public class FineTuning : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public FineTuning()
		: base(0, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Stash));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)5));
		((ConstructedCardModel)this).WithCards(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		List<CardModel> list = (await CardSelectCmd.FromHand(ctx, ((CardModel)this).Owner, new CardSelectorPrefs(StashCmd.StashSelectionPrompt, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue), (Func<CardModel, bool>)null, (AbstractModel)(object)this)).ToList();
		foreach (CardModel item in list)
		{
			CardCmd.Upgrade(item, (CardPreviewStyle)1);
			item.AddKeyword((CardKeyword)5);
		}
		await StashCmd.Stash(((CardModel)this).Owner, list);
	}
}
