using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.Powers;

public sealed class HexOriginalPower : CustomPowerModel
{
	public override PowerType Type => (PowerType)2;

	public override PowerStackType StackType => (PowerStackType)2;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromCard<Dazed>(false) };

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		Player owner = cardPlay.Card.Owner;
		if (((owner != null) ? owner.Creature : null) != ((PowerModel)this).Owner || (int)cardPlay.Card.Type == 1)
		{
			return;
		}
		((PowerModel)this).Flash();
		Player owner2 = cardPlay.Card.Owner;
		Creature player = ((owner2 != null) ? owner2.Creature : null);
		if (player != null)
		{
			CardPileAddResult[] statusCards = (CardPileAddResult[])(object)new CardPileAddResult[((PowerModel)this).Amount];
			int i = 0;
			while (i < ((PowerModel)this).Amount)
			{
				CardModel card = (CardModel)(object)((PowerModel)this).CombatState.CreateCard<Dazed>(player.Player);
				CardPileAddResult[] array = statusCards;
				int num = i;
				array[num] = await CardPileCmd.AddGeneratedCardToCombat(card, (PileType)1, (Player)null, (CardPilePosition)3);
				int num2 = i + 1;
				i = num2;
			}
			CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)statusCards, 1.2f, (CardPreviewStyle)1);
			await Cmd.Wait(0.5f, false);
		}
	}
}
