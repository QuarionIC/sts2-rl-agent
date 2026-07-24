using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Cards.Basic;
using Hermit.HermitCode.CustomEnums;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public class HairTrigger : HermitCardModel
{
	public HairTrigger()
		: base(2, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)3, (UpgradeType)1);
		((ConstructedCardModel)(object)this).WithPower<HairTriggerPower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HermitKeywords.DeadOn));
		((ConstructedCardModel)(object)this).WithCardTip<StrikeHermit>((Action<StrikeHermit, CardModel>?)delegate(StrikeHermit e, CardModel _)
		{
			((CardModel)e).AddKeyword((CardKeyword)1);
		});
	}

	protected override Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return CommonActions.ApplySelf<HairTriggerPower>(ctx, (CardModel)(object)this, false);
	}
}
