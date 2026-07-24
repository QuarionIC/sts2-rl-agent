using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Common;

[Pool(typeof(HexaghostCardPool))]
public class Displace : HexaghostCardModel
{
	public Displace()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithBlock(6, 2);
		((ConstructedCardModel)this).WithDamage(6, 2);
		((ConstructedCardModel)this).WithCards(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		CardModel val = (await DownfallCardCmd.SelectFromHand(ctx, DownfallCardSelectorPrefs.ToTopSelectionPrompt, (CardModel)(object)this)).FirstOrDefault();
		if (val != null)
		{
			await CardPileCmd.Add(val, (PileType)1, (CardPilePosition)2, (AbstractModel)null, false);
		}
	}
}
