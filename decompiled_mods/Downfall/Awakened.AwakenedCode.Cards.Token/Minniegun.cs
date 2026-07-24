using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class Minniegun : AwakenedCardModel
{
	public Minniegun()
		: base(2, (CardType)1, (CardRarity)7, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(2, 0);
		((ConstructedCardModel)(object)this).WithRepeat(5, 1);
		((ConstructedCardModel)(object)this).WithTip<Void>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitCount(((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue).Execute(ctx);
		await DownfallCardCmd.GiveCard<Void>(((CardModel)this).Owner, (PileType)1, (CardPilePosition)3, upgraded: false, 0.3f, (CardPreviewStyle)1, skipAnimation: false, (Action<Void>?)null, (Player?)null);
	}
}
