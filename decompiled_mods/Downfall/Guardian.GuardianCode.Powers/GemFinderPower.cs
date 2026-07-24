using System.Threading.Tasks;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Powers;

public class GemFinderPower : GuardianPowerModel
{
	public GemFinderPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		WithTip(GuardianKeyword.Gem);
		WithTip(GuardianTip.Brace);
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel card = cardPlay.Card;
		if (card.Owner.Creature == ((PowerModel)this).Owner && card is IGemSocketCard { GemCount: not 0 })
		{
			await GuardianCmd.Brace(ctx, card.Owner, ((PowerModel)this).Amount);
		}
	}
}
