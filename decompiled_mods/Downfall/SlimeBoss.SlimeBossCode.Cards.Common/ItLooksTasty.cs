using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using SlimeBoss.SlimeBossCode.Cards.Token;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;
using SlimeBoss.SlimeBossCode.Interfaces;

namespace SlimeBoss.SlimeBossCode.Cards.Common;

[Pool(typeof(SlimeBossCardPool))]
public class ItLooksTasty : SlimeBossCardModel, IHasConsumeEffect
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public ItLooksTasty()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(8, 2);
		((ConstructedCardModel)this).WithUpgradingCardTip<Lick>((Action<Lick, CardModel>)null);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SlimeBossTip.Consume));
	}

	public async Task ConsumeEffect(PlayerChoiceContext ctx, Creature creature, AttackCommand command, int amount)
	{
		await DownfallCardCmd.GiveCard<Lick>(((CardModel)this).Owner, (PileType)2, (CardPilePosition)1, ((CardModel)this).IsUpgraded, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Lick>?)null, (Player?)null);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}
}
