using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Hooks;
using BaseLib.Patches.UI;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Abstracts;

public abstract class CustomResource(string id)
{
	[CompilerGenerated]
	private int _003CAmount_003Ek__BackingField;

	public string Id { get; protected set; } = id;

	public virtual bool ApplySharedModification => true;

	public virtual bool IsDefaultOptional => false;

	public virtual int Amount
	{
		[CompilerGenerated]
		get
		{
			return _003CAmount_003Ek__BackingField;
		}
		set
		{
			if (value != _003CAmount_003Ek__BackingField)
			{
				int arg = _003CAmount_003Ek__BackingField;
				_003CAmount_003Ek__BackingField = value;
				this.AmountChanged?.Invoke(arg, _003CAmount_003Ek__BackingField);
			}
		}
	}

	public virtual UnplayableReason UnplayableReason => (UnplayableReason)16;

	public event Action<int, int>? AmountChanged;

	public abstract ICustomCostVisualsHandler CostVisualsHandler();

	public abstract ICustomResourceVisualsHandler ResourceVisualsHandler();

	public virtual void PrepForCombat()
	{
	}

	public virtual async Task<bool> Spend<T>(ICombatState combatState, AbstractModel? spender, int amount, bool optional) where T : CustomResource
	{
		if (!(this is T resource))
		{
			throw new ArgumentException("Attempted to call Spend on a resource with a generic type that does not match the resource.");
		}
		if (amount > Amount)
		{
			if (optional)
			{
				return false;
			}
			BaseLibMain.Logger.Warn($"Attempted to spend secondary resource {typeof(T).Name} with insufficient amount; Current: {Amount} | Required: {amount} ", 1);
			amount = Amount;
		}
		ModifyAmount(-amount);
		await BaseLibHooks.AfterSpendCustomResource(combatState, resource, spender, amount);
		return true;
	}

	public void ModifyAmount(int change)
	{
		Amount += change;
	}

	public virtual bool CanAfford(CardModel card, int cost)
	{
		return Amount >= cost;
	}
}
