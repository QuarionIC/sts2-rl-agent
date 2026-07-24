using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes;

namespace BaseLib.Config;

[HarmonyPatch(typeof(NGame), "Quit")]
public static class NGameQuitModConfigSavePatch
{
	public static void Prefix()
	{
		BaseLibMain.Logger.Info("NGame.Quit(): saving all ModConfigs", 1);
		foreach (ModConfig item in ModConfigRegistry.GetAll())
		{
			item.Save();
		}
	}
}
