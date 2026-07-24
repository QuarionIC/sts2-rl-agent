using System;
using System.Collections.Generic;
using System.Linq;
using Downfall.DownfallCode.Voting;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;
using MegaCrit.Sts2.addons.mega_text;

namespace Downfall.DownfallCode.Patches;

[HarmonyPatch(typeof(NMainMenu), "_Ready")]
internal static class MainMenuButtonsPatch
{
	private static NMainMenuSubmenuStack? _stack;

	[HarmonyPostfix]
	private static void Postfix(NMainMenu __instance)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		NMainMenuTextButton node = ((Node)__instance).GetNode<NMainMenuTextButton>(NodePath.op_Implicit("MainMenuTextButtons/SettingsButton"));
		_stack = FindStack((Node)(object)__instance) ?? FindStack((Node)(object)((Node)__instance).GetTree().Root);
		foreach (MainMenuButtonRegistry.Entry entry in MainMenuButtonRegistry.Entries)
		{
			if (!entry.IsVisible())
			{
				continue;
			}
			NMainMenuTextButton val = (NMainMenuTextButton)((Node)node).Duplicate(15);
			((Node)node).AddSibling((Node)(object)val, false);
			((Label)((Node)val).GetChild<MegaLabel>(0, false)).Text = entry.Label;
			MainMenuButtonRegistry.Entry captured = entry;
			((GodotObject)val).Connect(SignalName.Released, Callable.From<NButton>((Action<NButton>)delegate
			{
				if (captured.OnPress != null)
				{
					captured.OnPress(_stack);
				}
				else if (captured.SubmenuType != null)
				{
					NMainMenuSubmenuStack? stack = _stack;
					if (stack != null)
					{
						((NSubmenuStack)stack).PushSubmenuType(captured.SubmenuType);
					}
				}
			}), 0u);
		}
	}

	private static NMainMenuSubmenuStack? FindStack(Node root)
	{
		NMainMenuSubmenuStack val = (NMainMenuSubmenuStack)(object)((root is NMainMenuSubmenuStack) ? root : null);
		if (val != null)
		{
			return val;
		}
		return ((IEnumerable<Node>)root.GetChildren(false)).Select(FindStack).OfType<NMainMenuSubmenuStack>().FirstOrDefault();
	}
}
