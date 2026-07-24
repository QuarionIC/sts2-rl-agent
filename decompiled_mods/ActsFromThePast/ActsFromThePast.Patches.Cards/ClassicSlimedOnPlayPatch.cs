using System.Threading.Tasks;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast.Patches.Cards;

[HarmonyPatch(typeof(Slimed), "OnPlay")]
public class ClassicSlimedOnPlayPatch
{
	public static bool Prefix(Slimed __instance, PlayerChoiceContext choiceContext, CardPlay cardPlay, ref Task __result)
	{
		if (!ClassicSlimedTracker.IsClassicSlimed.Get((CardModel)(object)__instance))
		{
			return true;
		}
		NGoopyImpactVfx val = NGoopyImpactVfx.Create(((CardModel)__instance).Owner.Creature);
		if (val != null)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)val);
			}
		}
		__result = Task.CompletedTask;
		return false;
	}
}
