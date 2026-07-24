using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Downfall.DownfallCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class WindUp : ChampCardModel
{
	public WindUp()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Stance));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await ChampCmd.SelectStanceToEnter(ctx, ((CardModel)this).Owner);
		CardModel val = await CommonActions.SelectSingleCard((CardModel)(object)this, DownfallCardSelectorPrefs.ToHandSelectionPrompt, ctx, (PileType)1, (Func<CardModel, bool>)((CardModel c) => c.Tags.Contains(ChampTag.Finisher)));
		if (val != null)
		{
			await CardPileCmd.Add(val, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}
}
