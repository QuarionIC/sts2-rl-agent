using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace BaseLib.Abstracts;

public class CustomResourceCost<T> : ICustomResourceCost where T : CustomResource, new()
{
	private readonly CardModel _card;

	private int _base;

	private int _capturedXValue;

	private List<LocalCostModifier> _localModifiers = new List<LocalCostModifier>();

	private bool? _forceOptional;

	public int Canonical { get; }

	public bool CostsX { get; }

	public bool WasJustUpgraded { get; set; }

	public bool HasLocalModifiers => _localModifiers.Count > 0;

	public int CapturedXValue
	{
		get
		{
			if (!CostsX)
			{
				throw new InvalidOperationException("Only X-cost cards have a captured value.");
			}
			return _capturedXValue;
		}
		set
		{
			((AbstractModel)_card).AssertMutable();
			if (!CostsX)
			{
				throw new InvalidOperationException("Only X-cost cards have a captured value.");
			}
			_capturedXValue = value;
		}
	}

	public virtual bool IsOptional(Player? p)
	{
		PlayerCombatState val = ((p != null) ? p.PlayerCombatState : null);
		if (val == null)
		{
			return false;
		}
		return _forceOptional ?? CustomResources<T>.Get(val).IsDefaultOptional;
	}

	public CustomResourceCost(CardModel card, int canonicalCost, bool costsX = false)
	{
		_card = card;
		CostsX = costsX;
		Canonical = ((!CostsX) ? canonicalCost : 0);
		_base = Canonical;
	}

	public void MakeOptional(bool optional = true)
	{
		_forceOptional = optional;
	}

