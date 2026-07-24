using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Abstract;

public abstract class CardResource : CustomSingletonModel
{
	private readonly SpireField<Player, int> _current;

	public abstract string ResourceName { get; }

	public virtual Vector2 UiPosition => Vector2.One;

	public virtual Vector2 UiScale => Vector2.One;

	protected virtual bool ResetOnCombatStart => true;

	protected virtual bool ResetOnTurnStart => false;

	protected virtual bool InteractsWithEnergy => false;

	public event Action<Player, int>? Changed;

	protected CardResource()
		: base((HookType)1)
	{
		_current = new SpireField<Player, int>((Func<int>)(() => 0));
		CardResourceRegistry.Register(this);
	}

	public int Get(Player player)
	{
		return _current[player];
	}

	protected virtual void Set(Player player, int amount)
	{
		int num = Math.Max(0, amount);
		_current[player] = num;
		this.Changed?.Invoke(player, num);
	}

	public virtual void Gain(Player player, int amount)
	{
		Set(player, Get(player) + amount);
	}

	public virtual void Spend(Player player, int amount)
	{
		Set(player, Get(player) - amount);
	}

	public virtual bool CanAfford(Player player, int cost)
	{
		return Get(player) >= cost;
	}

	public virtual void Reset(Player player)
	{
		Set(player, 0);
	}

	public virtual Control? CreateCounter(Player player)
	{
		return null;
	}

	public override Task BeforeCombatStart()
	{
		if (!ResetOnCombatStart)
		{
			return Task.CompletedTask;
		}
		CombatState val = CombatManager.Instance.DebugOnlyGetState();
		if (val == null)
		{
			return Task.CompletedTask;
		}
		foreach (Player player in val.Players)
		{
			Reset(player);
		}
		return Task.CompletedTask;
	}

	public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		if (!ResetOnTurnStart)
		{
			return Task.CompletedTask;
		}
		foreach (Player player in combatState.Players)
		{
			Reset(player);
		}
		return Task.CompletedTask;
	}

	public virtual bool ShouldHandleSpending(CardModel card)
	{
		return InteractsWithEnergy;
	}

	public virtual bool ShouldHandleResourceCheck(CardModel card)
	{
		return InteractsWithEnergy;
	}

	public virtual bool UsesResourceExclusively(CardModel card)
	{
		return false;
	}

	public virtual (int energySpent, int starsSpent) HandleSpending(CardModel card)
	{
		return (energySpent: 0, starsSpent: 0);
	}

	public virtual (bool hasResources, UnplayableReason reason) CheckResources(CardModel card)
	{
		return (hasResources: true, reason: (UnplayableReason)0);
	}
}
