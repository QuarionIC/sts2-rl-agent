using System;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Utils;
using Downfall.DownfallCode.Commands;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;

namespace Awakened.AwakenedCode.Relics;

[Pool(typeof(AwakenedRelicPool))]
public class Zetsumei : AwakenedRelicModel
{
	private bool _isActivating;

	private int _spellsPlayed;

	public override bool ShowCounter => CombatManager.Instance.IsInProgress;

	public override int DisplayAmount
	{
		get
		{
			if (IsActivating)
			{
				return ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue;
			}
			return SpellsPlayed % ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue;
		}
	}

	private bool IsActivating
	{
		get
		{
			return _isActivating;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_isActivating = value;
			UpdateDisplay();
		}
	}

	private int SpellsPlayed
	{
		get
		{
			return _spellsPlayed;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_spellsPlayed = value;
			UpdateDisplay();
		}
	}

	public Zetsumei()
		: base((RelicRarity)3)
	{
		WithCards(4);
	}

	private void UpdateDisplay()
	{
		if (IsActivating)
		{
			((RelicModel)this).Status = (RelicStatus)0;
		}
		else
		{
			int intValue = ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue;
			((RelicModel)this).Status = (RelicStatus)((SpellsPlayed % intValue == intValue - 1) ? 1 : 0);
		}
		((RelicModel)this).InvokeDisplayAmountChanged();
	}

	public override Task BeforeCombatStart()
	{
		SpellsPlayed = 0;
		UpdateDisplay();
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == ((RelicModel)this).Owner && CombatManager.Instance.IsInProgress && cardPlay.Card is ISpell)
		{
			SpellsPlayed++;
			if (SpellsPlayed % ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue == 0)
			{
				TaskHelper.RunSafely(DoActivateVisuals());
				await DownfallCardCmd.GiveCard<Ceremony>(((RelicModel)this).Owner, (PileType)2, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Ceremony>?)null, (Player?)null);
			}
		}
	}

	private async Task DoActivateVisuals()
	{
		IsActivating = true;
		((RelicModel)this).Flash();
		await Cmd.Wait(1f, false);
		IsActivating = false;
	}

	public override Task AfterCombatEnd(CombatRoom _)
	{
		((RelicModel)this).Status = (RelicStatus)0;
		IsActivating = false;
		return Task.CompletedTask;
	}
}
