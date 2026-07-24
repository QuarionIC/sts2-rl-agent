using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;

namespace Automaton.AutomatonCode.Relics;

[Pool(typeof(AutomatonRelicPool))]
public class CableSpool : AutomatonRelicModel
{
	private int _usesLeft;

	public override bool ShowCounter => CombatManager.Instance.IsInProgress;

	public override int DisplayAmount => _usesLeft;

	public CableSpool()
		: base((RelicRarity)3)
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		WithCards(2);
		WithTip(AutomatonTip.Encode);
	}

	public override Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (card.Owner != ((RelicModel)this).Owner || !card.IsUpgradable || !AutomatonCmd.IsEncodable(card) || _usesLeft == 0)
		{
			return Task.CompletedTask;
		}
		CardCmd.Upgrade(card, (CardPreviewStyle)1);
		_usesLeft--;
		((RelicModel)this).InvokeDisplayAmountChanged();
		((RelicModel)this).Flash();
		return Task.CompletedTask;
	}

	public override Task BeforeCombatStart()
	{
		_usesLeft = ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue;
		((RelicModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		((RelicModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}
}
