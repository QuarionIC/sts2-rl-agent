using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class AllOut : ChampCardModel
{
	public AllOut()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)(object)this).WithRepeat(2, 1);
		((ConstructedCardModel)(object)this).WithFinisher();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Stance));
	}

	public override async Task FinisherEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await ChampCmd.PlayFinisher(ctx, cardPlay, skipClear: true, ((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue);
	}
}
