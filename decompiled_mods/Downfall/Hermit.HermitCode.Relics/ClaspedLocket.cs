using System.Collections.Generic;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace Hermit.HermitCode.Relics;

public sealed class ClaspedLocket : HermitRelicModel
{
	private bool _usedThisTurn;

	public override bool HasUponPickupEffect => true;

	public ClaspedLocket()
		: base((RelicRarity)1)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		WithVars((DynamicVar)new CardsVar(2));
		WithVar("Curses", 2);
		WithTip<Injury>();
	}

	public override async Task AfterObtained()
	{
		for (int i = 0; (decimal)i < ((RelicModel)this).DynamicVars["Curses"].BaseValue; i++)
		{
			CardCmd.PreviewCardPileAdd((IReadOnlyList<CardPileAddResult>)new _003C_003Ez__ReadOnlySingleElementList<CardPileAddResult>(await CardPileCmd.Add((CardModel)(object)((ICardScope)((RelicModel)this).Owner.RunState).CreateCard<Injury>(((RelicModel)this).Owner), (PileType)6, (CardPilePosition)1, (AbstractModel)null, false)), 2f, (CardPreviewStyle)1);
		}
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
	{
		if (card.Owner == ((RelicModel)this).Owner && (int)card.Type == 5 && !_usedThisTurn)
		{
			_usedThisTurn = true;
			((RelicModel)this).Flash();
			await CardCmd.Exhaust(ctx, card, false, false);
			await CardPileCmd.Draw(ctx, ((DynamicVar)((RelicModel)this).DynamicVars.Cards).BaseValue, ((RelicModel)this).Owner, false);
		}
	}

	public override Task AfterPlayerTurnStartEarly(PlayerChoiceContext choiceContext, Player player)
	{
		_usedThisTurn = false;
		return Task.CompletedTask;
	}
}
