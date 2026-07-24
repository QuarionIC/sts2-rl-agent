using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Mantis : AwakenedCardModel
{
	public Mantis()
		: base(1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<StrengthPower>(2, 1);
		((ConstructedCardModel)(object)this).WithTip<PlumeJab>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
		await DownfallCardCmd.GiveCard<PlumeJab>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, upgraded: false, 0.1f, (CardPreviewStyle)1, skipAnimation: false, (Action<PlumeJab>?)null, (Player?)null);
	}
}
