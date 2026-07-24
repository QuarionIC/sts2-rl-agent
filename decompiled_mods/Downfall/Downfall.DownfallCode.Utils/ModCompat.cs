using System;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Modding;

namespace Downfall.DownfallCode.Utils;

public class ModCompat
{
	public static async Task TryExecute(string modId, Func<Task> action)
	{
		if (ModManager.GetLoadedMods().Any((Mod m) => m.manifest?.id == modId))
		{
			await action();
		}
	}
}
