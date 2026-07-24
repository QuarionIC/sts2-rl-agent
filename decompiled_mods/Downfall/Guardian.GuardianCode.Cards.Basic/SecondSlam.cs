using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Interfaces;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Basic;

[Pool(typeof(GuardianCardPool))]
public class SecondSlam : GuardianCardModel, IGemSocketCard, IModifyReplayCount, ICardOverlay
{
	public int GemSlots
	{
		get
		{
			if (!((CardModel)this).IsUpgraded)
			{
				return 1;
			}
			return 2;
		}
	}

	public SecondSlam()
		: base(1, (CardType)1, (CardRarity)1, (TargetType)2)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)2,
			(CardKeyword)1
		});
		((ConstructedCardModel)this).WithDamage(7, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
