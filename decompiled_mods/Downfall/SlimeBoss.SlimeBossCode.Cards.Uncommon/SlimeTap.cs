using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class SlimeTap : SlimeBossCardModel
{
	public SlimeTap()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithEnergy(1, 1);
		((ConstructedCardModel)this).WithCards(2, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (await SlimeBossCmd.Absorb(ctx, (CardModel)(object)this))
		{
			await PlayerCmd.GainEnergy(((DynamicVar)((CardModel)this).DynamicVars.Energy).BaseValue, ((CardModel)this).Owner);
			await CommonActions.Draw((CardModel)(object)this, ctx);
		}
	}
}
