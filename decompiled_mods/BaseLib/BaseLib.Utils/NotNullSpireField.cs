using System;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public class NotNullSpireField<TKey, TVal> : ICloneableField where TKey : class where TVal : class
{
	private readonly ConditionalWeakTable<TKey, TVal> _table = new ConditionalWeakTable<TKey, TVal>();

	private readonly Func<TKey, TVal> _defaultVal;

	private Action<TKey, TKey, TVal>? _cloneFunc;

	public bool ShouldClone => _cloneFunc != null;

	public TVal this[TKey obj]
	{
		get
		{
			return Get(obj);
		}
		set
		{
			Set(obj, value);
		}
	}

	public NotNullSpireField(Func<TVal> defaultVal)
	{
		_defaultVal = (TKey _) => defaultVal();
	}

	public NotNullSpireField(Func<TKey, TVal> defaultVal)
	{
		_defaultVal = defaultVal;
	}

	public NotNullSpireField<TKey, TVal> CopyOnClone(Action<TKey, TKey, TVal>? cloneVal = null)
	{
		if (!typeof(TKey).IsAssignableTo(typeof(AbstractModel)))
		{
			throw new InvalidOperationException("Cannot enable CopyOnClone for SpireField on type " + typeof(TKey).Name + "; only valid for SpireFields attached to AbstractModel types.");
		}
		_cloneFunc = cloneVal ?? ((Action<TKey, TKey, TVal>)delegate(TKey _, TKey dst, TVal val)
		{
			Set(dst, val);
		});
		return this;
	}

	public void Clone(AbstractModel src, AbstractModel dst)
	{
		if (!(src is TKey val) || !(dst is TKey arg))
		{
			throw new ArgumentException($"Unable to clone SpireField on type {typeof(TKey).Name} from {((object)src).GetType().Name} to {((object)dst).GetType().Name}.");
		}
		if (ShouldClone)
		{
			_cloneFunc(val, arg, Get(val));
		}
	}

	public TVal Get(TKey obj)
	{
		if (_table.TryGetValue(obj, out var value))
		{
			return value;
		}
		TVal val = _defaultVal(obj);
		_table.Add(obj, val);
		if (ShouldClone && !typeof(TVal).IsValueType)
		{
			AbstractModel val2 = (AbstractModel)(object)((obj is AbstractModel) ? obj : null);
			if (val2 != null)
			{
				ICloneableField.AddClonedField(val2, this);
			}
		}
		return val;
	}

	public void Set(TKey obj, TVal val)
	{
		_table.AddOrUpdate(obj, val);
		if (ShouldClone)
		{
			AbstractModel val2 = (AbstractModel)(object)((obj is AbstractModel) ? obj : null);
			if (val2 != null)
			{
				ICloneableField.AddClonedField(val2, this);
			}
		}
	}
}
