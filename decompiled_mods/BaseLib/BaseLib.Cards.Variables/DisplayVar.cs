using System;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Cards.Variables;

public class DisplayVar<T> : DynamicVar where T : class
{
	private T? _tOwner;

	private readonly Func<T, string> _displayText;

	public DisplayVar(string name, Func<T, string> displayText)
		: base(name, 0m)
	{
		_displayText = displayText;
	}

	public override void SetOwner(AbstractModel owner)
	{
		((DynamicVar)this).SetOwner(owner);
		_tOwner = owner as T;
	}

	public override string ToString()
	{
		if (_tOwner != null)
		{
			return _displayText(_tOwner);
		}
		return $"Owner of DisplayVar '' is wrong type [{((object)base._owner)?.GetType()}]";
	}
}
