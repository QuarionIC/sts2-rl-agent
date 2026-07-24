using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hermit.HermitCode.Cards.Rare;

public class ScopeOut : HermitCardModel
{
	public ScopeOut()
		: base(1, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<StrengthPower>(2, 1);
		((ConstructedCardModel)(object)this).WithPower<ScopeOutPower>(1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<ScopeOutPower>(ctx, (CardModel)(object)this, false);
	}
}
