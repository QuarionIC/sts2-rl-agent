using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Features;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Common;

[Pool(typeof(SlimeBossCardPool))]
public class FormOfWall : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public FormOfWall()
		: base(2, (CardType)2, (CardRarity)2, CustomTargetType.AllAttackingEnemies)
	{
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(12, 3);
		((ConstructedCardModel)this).WithPower<GoopPower>(4, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.Apply<GoopPower>(ctx, (CardModel)(object)this, cardPlay, false);
	}
}
