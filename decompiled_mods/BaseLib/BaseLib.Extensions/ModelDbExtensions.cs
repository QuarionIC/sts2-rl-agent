using System.Runtime.CompilerServices;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Extensions;

public static class ModelDbExtensions
{
	[SpecialName]
	public sealed class _003CG_003E_0024FDD01F76FF056B5039892AFD9DDA2EC2
	{
		[SpecialName]
		public static class _003CM_003E_00246798FB08C505FED58E7C0DFE03AB7AE7
		{
		}

		[ExtensionMarker("<M>$6798FB08C505FED58E7C0DFE03AB7AE7")]
		public static T CardModifier<T>() where T : CardModifier
		{
			throw null;
		}

		[ExtensionMarker("<M>$6798FB08C505FED58E7C0DFE03AB7AE7")]
		public static T CardModifier<T>(bool mutableClone = true) where T : CardModifier
		{
			throw null;
		}
	}

	public static T CardModifier<T>() where T : notnull, CardModifier
	{
		return CardModifier<T>(mutableClone: false);
	}

	public static T CardModifier<T>(bool mutableClone = true) where T : notnull, CardModifier
	{
		T val = ModelDb.Get<T>();
		if (mutableClone)
		{
			return (T)(CardModifier)(object)((AbstractModel)val).MutableClone();
		}
		return val;
	}
}
