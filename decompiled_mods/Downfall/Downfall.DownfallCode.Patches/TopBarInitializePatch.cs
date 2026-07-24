using System;
using Downfall.DownfallCode.Utils.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NTopBar), "Initialize")]
internal class TopBarInitializePatch
{
	[HarmonyPostfix]
	private static void AddRegisteredElements(NTopBar __instance, IRunState runState)
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		Player me = LocalContext.GetMe((IPlayerCollection)(object)runState);
		if (me == null)
		{
			return;
		}
		HBoxContainer nodeOrNull = ((Node)__instance).GetNodeOrNull<HBoxContainer>(NodePath.op_Implicit("RightAlignedStuff"));
		if (nodeOrNull == null)
		{
			return;
		}
		foreach (Type type in TopBarElementRegistry.Types)
		{
			ITopBarElementDescriptor topBarElementDescriptor = (ITopBarElementDescriptor)Activator.CreateInstance(type);
			if (!topBarElementDescriptor.CanUse(me))
			{
				continue;
			}
			PackedScene val = ResourceLoader.Load<PackedScene>(topBarElementDescriptor.ScenePath, (string)null, (CacheMode)1);
			if (val != null)
			{
				Control val2 = val.Instantiate<Control>((GenEditState)0);
				val2.CustomMinimumSize = new Vector2(topBarElementDescriptor.Width, 0f);
				val2.SizeFlagsHorizontal = (SizeFlags)0;
				((Node)nodeOrNull).AddChild((Node)(object)val2, false, (InternalMode)0);
				((Node)nodeOrNull).MoveChild((Node)(object)val2, 3);
				if (val2 is ITopBarElement topBarElement)
				{
					topBarElement.Initialize(me);
				}
			}
		}
	}
}
