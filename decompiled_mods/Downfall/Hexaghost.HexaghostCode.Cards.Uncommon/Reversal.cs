using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class Reversal : HexaghostCardModel
{
	public Reversal()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(5, 1);
		((ConstructedCardModel)(object)this).WithRepeat(2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (HexaghostCmd.IsIgnited(((CardModel)this).Owner))
		{
			await HexaghostCmd.Extinguish(((CardModel)this).Owner);
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1 + ((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue, (string)null, (string)null, (string)null).Execute(ctx);
		}
		else
		{
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		}
	}
}
