using System;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Common;

[Pool(typeof(AutomatonCardPool))]
public class WildStrike : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public WildStrike()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(14, 5);
		((ConstructedCardModel)(object)this).WithTip<Wound>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null).Execute(ctx);
		await DownfallCardCmd.GiveCard<Wound>(((CardModel)this).Owner, (PileType)1, (CardPilePosition)3, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Wound>?)null, (Player?)null);
	}
}
