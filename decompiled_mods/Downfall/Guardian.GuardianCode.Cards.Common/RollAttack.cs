using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Interfaces;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Extensions;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Common;

[Pool(typeof(GuardianCardPool))]
public class RollAttack : GuardianCardModel, IGemSocketCard, IModifyReplayCount, ICardOverlay
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Magerblutooth>();

	protected override bool ShouldGlowGoldInternal => GuardianCmd.IsInMode<GuardianDefensiveMode>(((CardModel)this).Owner);

	public override TargetType TargetType
	{
		get
		{
			if (((CardModel)this)._owner != null && ((AbstractModel)this).IsMutable)
			{
				if (GuardianCmd.IsInMode<GuardianDefensiveMode>(((CardModel)this).Owner))
				{
					return (TargetType)3;
				}
				return (TargetType)2;
			}
			return (TargetType)2;
		}
	}

	public int GemSlots => 1;

	public RollAttack()
		: base(2, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(16, 4);
		((ConstructedCardModel)(object)this).WithBrace(8);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.DefensiveMode));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		if (!GuardianCmd.IsInMode<GuardianDefensiveMode>(((CardModel)this).Owner))
		{
			await GuardianCmd.Brace(ctx, (CardModel)(object)this);
		}
	}
}
