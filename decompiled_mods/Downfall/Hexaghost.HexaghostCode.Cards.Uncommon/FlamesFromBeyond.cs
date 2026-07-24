using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Extensions;
using Hexaghost.HexaghostCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class FlamesFromBeyond : HexaghostCardModel, IHasAfterlifeEffect
{
	public FlamesFromBeyond()
		: base(2, (CardType)2, (CardRarity)3, (TargetType)3)
	{
		((ConstructedCardModel)(object)this).WithAfterlife();
		((ConstructedCardModel)this).WithPower<SoulBurnPower>(10, 3);
	}

	public async Task AfterlifeEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await MyCommonActions.Apply<SoulBurnPower>(ctx, (AbstractModel)(object)this, cardPlay.Target);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await AfterlifeEffect(ctx, cardPlay);
		await MyCommonActions.Apply<SoulBurnPower>(ctx, (AbstractModel)(object)this, cardPlay.Target);
	}
}
