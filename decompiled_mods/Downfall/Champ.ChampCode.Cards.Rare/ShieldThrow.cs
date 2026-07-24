using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class ShieldThrow : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	protected override bool ShouldGlowRedInternal => !((CardModel)this).Owner.ShouldDefensiveComboTrigger();

	public ShieldThrow()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithCalculatedDamage(0, (Func<CardModel, Creature, decimal>)BlockDamage, (ValueProp)8, 0, 0);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)(object)this).WithDefensiveTip();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
		((ConstructedCardModel)(object)this).WithPower<NoBlockNextTurnPower>(1, showTooltip: false);
	}

	private static decimal BlockDamage(CardModel card, Creature? creature)
	{
		return card.Owner.Creature.Block;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitCount(2).Execute(ctx);
		if (!((CardModel)this).Owner.ShouldDefensiveComboTrigger())
		{
			await CommonActions.ApplySelf<NoBlockNextTurnPower>(ctx, (CardModel)(object)this, false);
		}
	}
}
