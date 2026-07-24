using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BaseLib.Extensions;

public static class ImageHelperExtensions
{
	[SpecialName]
	public sealed class _003CG_003E_00240D62A6A42F1433B57F08660DC9964CD1
	{
		[SpecialName]
		public static class _003CM_003E_00242442245144B4237CF75488E7F778F1E4
		{
		}

		[ExtensionMarker("<M>$2442245144B4237CF75488E7F778F1E4")]
		public static string GetModImagePath(string innerPath, Type? type = null)
		{
			throw null;
		}
	}

	public static string GetModImagePath(string innerPath, Type? type = null)
	{
		return Path.Join("res://" + ((type != null) ? type.GetRootNamespace() : Assembly.GetCallingAssembly().GetName().Name), "images", innerPath);
	}
}
