using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Extensions;
using Hexaghost.HexaghostCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Common;

[Pool(typeof(HexaghostCardPool))]
public class SpectersWail : HexaghostCardModel, IHasAfterlifeEffect
{
	public SpectersWail()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)3)
	{
		((ConstructedCardModel)(object)this).WithAfterlife();
		((ConstructedCardModel)this).WithDamage(4, 2);
	}

	public async Task AfterlifeEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithAttackerFx("vfx/vfx_spooky_scream", (string)null, (string)null).Execute(ctx);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 2, (string)null, (string)null, (string)null).WithAttackerFx("vfx/vfx_spooky_scream", (string)null, (string)null).Execute(ctx);
	}
}
