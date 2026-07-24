using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class Tsunami : SneckoCardModel
{
	public Tsunami()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithPower<TsunamiPower>(4, 1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoKeywords.Overflow));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.ApplySelf<TsunamiPower>(ctx, (CardModel)(object)this, false);
	}
}