	public int GetWithModifiers(CostModifiers modifiers)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		int num = _base;
		if (((AbstractModel)_card).IsCanonical || _base < 0 || CostsX)
		{
			return num;
		}
		if (((Enum)modifiers).HasFlag((Enum)(object)(CostModifiers)2))
		{
			foreach (LocalCostModifier localModifier in _localModifiers)
			{
				num = localModifier.Modify(num);
			}
		}
		if (((Enum)modifiers).HasFlag((Enum)(object)(CostModifiers)4) && _card.CombatState != null)
		{
			num = (int)Hook.ModifyEnergyCostInCombat(_card.CombatState, _card, (decimal)num);
		}
		return Math.Max(0, num);
	}

	public int ResolveXValue()
	{
		if (!CostsX)
		{
			throw new InvalidOperationException($"This cost of type {GetType()} is not an X-cost.");
		}
		return Hook.ModifyXValue(_card.CombatState, _card, CapturedXValue);
	}

	public int GetAmountToSpend()
	{
		if (!CostsX)
		{
			return Math.Max(0, GetWithModifiers((CostModifiers)(-1)));
		}
		PlayerCombatState playerCombatState = _card.Owner.PlayerCombatState;
		if (playerCombatState == null)
		{
			return 0;
		}
		return CustomResources<T>.Get(playerCombatState).Amount;
	}

	public int GetResolved()
	{
		if (!CostsX)
		{
			return Math.Max(0, GetWithModifiers((CostModifiers)(-1)));
		}
		return CapturedXValue;
	}

	public UnplayableReason ResourceCheck(PlayerCombatState combatState, CardModel card)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		if (IsOptional(combatState._player))
		{
			return (UnplayableReason)0;
		}
		T val = CustomResources<T>.Get(combatState);
		int withModifiers = GetWithModifiers((CostModifiers)(-1));
		if (!val.CanAfford(card, withModifiers))
		{
			return val.UnplayableReason;
		}
		return (UnplayableReason)0;
	}

	public void SetUntilPlayed(int cost, bool reduceOnly = false)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		if (cost != 0 || Canonical >= 0)
		{
			_localModifiers.Add(new LocalCostModifier(cost, (LocalCostType)1, (LocalCostModifierExpiration)4, reduceOnly));
		}
	}

	public void SetThisTurnOrUntilPlayed(int cost, bool reduceOnly = false)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		if (cost != 0 || Canonical >= 0)
		{
			_localModifiers.Add(new LocalCostModifier(cost, (LocalCostType)1, (LocalCostModifierExpiration)6, reduceOnly));
		}
	}

	public void SetThisTurn(int cost, bool reduceOnly = false)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		if (cost != 0 || Canonical >= 0)
		{
			_localModifiers.Add(new LocalCostModifier(cost, (LocalCostType)1, (LocalCostModifierExpiration)2, reduceOnly));
		}
	}

	public void SetThisCombat(int cost, bool reduceOnly = false)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		if (cost != 0 || Canonical >= 0)
		{
			_localModifiers.Add(new LocalCostModifier(cost, (LocalCostType)1, (LocalCostModifierExpiration)0, reduceOnly));
		}
	}

	public void AddUntilPlayed(int amount, bool reduceOnly = false)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		if (amount != 0)
		{
			_localModifiers.Add(new LocalCostModifier(amount, (LocalCostType)2, (LocalCostModifierExpiration)4, reduceOnly));
		}
	}

	public void AddThisTurnOrUntilPlayed(int amount, bool reduceOnly = false)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		if (amount != 0)
		{
			_localModifiers.Add(new LocalCostModifier(amount, (LocalCostType)2, (LocalCostModifierExpiration)6, reduceOnly));
		}
	}

	public void AddThisTurn(int amount, bool reduceOnly = false)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		if (amount != 0)
		{
			_localModifiers.Add(new LocalCostModifier(amount, (LocalCostType)2, (LocalCostModifierExpiration)2, reduceOnly));
		}
	}

	public void AddThisCombat(int amount, bool reduceOnly = false)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		if (amount != 0)
		{
			_localModifiers.Add(new LocalCostModifier(amount, (LocalCostType)2, (LocalCostModifierExpiration)0, reduceOnly));
		}
	}

	public bool EndOfTurnCleanup()
	{
		((AbstractModel)_card).AssertMutable();
		return _localModifiers.RemoveAll((LocalCostModifier m) => ((Enum)m.Expiration).HasFlag((Enum)(object)(LocalCostModifierExpiration)2)) > 0;
	}

	public bool AfterCardPlayedCleanup()
	{
		((AbstractModel)_card).AssertMutable();
		return _localModifiers.RemoveAll((LocalCostModifier m) => ((Enum)m.Expiration).HasFlag((Enum)(object)(LocalCostModifierExpiration)4)) > 0;
	}

	public void UpgradeCostBy(int addend)
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Invalid comparison between Unknown and I4
		((AbstractModel)_card).AssertMutable();
		if (CostsX || addend == 0)
		{
			return;
		}
		int num = _base;
		int num2 = Math.Max(_base + addend, 0);
		WasJustUpgraded = true;
		if (num2 < num)
		{
			foreach (LocalCostModifier localModifier in _localModifiers)
			{
				if ((int)localModifier.Type == 1 && localModifier.Amount > num2)
				{
					localModifier.Amount = num2;
				}
			}
		}
		SetCustomBaseCost(num2);
	}

	public void FinalizeUpgrade()
	{
		((AbstractModel)_card).AssertMutable();
		WasJustUpgraded = false;
	}

	public void ResetForDowngrade()
	{
		((AbstractModel)_card).AssertMutable();
		_base = Canonical;
		_card.InvokeEnergyCostChanged();
	}

	public void SetCustomBaseCost(int newBaseCost)
	{
		((AbstractModel)_card).AssertMutable();
		_base = newBaseCost;
		_card.InvokeEnergyCostChanged();
	}

	public CustomResourceCost<T> Clone(CardModel newCard)
	{
		List<LocalCostModifier> localModifiers = _localModifiers.Select((LocalCostModifier m) => m.Clone()).ToList();
		return new CustomResourceCost<T>(newCard, CustomResources<T>.CanonicalCost(newCard), newCard.EnergyCost.CostsX)
		{
			_base = _base,
			_capturedXValue = _capturedXValue,
			WasJustUpgraded = WasJustUpgraded,
			_forceOptional = _forceOptional,
			_localModifiers = localModifiers
		};
	}

	public void UpdateCostVisuals(NCard nCard, PileType pileType)
	{
	}
}
