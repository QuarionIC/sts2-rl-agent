using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dolso;
using Godot.Bridge;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;

namespace Act4Heart;

[ModInitializer("Initialize")]
internal static class ModMain
{
	internal static Config current_config => ConfigSynchronizer.current_config;

	internal static ConfigSynchronizer? config_synchronizer => ConfigSynchronizer.instance;

	internal static ConfigReader config_reader => ConfigSynchronizer.config_reader;

	private static void Initialize()
	{
		int num = ConfigSynchronizer.Startup();
		ScriptManagerBridge.LookupScriptsInAssembly(typeof(ModMain).Assembly);
		int value = num | HookAttribute.ScanAndApply();
		ModsCompat();
		HookAttribute.ThrowIfHookFailed(value);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ModsCompat()
	{
		if (ModManager._mods.Any((Mod a) => a.manifest?.id == "BaseLib" && (int)a.state == 1))
		{
			HookManager.harm.CreateProcessor((MethodBase)HookManager.GetMethod(typeof(EncounterModel), "GetBackgroundAssets")).Patch();
		}
	}
}
