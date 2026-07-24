using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Hexaghost.HexaghostCode.Cards.Token;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Extensions;
using Hexaghost.HexaghostCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(HexaghostCardPool))]
public class NightmareGuise : HexaghostCardModel, IHasAfterlifeEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public NightmareGuise()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithUpgradingCardTip<ShadowGuise>((Action<ShadowGuise, CardModel>)null);
		((ConstructedCardModel)this).WithBlock(4, 2);
		((ConstructedCardModel)(object)this).WithAfterlife();
	}

	public async Task AfterlifeEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await DownfallCardCmd.GiveCard<ShadowGuise>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<ShadowGuise>?)null, (Player?)null);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await AfterlifeEffect(ctx, cardPlay);
	}
}
