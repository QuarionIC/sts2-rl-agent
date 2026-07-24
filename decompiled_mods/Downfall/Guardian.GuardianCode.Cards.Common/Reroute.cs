using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Interfaces;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using Guardian.GuardianCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Common;

[Pool(typeof(GuardianCardPool))]
public class Reroute : GuardianCardModel, IGemSocketCard, IModifyReplayCount, ICardOverlay
{
	public int GemSlots => 1;

	public Reroute()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(9, 3);
		((ConstructedCardModel)(object)this).WithPower<ReroutePower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await CommonActions.ApplySelf<ReroutePower>(ctx, (CardModel)(object)this, false);
	}
}
