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

namespace SlimeBoss.SlimeBossCode.Cards.Common;

[Pool(typeof(SlimeBossCardPool))]
public class LeechEnergy : SlimeBossCardModel, IHasConsumeEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public LeechEnergy()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(5, 3);
		((ConstructedCardModel)this).WithEnergy(1, 0);
		((ConstructedCardModel)this).WithCards(1, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SlimeBossTip.Consume));
	}

	public async Task ConsumeEffect(PlayerChoiceContext ctx, Creature creature, AttackCommand command, int amount)
	{
		await PlayerCmd.GainEnergy(((DynamicVar)((CardModel)this).DynamicVars.Energy).BaseValue, ((CardModel)this).Owner);
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
