using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Act4Heart.Keys;
using Act4Heart.Powers;
using Dolso;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;

namespace Act4Heart.Hooks;

[Hook]
internal static class GeneralHooks
{
	[HookBefore(typeof(PowerModel), "get_PackedIconPath")]
	private static bool GetPowerIcon_Before_PackedIconPath(PowerModel __instance, ref string __result)
	{
		if (!(__instance is A4hPowerModel a4hPowerModel))
		{
			return true;
		}
		__result = a4hPowerModel.icon_path;
		return false;
	}

	[HookAfter(typeof(ModelDb), "get_AllRelicPools")]
	private static void InsertKeyRelicPool(ref IEnumerable<RelicPoolModel> __result)
	{
		__result = __result.Concat(new _003C_003Ez__ReadOnlySingleElementList<RelicPoolModel>((RelicPoolModel)(object)ModelDb.RelicPool<KeyRelicPool>()));
	}

	[HookBefore(typeof(RunManager), "EnterNextAct")]
	private static bool CheckKeys_Before_EnterNextAct(RunManager __instance, ref Task __result)
	{
		if (!ModMain.current_config.keys_enable)
		{
			return true;
		}
		try
		{
			return CheckKeysBeforeAdvanceAct(__instance, ref __result);
		}
		catch (Exception data)
		{
			log.error(data);
			return true;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool CheckKeysBeforeAdvanceAct(RunManager __instance, ref Task __result)
	{
		RunState state = __instance.State;
		if (state != null && state.CurrentActIndex < state.Acts.Count - 1 && state.Acts[state.CurrentActIndex + 1] is TheEnding)
		{
			AbstractRoom currentRoom = state.CurrentRoom;
			AbstractRoom obj = ((currentRoom is EventRoom) ? currentRoom : null);
			if ((obj == null || !obj.IsVictoryRoom) && !KeyDoor.EveryoneHasAllKeys())
			{
				__instance.ActChangeSynchronizer._lastTransitioningActIndex = state.CurrentActIndex - 1;
				AbstractRoom currentRoom2 = state.CurrentRoom;
				AbstractRoom obj2 = ((currentRoom2 is EventRoom) ? currentRoom2 : null);
				if (((obj2 != null) ? ((EventRoom)obj2).LocalMutableEvent : null) is KeyDoor keyDoor)
				{
					if (keyDoor.state == 1)
					{
						return true;
					}
					__result = EnterEvent<TheArchitect>(__instance);
					return false;
				}
				__result = EnterEvent<KeyDoor>(__instance);
				return false;
			}
		}
		return true;
	}

	private static async Task EnterEvent<T>(RunManager self) where T : EventModel
	{
		NetLoadingHandle val = new NetLoadingHandle(self.NetService);
		try
		{
			if (TestMode.IsOff)
			{
				await NGame.Instance.Transition.RoomFadeOut();
			}
			self.ClearScreens();
			await self.EnterRoom((AbstractRoom)new EventRoom((EventModel)(object)ModelDb.Event<T>()));
			await self.FadeIn(true);
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}
}
