using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Common;

[Pool(typeof(ChampCardPool))]
public class StanceDance : ChampCardModel
{
	public StanceDance()
		: base(0, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampKeyword.TriggerSkillBonus));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Stance));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await ChampCmd.SelectStanceToEnter(ctx, ((CardModel)this).Owner);
		ChampStanceModel stance = ((CardModel)this).Owner.ChampStance();
		await stance.SkillBonus(ctx);
		if (((CardModel)this).IsUpgraded)
		{
			await stance.SkillBonus(ctx);
		}
	}
}
