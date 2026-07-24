using System;
using System.Linq.Expressions;
using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace BaseLib.Patches.Fixes;

[HarmonyPatch(typeof(NCardPlay), "TryPlayCard")]
internal static class NCardPlayTryPlayCardAnyPlayerPatch
{
	private static readonly Action<NCardPlay, bool>? CleanupBool = CreateCleanupBool();

	private static readonly Action<NCardPlay>? CleanupVoid = CreateCleanupVoid();

	private static readonly Action? FocusDefaultControl = CreateFocusDefaultControl();

	[HarmonyPrefix]
	private static bool TryPlayAnyPlayer(NCardPlay __instance, Creature? target)
	{
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		CardModel card = __instance.Card;
		if (!AnyPlayerCardTargetingHelper.IsAnyPlayerMultiplayer(card))
		{
			return true;
		}
		if (target == null || ((NCardHolder)__instance.Holder).CardModel == null)
		{
			__instance.CancelPlayCard();
			return false;
		}
		if (!((NCardHolder)__instance.Holder).CardModel.CanPlayTargeting(target))
		{
			__instance.CannotPlayThisCardFtueCheck(((NCardHolder)__instance.Holder).CardModel);
			__instance.CancelPlayCard();
			return false;
		}
		__instance._isTryingToPlayCard = true;
		bool num = card.TryManualPlay(target);
		__instance._isTryingToPlayCard = false;
		if (num)
		{
			__instance.AutoDisableCannotPlayCardFtueCheck();
			if (((Node)__instance.Holder).IsInsideTree())
			{
				Rect2 visibleRect = ((Node)__instance).GetViewport().GetVisibleRect();
				Vector2 size = ((Rect2)(ref visibleRect)).Size;
				__instance.Holder.SetTargetPosition(new Vector2(size.X / 2f, size.Y - ((Control)__instance.Holder).Size.Y));
			}
			InvokeCleanupFinished(__instance, success: true);
			FocusAfterPlayed();
		}
		else
		{
			__instance.CancelPlayCard();
		}
		return false;
	}

	private static Action<NCardPlay, bool>? CreateCleanupBool()
	{
		MethodInfo methodInfo = AccessTools.DeclaredMethod(typeof(NCardPlay), "Cleanup", new Type[1] { typeof(bool) }, (Type[])null);
		if (!(methodInfo != null))
		{
			return null;
		}
		return AccessTools.MethodDelegate<Action<NCardPlay, bool>>(methodInfo, (object)null, true, (Type[])null);
	}

	private static Action<NCardPlay>? CreateCleanupVoid()
	{
		if (CleanupBool != null)
		{
			return null;
		}
		MethodInfo methodInfo = AccessTools.DeclaredMethod(typeof(NCardPlay), "Cleanup", Type.EmptyTypes, (Type[])null);
		if (!(methodInfo != null))
		{
			return null;
		}
		return AccessTools.MethodDelegate<Action<NCardPlay>>(methodInfo, (object)null, true, (Type[])null);
	}

	private static void InvokeCleanupFinished(NCardPlay instance, bool success)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		if (CleanupBool != null)
		{
			CleanupBool(instance, success);
			return;
		}
		CleanupVoid?.Invoke(instance);
		((GodotObject)instance).EmitSignal(SignalName.Finished, (Variant[])(object)new Variant[1] { Variant.op_Implicit(success) });
	}

	private static Action? CreateFocusDefaultControl()
	{
		Type type = AccessTools.TypeByName("MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext.ActiveScreenContext");
		if (type == null)
		{
			return null;
		}
		MethodInfo methodInfo = AccessTools.PropertyGetter(type, "Instance");
		MethodInfo methodInfo2 = AccessTools.Method(type, "FocusOnDefaultControl", (Type[])null, (Type[])null);
		if (methodInfo == null || methodInfo2 == null)
		{
			return null;
		}
		Func<object?> getInst = CompileStaticGetterAsObject(methodInfo);
		Action<object> focus = CompileInstanceVoidMethodAsObjectAction(methodInfo2);
		if (getInst == null || focus == null)
		{
			return null;
		}
		return delegate
		{
			object obj = getInst();
			if (obj != null)
			{
				focus(obj);
			}
		};
	}

	private static Func<object?>? CompileStaticGetterAsObject(MethodInfo getter)
	{
		try
		{
			return Expression.Lambda<Func<object>>(Expression.Convert(Expression.Call(getter), typeof(object)), Array.Empty<ParameterExpression>()).Compile();
		}
		catch
		{
			return null;
		}
	}

	private static Action<object>? CompileInstanceVoidMethodAsObjectAction(MethodInfo mi)
	{
		try
		{
			ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "inst");
			return Expression.Lambda<Action<object>>(Expression.Call(Expression.Convert(parameterExpression, mi.DeclaringType), mi), new ParameterExpression[1] { parameterExpression }).Compile();
		}
		catch
		{
			return null;
		}
	}

	private static void FocusAfterPlayed()
	{
		if (FocusDefaultControl != null)
		{
			FocusDefaultControl();
			return;
		}
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			NodeUtil.TryGrabFocus((Control)(object)instance.Ui.Hand);
		}
	}
}
