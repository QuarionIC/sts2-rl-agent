using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class SwordThrow : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	protected override bool ShouldGlowRedInternal => !((CardModel)this).Owner.ShouldBerserkerComboTrigger();

	public SwordThrow()
		: base(1, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(9, 4);
		((ConstructedCardModel)(object)this).WithRepeat(2);
		((ConstructedCardModel)(object)this).WithPower<EntangledNextTurnPower>(1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithBerserkerTip();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitCount(((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue).Execute(ctx);
		if (!((CardModel)this).Owner.ShouldBerserkerComboTrigger())
		{
			await CommonActions.ApplySelf<EntangledNextTurnPower>(ctx, (CardModel)(object)this, false);
		}
	}
}
