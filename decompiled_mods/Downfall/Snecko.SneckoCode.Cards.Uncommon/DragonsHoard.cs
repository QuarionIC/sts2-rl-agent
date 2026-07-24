using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class DragonsHoard : SneckoCardModel
{
	public DragonsHoard()
		: base(3, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<StrengthPower>(3, 0);
		((ConstructedCardModel)this).WithPower<DexterityPower>(3, 0);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<DexterityPower>(ctx, (CardModel)(object)this, false);
	}
}
