using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class KillingSpree : ChampCardModel
{
	public KillingSpree()
		: base(2, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithPower<KillingSpreePower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampKeyword.TriggerSkillBonus));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Stance));
		((ConstructedCardModel)this).WithVar("Skill", 3, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<KillingSpreePower>(ctx, (CardModel)(object)this, false);
		for (int i = 0; i < ((CardModel)this).DynamicVars["Skill"].IntValue; i++)
		{
			await ((CardModel)this).Owner.ChampStance().SkillBonus(ctx);
		}
	}
}
