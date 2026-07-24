using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActsFromThePast.Acts;
using ActsFromThePast.Acts.TheBeyond;
using ActsFromThePast.Acts.TheCity;
using ActsFromThePast.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Patches.Content;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Patches.Events;

public class ShrinePatches
{
	[HarmonyPatch(typeof(ActModel), "GenerateRooms")]
	[HarmonyPriority(200)]
	public static class EventPoolPatch
	{
		private static readonly FieldInfo RoomsField = typeof(ActModel).GetField("_rooms", BindingFlags.Instance | BindingFlags.NonPublic);

		private static readonly Assembly ModAssembly = typeof(EventPoolPatch).Assembly;

		private static readonly HashSet<EventModel> ModSharedEvents = new HashSet<EventModel>((IEnumerable<EventModel>)CustomContentDictionary.SharedCustomEvents.Where((CustomEventModel e) => ((object)e).GetType().Assembly == ModAssembly));

		private const float ShrineChance = 0.25f;

		private static bool IsLegacyAct(ActModel act)
		{
			if (act is ExordiumAct || act is TheCityAct || act is TheBeyondAct)
			{
				return true;
			}
			return false;
		}

		private static bool IsModSharedEvent(EventModel e)
		{
			return ModSharedEvents.Contains(e);
		}

		private static bool IsBaseGameSharedEvent(EventModel e)
		{
			return ModelDb.AllSharedEvents.Contains(e) && !IsModSharedEvent(e);
		}

		private static int GetActNumber(ActModel act)
		{
			if (1 == 0)
			{
			}
			int result;
			if (!(act is Overgrowth) && !(act is Underdocks))
			{
				if (!(act is Hive))
				{
					if (!(act is Glory))
					{
						CustomActModel val = (CustomActModel)(object)((act is CustomActModel) ? act : null);
						result = ((val == null) ? (-1) : val.ActNumber);
					}
					else
					{
						result = 3;
					}
				}
				else
				{
					result = 2;
				}
			}
			else
			{
				result = 1;
			}
			if (1 == 0)
			{
			}
			return result;
		}

		public static void Postfix(ActModel __instance, Rng rng)
		{
			object? obj = RoomsField?.GetValue(__instance);
			RoomSet val = (RoomSet)((obj is RoomSet) ? obj : null);
			if (val == null)
			{
				return;
			}
			if (IsLegacyAct(__instance) && !ActsFromThePastConfig.AllowNonLegacySharedEventsInLegacyActs)
			{
				val.events.RemoveAll((EventModel e) => IsBaseGameSharedEvent(e));
			}
			if (!IsLegacyAct(__instance) && !ActsFromThePastConfig.AllowLegacySharedEventsInNonLegacyActs)
			{
				val.events.RemoveAll((EventModel e) => IsModSharedEvent(e));
			}
			int actNumber = GetActNumber(__instance);
			if (actNumber >= 0)
			{
				val.events.RemoveAll((EventModel e) => e is IActRestricted actRestricted && !actRestricted.AllowedActIndices.Contains(actNumber));
			}
			List<EventModel> list = val.events.Where((EventModel e) => e is IShrineEvent).ToList();
			if (list.Count == 0)
			{
				return;
			}
			List<EventModel> list2 = val.events.Where((EventModel e) => !(e is IShrineEvent)).ToList();
			List<EventModel> list3 = new List<EventModel>();
			int num = 0;
			int num2 = 0;
			int num3 = list.Count + list2.Count;
			for (int num4 = 0; num4 < num3; num4++)
			{
				if (rng.NextFloat(1f) < 0.25f && num < list.Count)
				{
					list3.Add(list[num++]);
				}
				else if (num2 < list2.Count)
				{
					list3.Add(list2[num2++]);
				}
				else
				{
					list3.Add(list[num++]);
				}
			}
			val.events.Clear();
			val.events.AddRange(list3);
		}
	}

	[HarmonyPatch(typeof(RoomSet), "EnsureNextEventIsValid")]
	public static class RepeatableShrineValidityPatch
	{
		private static readonly FieldInfo VisitedEventIdsField = typeof(RunState).GetField("_visitedEventIds", BindingFlags.Instance | BindingFlags.NonPublic);

		[ThreadStatic]
		private static List<ModelId>? _temporarilyRemoved;

		public static void Prefix(RoomSet __instance, RunState runState)
		{
			_temporarilyRemoved = null;
			if (__instance.events.Count == 0 || !(VisitedEventIdsField?.GetValue(runState) is HashSet<ModelId> hashSet))
			{
				return;
			}
			foreach (EventModel @event in __instance.events)
			{
				if (@event is IShrineEvent { IsOneTimeEvent: false } && hashSet.Contains(((AbstractModel)@event).Id))
				{
					if (_temporarilyRemoved == null)
					{
						_temporarilyRemoved = new List<ModelId>();
					}
					_temporarilyRemoved.Add(((AbstractModel)@event).Id);
				}
			}
			if (_temporarilyRemoved == null)
			{
				return;
			}
			foreach (ModelId item in _temporarilyRemoved)
			{
				hashSet.Remove(item);
			}
		}

		public static void Postfix(RunState runState)
		{
			if (_temporarilyRemoved == null || !(VisitedEventIdsField?.GetValue(runState) is HashSet<ModelId> hashSet))
			{
				return;
			}
			foreach (ModelId item in _temporarilyRemoved)
			{
				hashSet.Add(item);
			}
			_temporarilyRemoved = null;
		}
	}
}
