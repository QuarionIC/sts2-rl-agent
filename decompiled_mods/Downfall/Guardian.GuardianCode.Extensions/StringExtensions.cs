using System.IO;

namespace Guardian.GuardianCode.Extensions;

internal static class StringExtensions
{
	public static string GemPath(this string path)
	{
		return Path.Join("Guardian", "images", "gems", path);
	}
}
