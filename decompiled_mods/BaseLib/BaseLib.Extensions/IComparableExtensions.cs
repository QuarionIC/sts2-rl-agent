using System;
using System.Runtime.CompilerServices;

namespace BaseLib.Extensions;

public static class IComparableExtensions
{
	[SpecialName]
	public sealed class _003CG_003E_0024E725505F0A27D63E453277C0C551DC0B<_0024T0> where _0024T0 : notnull, IComparable<_0024T0>
	{
		[SpecialName]
		public static class _003CM_003E_0024A77B06AA0300846ABE8D6680B3688E2B
		{
		}

		[ExtensionMarker("<M>$A77B06AA0300846ABE8D6680B3688E2B")]
		public bool GreaterThan(_0024T0 b)
		{
			throw null;
		}

		[ExtensionMarker("<M>$A77B06AA0300846ABE8D6680B3688E2B")]
		public bool GreaterThanOrEqual(_0024T0 b)
		{
			throw null;
		}

		[ExtensionMarker("<M>$A77B06AA0300846ABE8D6680B3688E2B")]
		public bool LessThan(_0024T0 b)
		{
			throw null;
		}

		[ExtensionMarker("<M>$A77B06AA0300846ABE8D6680B3688E2B")]
		public bool LessThanOrEqual(_0024T0 b)
		{
			throw null;
		}
	}

	public static bool GreaterThan<T>(this T a, T b) where T : notnull, IComparable<T>
	{
		return a.CompareTo(b) > 0;
	}

	public static bool GreaterThanOrEqual<T>(this T a, T b) where T : notnull, IComparable<T>
	{
		return a.CompareTo(b) >= 0;
	}

	public static bool LessThan<T>(this T a, T b) where T : notnull, IComparable<T>
	{
		return a.CompareTo(b) < 0;
	}

	public static bool LessThanOrEqual<T>(this T a, T b) where T : notnull, IComparable<T>
	{
		return a.CompareTo(b) <= 0;
	}
}
