using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace BaseLib.Config.UI;

public interface ISelectionReticle
{
	NSelectionReticle? Reticle { get; set; }

	void SetupSelectionReticle(Control targetControl, int margin = -12)
	{
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Expected O, but got Unknown
		if (Reticle == null)
		{
			PackedScene scene = PreloadManager.Cache.GetScene(SceneHelper.GetScenePath("ui/selection_reticle"));
			Reticle = scene.Instantiate<NSelectionReticle>((GenEditState)0);
			((Node)Reticle).Name = StringName.op_Implicit("SelectionReticle");
			Control val = targetControl;
			if (targetControl is Container)
			{
				Control val2 = new Control
				{
					Name = StringName.op_Implicit("ReticleWrapper"),
					MouseFilter = (MouseFilterEnum)2,
					SizeFlagsHorizontal = (SizeFlags)3,
					SizeFlagsVertical = (SizeFlags)3
				};
				((Node)targetControl).AddChild((Node)(object)val2, false, (InternalMode)0);
				val2.SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, 0);
				val = val2;
			}
			((Node)val).AddChild((Node)(object)Reticle, false, (InternalMode)0);
			((Control)Reticle).SetAnchorsAndOffsetsPreset((LayoutPreset)15, (LayoutPresetMode)0, margin);
		}
		targetControl.FocusEntered += delegate
		{
			NControllerManager instance = NControllerManager.Instance;
			if (instance != null && instance.IsUsingController)
			{
				Reticle.OnSelect();
			}
		};
		targetControl.FocusExited += delegate
		{
			Reticle.OnDeselect();
		};
	}
}
