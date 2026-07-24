using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Extensions;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class MassRepurpose : SlimeBossCardModel
{
	public MassRepurpose()
		: base(0, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithCommand(1m);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int absorbed = await SlimeBossCmd.AbsorbAll(ctx, (CardModel)(object)this);
		for (int i = 0; i < absorbed; i++)
		{
			await SlimeBossCmd.SplitRandom(ctx, ((CardModel)this).Owner, SlimeType.Specialist);
		}
		if (((CardModel)this).IsUpgraded)
		{
			await SlimeBossCmd.CommandAll(ctx, ((CardModel)this).Owner, (CardModel)(object)this, (ValueProp)8);
		}
	}
}
