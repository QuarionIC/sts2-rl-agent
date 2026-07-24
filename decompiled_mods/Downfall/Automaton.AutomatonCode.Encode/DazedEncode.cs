using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Encode;

public class DazedEncode : Encodable
{
	public override TargetType Target => (TargetType)1;

	public override CardType Type => (CardType)2;

	public override DynamicVar FunctionDynamicVar => new DynamicVar("Dazed", 0m);

	public override Task OnPlay(AbstractModel model, PlayerChoiceContext ctx, Creature? target, CardPlay? cardPlay)
	{
		Player player = model.GetCreature().Player;
		if (player != null)
		{
			return DownfallCardCmd.GiveCards<Dazed>(player, (PileType)1, model.GetDynamicVars()["Dazed"].BaseValue, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Dazed>?)null, (Player?)null);
		}
		return Task.CompletedTask;
	}

	public override IEnumerable<IHoverTip> HoverTips(AbstractModel model)
	{
		return new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromCard<Dazed>(false));
	}

	public override DynamicVar DynamicVar(AbstractModel model)
	{
		return model.GetDynamicVars()["Dazed"];
	}
}
