using System.Threading.Tasks;
using Downfall.DownfallCode.Commands;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Hermit.HermitCode.Relics;

public sealed class CharredGlove : HermitRelicModel
{
	public CharredGlove()
		: base((RelicRarity)4)
	{
		WithPower<VigorPower>(3, showTooltip: true);
	}

	protected override async Task AfterCardGeneratedForCombat(PlayerChoiceContext ctx, CardModel card, Player? player)
	{
		if (player == ((RelicModel)this).Owner && (int)card.Type == 5)
		{
			((RelicModel)this).Flash();
			await MyCommonActions.ApplySelf<VigorPower>(ctx, (AbstractModel)(object)this);
		}
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
	{
		if (card.Owner == ((RelicModel)this).Owner && (int)card.Type == 5)
		{
			((RelicModel)this).Flash();
			await MyCommonActions.ApplySelf<VigorPower>(ctx, (AbstractModel)(object)this);
		}
	}
}
