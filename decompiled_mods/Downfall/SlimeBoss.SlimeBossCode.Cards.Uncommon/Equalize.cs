using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
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
public class Equalize : SlimeBossCardModel, IHasConsumeEffect
{
	public override bool CanBeGeneratedInCombat => false;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Equalize()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(8, 4);
		((ConstructedCardModel)this).WithHeal(4, 2);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SlimeBossTip.Consume));
	}

	public Task ConsumeEffect(PlayerChoiceContext ctx, Creature creature, AttackCommand command, int amount)
	{
		return CardCmd.AutoPlay(ctx, (CardModel)(object)this, creature, (AutoPlayType)1, false, false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await CreatureCmd.Heal(((CardModel)this).Owner.Creature, ((DynamicVar)((CardModel)this).DynamicVars.Heal).BaseValue, true);
	}
}
