using System;

namespace BaseLib.Utils;

public class ReadonlySpireField<TKey, TVal> : NotNullSpireField<TKey, TVal> where TKey : class where TVal : class
{
	public ReadonlySpireField(Func<TVal> defaultVal)
		: base(defaultVal)
	{
	}

	public ReadonlySpireField(Func<TKey, TVal> defaultVal)
		: base(defaultVal)
	{
	}

	[Obsolete("ReadonlySpireField cannot be set; exception will be thrown.")]
	public new void Set(TKey obj, TVal? val)
	{
		throw new InvalidOperationException("The value of a ReadonlySpireField should not be set. If possible, modify its current value instead.");
	}
}
