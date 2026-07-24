using System;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class Procession : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Procession()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithTip<Void>();
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel card = await CommonActions.SelectSingleCard((CardModel)(object)this, DownfallCardSelectorPrefs.PlaySelectionPrompt, ctx, (PileType)1);
		if (card != null)
		{
			await CardCmd.AutoPlay(ctx, card, (Creature)null, (AutoPlayType)1, false, false);
			await DownfallCardCmd.GiveCards<Void>(((CardModel)this).Owner, (PileType)1, (decimal)card.EnergyCost.GetResolved(), (CardPilePosition)3, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Void>?)null, (Player?)null);
		}
	}
}
