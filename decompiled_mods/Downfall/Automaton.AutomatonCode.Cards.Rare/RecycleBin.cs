using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class RecycleBin : AutomatonCardModel
{
	public RecycleBin()
		: base(1, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<RecycleBinPower>(4, 1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
	}

	protected override Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return CommonActions.ApplySelf<RecycleBinPower>(ctx, (CardModel)(object)this, false);
	}
}
