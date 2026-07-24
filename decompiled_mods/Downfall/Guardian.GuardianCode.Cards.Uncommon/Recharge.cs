using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class Recharge : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public Recharge()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(7, 2);
		((ConstructedCardModel)(object)this).WithBrace(6, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await GuardianCmd.Brace(ctx, (CardModel)(object)this);
	}
}
