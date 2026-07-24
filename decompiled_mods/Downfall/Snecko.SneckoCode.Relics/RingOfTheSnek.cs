using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Events;

namespace Snecko.SneckoCode.Relics;

[Pool(typeof(SneckoRelicPool))]
public class RingOfTheSnek : SneckoRelicModel, IAfterOverflowEffect
{
	private bool _isActivating;

	private int _overflowEffectsPlayed;

	public override bool ShowCounter => CombatManager.Instance.IsInProgress;

	public override int DisplayAmount
	{
		get
		{
			if (IsActivating)
			{
				return ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue;
			}
			return OverflowEffectsPlayed % ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue;
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

	private int OverflowEffectsPlayed
	{
		get
		{
			return _overflowEffectsPlayed;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_overflowEffectsPlayed = value;
			UpdateDisplay();
		}
	}

	public RingOfTheSnek()
		: base((RelicRarity)4)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		WithVars((DynamicVar)new PowerVar<WeakPower>(1m), (DynamicVar)new PowerVar<VulnerablePower>(1m), (DynamicVar)new CardsVar(3));
	}

	public async Task AfterOverflowEffect(PlayerChoiceContext ctx, CardPlay cardPlay, CardModel card)
	{
		if (cardPlay.Card.Owner != ((RelicModel)this).Owner || !CombatManager.Instance.IsInProgress || ((RelicModel)this).Owner.Creature.CombatState == null)
		{
			return;
		}
		OverflowEffectsPlayed++;
		int intValue = ((DynamicVar)((RelicModel)this).DynamicVars.Cards).IntValue;
		if (OverflowEffectsPlayed % intValue == 0)
		{
			TaskHelper.RunSafely(DoActivateVisuals());
			Creature target = ((RelicModel)this).Owner.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((RelicModel)this).Owner.Creature.CombatState.HittableEnemies);
			if (target != null)
			{
				await PowerCmd.Apply<WeakPower>(ctx, target, DynamicVarSetExtensions.Power<WeakPower>(((RelicModel)this).DynamicVars).BaseValue, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				await PowerCmd.Apply<VulnerablePower>(ctx, target, DynamicVarSetExtensions.Power<VulnerablePower>(((RelicModel)this).DynamicVars).BaseValue, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
			}
		}
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
			((RelicModel)this).Status = (RelicStatus)((OverflowEffectsPlayed % intValue == intValue - 1) ? 1 : 0);
		}
		((RelicModel)this).InvokeDisplayAmountChanged();
	}

	public override Task BeforeCombatStart()
	{
		OverflowEffectsPlayed = 0;
		((RelicModel)this).Status = (RelicStatus)0;
		return Task.CompletedTask;
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
