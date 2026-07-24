using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Powers;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public sealed class HighNoon : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public HighNoon()
		: base(2, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<HighNoonPower>(1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<HighNoonDefendPower>(0, 1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)15));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.ApplySelf<HighNoonPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<HighNoonDefendPower>(ctx, (CardModel)(object)this, false);
	}
}
