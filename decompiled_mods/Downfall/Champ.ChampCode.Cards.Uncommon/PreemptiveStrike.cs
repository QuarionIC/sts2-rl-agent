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
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class PreemptiveStrike : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	protected override bool ShouldGlowRedInternal => !((CardModel)this).Owner.ShouldDefensiveComboTrigger();

	public PreemptiveStrike()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)3)
	{
		((ConstructedCardModel)this).WithCalculatedDamage(0, (Func<CardModel, Creature, decimal>)CalcDamage, (ValueProp)8, 0, 0);
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)1 });
		((ConstructedCardModel)(object)this).WithDefensiveTip();
		((ConstructedCardModel)(object)this).WithTip<CounterPower>();
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	private static decimal CalcDamage(CardModel arg1, Creature? arg2)
	{
		return arg1.Owner.Creature.GetPowerAmount<CounterPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		if (!((CardModel)this).Owner.ShouldDefensiveComboTrigger())
		{
			int num = -((CardModel)this).Owner.Creature.GetPowerAmount<CounterPower>() / 2;
			if (num < 0)
			{
				await CommonActions.ApplySelf<CounterPower>(ctx, (CardModel)(object)this, (decimal)num, false);
			}
		}
	}
}
