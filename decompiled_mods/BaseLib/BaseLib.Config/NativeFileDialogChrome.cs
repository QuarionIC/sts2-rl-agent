using System;
using Godot;

namespace BaseLib.Config;

internal static class NativeFileDialogChrome
{
	private const int FileDialogLayer = 132;

	public static void Popup(FileDialog dialog, float centeredRatio = 0.55f)
	{
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Expected O, but got Unknown
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Expected O, but got Unknown
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		MainLoop mainLoop = Engine.GetMainLoop();
		SceneTree val = (SceneTree)(object)((mainLoop is SceneTree) ? mainLoop : null);
		if (((val != null) ? val.Root : null) == null)
		{
			((Node)dialog).QueueFree();
			return;
		}
		Viewport viewport = ((Node)val.Root).GetViewport();
		Viewport obj = viewport;
		Control previousFocus = ((obj != null) ? obj.GuiGetFocusOwner() : null);
		MouseModeEnum previousMouseMode = Input.MouseMode;
		CanvasLayer layer = new CanvasLayer
		{
			Name = StringName.op_Implicit("BaseLibNativeFileDialogModal"),
			Layer = 132
		};
		((Node)val.Root).AddChild((Node)(object)layer, false, (InternalMode)0);
		Control shield = new Control
		{
			Name = StringName.op_Implicit("FileDialogShieldRoot"),
			MouseFilter = (MouseFilterEnum)0
		};
		((Node)layer).AddChild((Node)(object)shield, false, (InternalMode)0);
		ColorRect val2 = new ColorRect
		{
			Name = StringName.op_Implicit("FileDialogDim"),
			Color = new Color(0f, 0f, 0f, 0.55f),
			MouseFilter = (MouseFilterEnum)0
		};
		((Control)val2).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
		((Node)shield).AddChild((Node)(object)val2, false, (InternalMode)0);
		((Node)layer).AddChild((Node)(object)dialog, false, (InternalMode)0);
		ConfigureDialog(dialog);
		Callable val3 = Callable.From((Action)FitShieldToViewport);
		((Callable)(ref val3)).CallDeferred(Array.Empty<Variant>());
		if (viewport != null)
		{
			viewport.SizeChanged += FitShieldToViewport;
		}
		((AcceptDialog)dialog).Canceled += CloseDialog;
		((Window)dialog).CloseRequested += CloseDialog;
		((Node)dialog).TreeExiting += RestoreMouseAndFocus;
		Input.MouseMode = (MouseModeEnum)0;
		((Window)dialog).PopupCenteredRatio(centeredRatio);
		void CloseDialog()
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)dialog))
			{
				((Node)dialog).QueueFree();
			}
		}
		void FitShieldToViewport()
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			if (GodotObject.IsInstanceValid((GodotObject)(object)shield) && viewport != null)
			{
				Rect2 visibleRect = viewport.GetVisibleRect();
				shield.Position = ((Rect2)(ref visibleRect)).Position;
				shield.Size = ((Rect2)(ref visibleRect)).Size;
			}
		}
		void RestoreMouseAndFocus()
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			if (GodotObject.IsInstanceValid((GodotObject)(object)viewport))
			{
				viewport.SizeChanged -= FitShieldToViewport;
			}
			Input.MouseMode = previousMouseMode;
			if (GodotObject.IsInstanceValid((GodotObject)(object)layer))
			{
				((Node)layer).QueueFree();
			}
			Control target = previousFocus;
			if (target != null && GodotObject.IsInstanceValid((GodotObject)(object)target) && ((CanvasItem)target).IsVisibleInTree())
			{
				Callable val4 = Callable.From((Action)delegate
				{
					if (GodotObject.IsInstanceValid((GodotObject)(object)target) && ((CanvasItem)target).IsVisibleInTree())
					{
						target.GrabFocus();
					}
				});
				((Callable)(ref val4)).CallDeferred(Array.Empty<Variant>());
			}
		}
	}

	private static void ConfigureDialog(FileDialog dialog)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		((Node)dialog).Name = StringName.op_Implicit("BaseLibNativeFileDialog");
		((Window)dialog).Exclusive = true;
		((Window)dialog).Unresizable = false;
		((Window)dialog).Transparent = false;
		((Window)dialog).MinSize = new Vector2I(760, 520);
		((Window)dialog).Size = ((Window)dialog).MinSize;
	}
}
