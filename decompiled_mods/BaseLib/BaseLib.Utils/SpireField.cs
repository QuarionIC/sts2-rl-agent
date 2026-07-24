using System;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public class SpireField<TKey, TVal> : ICloneableField where TKey : class
{
	private readonly ConditionalWeakTable<TKey, object?> _table = new ConditionalWeakTable<TKey, object>();

	private readonly Func<TKey, TVal?> _defaultVal;

	private Action<TKey, TKey, TVal?>? _cloneFunc;

	public bool ShouldClone => _cloneFunc != null;

	public TVal? this[TKey obj]
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

	public SpireField(Func<TVal?> defaultVal)
	{
		_defaultVal = (TKey _) => defaultVal();
	}

	public SpireField(Func<TKey, TVal?> defaultVal)
	{
		_defaultVal = defaultVal;
	}

	public SpireField<TKey, TVal> CopyOnClone(Action<TKey, TKey, TVal?>? cloneVal = null)
	{
		if (!typeof(TKey).IsAssignableTo(typeof(AbstractModel)))
		{
			throw new InvalidOperationException("Cannot enable CopyOnClone for SpireField on type " + typeof(TKey).Name + "; only valid for SpireFields attached to AbstractModel types.");
		}
		_cloneFunc = cloneVal ?? ((Action<TKey, TKey, TVal>)delegate(TKey _, TKey dst, TVal? val)
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

	public TVal? Get(TKey obj)
	{
		if (_table.TryGetValue(obj, out object value))
		{
			return (TVal)value;
		}
		_table.Add(obj, value = _defaultVal(obj));
		if (ShouldClone && !typeof(TVal).IsValueType)
		{
			AbstractModel val = (AbstractModel)(object)((obj is AbstractModel) ? obj : null);
			if (val != null)
			{
				ICloneableField.AddClonedField(val, this);
			}
		}
		return (TVal)value;
	}

	public void Set(TKey obj, TVal? val)
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
