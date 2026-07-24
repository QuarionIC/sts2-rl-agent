using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;
using SlimeBoss.SlimeBossCode.Interfaces;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class GrowthPunch : SlimeBossCardModel, IHasConsumeEffect
{
	public GrowthPunch()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(4, 1);
		((ConstructedCardModel)this).WithBlock(4, 1);
		((ConstructedCardModel)this).WithVar("Increase", 4, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SlimeBossTip.Consume));
	}

	public Task ConsumeEffect(PlayerChoiceContext ctx, Creature creature, AttackCommand command, int amount)
	{
		((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy(((CardModel)this).DynamicVars["Increase"].BaseValue);
		((DynamicVar)((CardModel)this).DynamicVars.Block).UpgradeValueBy(((CardModel)this).DynamicVars["Increase"].BaseValue);
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
