using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Common;

[Pool(typeof(ChampCardPool))]
public class PreciseThrust : ChampCardModel, IBerserkerComboCard, IDefensiveComboCard
{
	public PreciseThrust()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(6, 2);
		((ConstructedCardModel)this).WithBlock(6, 2);
	}

	public Task BerserkerComboEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return Task.CompletedTask;
	}

	public async Task DefensiveComboEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = ((!((CardModel)this).Owner.ShouldBerserkerComboTrigger()) ? 1 : 2);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, num, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
