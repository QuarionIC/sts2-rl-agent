using BaseLib.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace ActsFromThePast;

[ModInitializer("Initialize")]
public class ActsFromThePastInitializer
{
	public static void Initialize()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Expected O, but got Unknown
		Harmony val = new Harmony("actsfromthepast.actsfromthepast");
		val.PatchAll(typeof(ActsFromThePastInitializer).Assembly);
		ModConfigRegistry.Register("ActsFromThePast", (ModConfig)(object)new ActsFromThePastConfig());
	}
}
