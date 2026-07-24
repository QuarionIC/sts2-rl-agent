using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Cards.Curse;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class Midnight : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public Midnight()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(12, 3);
		((ConstructedCardModel)(object)this).WithTip<ImpendingDoom>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.CardBlock((CardModel)(object)this, play);
		await DownfallCardCmd.GiveCard<ImpendingDoom>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<ImpendingDoom>?)null, (Player?)null);
	}
}
