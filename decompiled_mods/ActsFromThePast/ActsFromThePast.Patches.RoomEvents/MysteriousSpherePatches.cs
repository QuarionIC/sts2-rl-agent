using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.RoomEvents;

public class MysteriousSpherePatches
{
	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	public class VisualsPatch
	{
		public static void Postfix(NCombatRoom __instance)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Invalid comparison between Unknown and I4
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Expected O, but got Unknown
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Unknown result type (might be due to invalid IL or missing references)
			//IL_0188: Unknown result type (might be due to invalid IL or missing references)
			//IL_018e: Unknown result type (might be due to invalid IL or missing references)
			if ((int)__instance.Mode != 2)
			{
				return;
			}
			object? obj = typeof(NCombatRoom).GetField("_visuals", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);
			ICombatRoomVisuals val = (ICombatRoomVisuals)((obj is ICombatRoomVisuals) ? obj : null);
			if (((val != null) ? val.Encounter : null) == null || !SphereEncounters.Contains(((object)val.Encounter).GetType()))
			{
				return;
			}
			Texture2D val2 = GD.Load<Texture2D>("res://images/event_extras/bgSphere.png");
			if (val2 != null)
			{
				TextureRect val3 = new TextureRect();
				((Node)val3).Name = StringName.op_Implicit("EventBgOverlay");
				val3.Texture = val2;
				val3.StretchMode = (StretchModeEnum)0;
				((Control)val3).MouseFilter = (MouseFilterEnum)2;
				((Control)val3).SetAnchorsPreset((LayoutPreset)15, false);
				((Control)val3).OffsetTop = 40f;
				((CanvasItem)val3).ZIndex = -11;
				((Node)__instance).AddChild((Node)(object)val3, false, (InternalMode)0);
			}
			Control sceneContainer = ((Node)__instance).GetNodeOrNull<Control>(NodePath.op_Implicit("%CombatSceneContainer"));
			if (sceneContainer == null)
			{
				return;
			}
			Vector2 basePos = sceneContainer.Position;
			TextureRect bgNode = ((Node)__instance).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("EventBgOverlay"));
			((GodotObject)sceneContainer).Connect(StringName.op_Implicit("item_rect_changed"), Callable.From((Action)delegate
			{
				//IL_0025: Unknown result type (might be due to invalid IL or missing references)
				//IL_002b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0030: Unknown result type (might be due to invalid IL or missing references)
				//IL_0035: Unknown result type (might be due to invalid IL or missing references)
				//IL_005c: Unknown result type (might be due to invalid IL or missing references)
				//IL_006f: Unknown result type (might be due to invalid IL or missing references)
				if (GodotObject.IsInstanceValid((GodotObject)(object)sceneContainer))
				{
					Vector2 val4 = sceneContainer.Position - basePos;
					if (bgNode != null && GodotObject.IsInstanceValid((GodotObject)(object)bgNode))
					{
						((Control)bgNode).OffsetTop = 40f + val4.Y;
						((Control)bgNode).OffsetLeft = val4.X;
					}
				}
			}), 0u);
		}
	}

	[HarmonyPatch(typeof(RewardsSet), "WithRewardsFromRoom")]
	public class RewardsPatch
	{
		public static void Postfix(RewardsSet __result, AbstractRoom room)
		{
			CombatRoom val = (CombatRoom)(object)((room is CombatRoom) ? room : null);
			if (val == null || !SphereEncounters.Contains(((object)val.Encounter).GetType()))
			{
				return;
			}
			HashSet<Reward> extraRewards = val.ExtraRewards.Values.SelectMany((List<Reward> list) => list).ToHashSet();
			__result.Rewards.RemoveAll(delegate(Reward r)
			{
				bool flag = !extraRewards.Contains(r);
				bool flag2 = flag;
				if (flag2)
				{
					bool flag3 = ((r is GoldReward || r is RelicReward) ? true : false);
					flag2 = flag3;
				}
				return flag2;
			});
		}
	}

	private static readonly HashSet<Type> SphereEncounters = new HashSet<Type> { typeof(TwoOrbWalkersEvent) };

	public static void SwapToOpenSphere()
	{
		NEventRoom instance = NEventRoom.Instance;
		NEventLayout obj = ((instance != null) ? instance.Layout : null);
		NCombatEventLayout val = (NCombatEventLayout)(object)((obj is NCombatEventLayout) ? obj : null);
		if (val == null)
		{
			return;
		}
		NCombatRoom embeddedCombatRoom = val.EmbeddedCombatRoom;
		if (embeddedCombatRoom == null)
		{
			return;
		}
		TextureRect nodeOrNull = ((Node)embeddedCombatRoom).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("EventBgOverlay"));
		if (nodeOrNull != null)
		{
			Texture2D val2 = GD.Load<Texture2D>("res://images/event_extras/bgSphereOpen.png");
			if (val2 != null)
			{
				nodeOrNull.Texture = val2;
			}
		}
	}
}
