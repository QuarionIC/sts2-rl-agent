using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class ViciousMockery : ChampCardModel
{
	public ViciousMockery()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)2)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithPower<VigorPower>(5, 1);
		((ConstructedCardModel)this).WithPower<WeakPower>(1, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampKeyword.TriggerSkillBonus));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<VigorPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.Apply<WeakPower>(ctx, cardPlay.Target, (CardModel)(object)this, false);
		await ((CardModel)this).Owner.ChampStance().SkillBonus(ctx);
	}
}
