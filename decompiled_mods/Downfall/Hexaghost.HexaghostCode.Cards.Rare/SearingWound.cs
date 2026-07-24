using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(HexaghostCardPool))]
public class SearingWound : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	public SearingWound()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)7)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)5, (UpgradeType)1);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState == null)
		{
			return;
		}
		foreach (Creature hittableEnemy in ((CardModel)this).CombatState.HittableEnemies)
		{
			int powerAmount = hittableEnemy.GetPowerAmount<SoulBurnPower>();
			await DownfallCreatureCmd.Damage(ctx, hittableEnemy, powerAmount, (ValueProp)14, ((CardModel)this).Owner.Creature, (CardModel?)(object)this, cardPlay);
		}
	}
}
