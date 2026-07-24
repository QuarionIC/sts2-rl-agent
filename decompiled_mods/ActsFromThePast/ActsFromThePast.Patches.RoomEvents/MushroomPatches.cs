using System;
using System.Collections.Generic;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Events;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Patches.RoomEvents;

public class MushroomPatches
{
	[HarmonyPatch(typeof(NCombatRoom), "_Ready")]
	public class VisualsPatch
	{
		public static void Postfix(NCombatRoom __instance)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Invalid comparison between Unknown and I4
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Expected O, but got Unknown
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Expected O, but got Unknown
			//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0247: Unknown result type (might be due to invalid IL or missing references)
			//IL_024d: Unknown result type (might be due to invalid IL or missing references)
			if ((int)__instance.Mode != 2)
			{
				return;
			}
			object? obj = typeof(NCombatRoom).GetField("_visuals", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);
			ICombatRoomVisuals val = (ICombatRoomVisuals)((obj is ICombatRoomVisuals) ? obj : null);
			if (((val != null) ? val.Encounter : null) == null || !MushroomEncounters.Contains(((object)val.Encounter).GetType()))
			{
				return;
			}
			Control nodeOrNull = ((Node)__instance).GetNodeOrNull<Control>(NodePath.op_Implicit("%EnemyContainer"));
			if (nodeOrNull != null)
			{
				((CanvasItem)nodeOrNull).Visible = false;
			}
			Texture2D val2 = GD.Load<Texture2D>("res://images/event_extras/bgShrooms.png");
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
			Texture2D val4 = GD.Load<Texture2D>("res://images/event_extras/fgShrooms.png");
			if (val4 != null)
			{
				TextureRect val5 = new TextureRect();
				((Node)val5).Name = StringName.op_Implicit("EventFgOverlay");
				val5.Texture = val4;
				val5.StretchMode = (StretchModeEnum)0;
				((Control)val5).MouseFilter = (MouseFilterEnum)2;
				((Control)val5).SetAnchorsPreset((LayoutPreset)15, false);
				((Control)val5).OffsetTop = 40f;
				((CanvasItem)val5).ZIndex = -6;
				((Node)__instance).AddChild((Node)(object)val5, false, (InternalMode)0);
			}
			Control sceneContainer = ((Node)__instance).GetNodeOrNull<Control>(NodePath.op_Implicit("%CombatSceneContainer"));
			if (sceneContainer == null)
			{
				return;
			}
			Vector2 basePos = sceneContainer.Position;
			TextureRect bgNode = ((Node)__instance).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("EventBgOverlay"));
			TextureRect fgNode = ((Node)__instance).GetNodeOrNull<TextureRect>(NodePath.op_Implicit("EventFgOverlay"));
			((GodotObject)sceneContainer).Connect(StringName.op_Implicit("item_rect_changed"), Callable.From((Action)delegate
			{
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_002e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0033: Unknown result type (might be due to invalid IL or missing references)
				//IL_0038: Unknown result type (might be due to invalid IL or missing references)
				//IL_005f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0072: Unknown result type (might be due to invalid IL or missing references)
				//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
				//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
				if (GodotObject.IsInstanceValid((GodotObject)(object)sceneContainer))
				{
					Vector2 val6 = sceneContainer.Position - basePos;
					if (bgNode != null && GodotObject.IsInstanceValid((GodotObject)(object)bgNode))
					{
						((Control)bgNode).OffsetTop = 40f + val6.Y;
						((Control)bgNode).OffsetLeft = val6.X;
					}
					if (fgNode != null && GodotObject.IsInstanceValid((GodotObject)(object)fgNode))
					{
						((Control)fgNode).OffsetTop = 40f + val6.Y;
						((Control)fgNode).OffsetLeft = val6.X;
					}
				}
			}), 0u);
		}
	}

	private static readonly HashSet<Type> MushroomEncounters = new HashSet<Type> { typeof(ThreeFungiBeastsEvent) };

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
