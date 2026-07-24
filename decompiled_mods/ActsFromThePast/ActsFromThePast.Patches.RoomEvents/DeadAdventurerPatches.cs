using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.RoomEvents;

public class DeadAdventurerPatches
{
	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	public class VisualsPatch
	{
		public static void Postfix(NCombatRoom __instance)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Invalid comparison between Unknown and I4
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Expected O, but got Unknown
			//IL_0191: Unknown result type (might be due to invalid IL or missing references)
			//IL_0196: Unknown result type (might be due to invalid IL or missing references)
			//IL_01be: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
			if ((int)__instance.Mode != 2)
			{
				return;
			}
			object? obj = typeof(NCombatRoom).GetField("_visuals", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);
			ICombatRoomVisuals val = (ICombatRoomVisuals)((obj is ICombatRoomVisuals) ? obj : null);
			if (((val != null) ? val.Encounter : null) == null || !DeadAdventurerEncounters.Contains(((object)val.Encounter).GetType()))
			{
				return;
			}
			Control nodeOrNull = ((Node)__instance).GetNodeOrNull<Control>(NodePath.op_Implicit("%EnemyContainer"));
			if (nodeOrNull != null)
			{
				((CanvasItem)nodeOrNull).Visible = false;
			}
			Texture2D val2 = GD.Load<Texture2D>("res://images/event_extras/dead_adventurer.png");
			if (val2 == null)
			{
				return;
			}
			TextureRect overlay = new TextureRect();
			((Node)overlay).Name = StringName.op_Implicit("EventImageOverlay");
			overlay.Texture = val2;
			overlay.StretchMode = (StretchModeEnum)0;
			((Control)overlay).MouseFilter = (MouseFilterEnum)2;
			((Control)overlay).SetAnchorsPreset((LayoutPreset)15, false);
			((Control)overlay).OffsetTop = 40f;
			((CanvasItem)overlay).ZIndex = -11;
			((Node)__instance).AddChild((Node)(object)overlay, false, (InternalMode)0);
			Control sceneContainer = ((Node)__instance).GetNodeOrNull<Control>(NodePath.op_Implicit("%CombatSceneContainer"));
			if (sceneContainer == null)
			{
				return;
			}
			Vector2 basePos = sceneContainer.Position;
			((GodotObject)sceneContainer).Connect(StringName.op_Implicit("item_rect_changed"), Callable.From((Action)delegate
			{
				//IL_0036: Unknown result type (might be due to invalid IL or missing references)
				//IL_003c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0041: Unknown result type (might be due to invalid IL or missing references)
				//IL_0046: Unknown result type (might be due to invalid IL or missing references)
				//IL_0057: Unknown result type (might be due to invalid IL or missing references)
				//IL_006f: Unknown result type (might be due to invalid IL or missing references)
				if (GodotObject.IsInstanceValid((GodotObject)(object)overlay) && GodotObject.IsInstanceValid((GodotObject)(object)sceneContainer))
				{
					Vector2 val3 = sceneContainer.Position - basePos;
					((Control)overlay).OffsetTop = 40f + val3.Y;
					((Control)overlay).OffsetLeft = val3.X;
				}
			}), 0u);
		}
	}

	[HarmonyPatch(typeof(RewardsSet), "WithRewardsFromRoom")]
	public class DeadAdventurerRewardsPatch
	{
		public static void Postfix(RewardsSet __result, AbstractRoom room)
		{
			CombatRoom val = (CombatRoom)(object)((room is CombatRoom) ? room : null);
			if (val == null || !DeadAdventurerEncounters.Contains(((object)val.Encounter).GetType()))
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
					bool flag3 = ((r is GoldReward || r is CardReward || r is RelicReward) ? true : false);
					flag2 = flag3;
				}
				return flag2;
			});
		}
	}

	private static readonly HashSet<Type> DeadAdventurerEncounters = new HashSet<Type>
	{
		typeof(DeadAdventurerSentries),
		typeof(DeadAdventurerGremlinNob),
		typeof(DeadAdventurerLagavulin)
	};

	public static void RevealEnemies()
	{
		NEventRoom instance = NEventRoom.Instance;
		NEventLayout obj = ((instance != null) ? instance.Layout : null);
		NCombatEventLayout val = (NCombatEventLayout)(object)((obj is NCombatEventLayout) ? obj : null);
		if (val == null)
		{
			return;
		}
		NCombatRoom embeddedCombatRoom = val.EmbeddedCombatRoom;
		if (embeddedCombatRoom != null)
		{
			Control nodeOrNull = ((Node)embeddedCombatRoom).GetNodeOrNull<Control>(NodePath.op_Implicit("%EnemyContainer"));
			if (nodeOrNull != null)
			{
				((CanvasItem)nodeOrNull).Visible = true;
			}
		}
	}
}
