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

namespace Automaton.AutomatonCode.Cards.Multiplayer;

[Pool(typeof(AutomatonCardPool))]
public class Share : AutomatonCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	public Share()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<SharePower>(3, 1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<SharePower>(ctx, (CardModel)(object)this, false);
	}
}
