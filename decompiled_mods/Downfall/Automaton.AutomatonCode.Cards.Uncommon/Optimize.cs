using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using Automaton.AutomatonCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Optimize : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Optimize()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeyword((CardKeyword)3, (UpgradeType)1);
		((ConstructedCardModel)(object)this).WithPower<OptimizePower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Stash));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<OptimizePower>(ctx, (CardModel)(object)this, false);
	}
}
