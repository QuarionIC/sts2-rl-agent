using System;
using System.Collections.Generic;
using System.ComponentModel;
using Downfall.DownfallCode.Interfaces;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace SlimeBoss.SlimeBossCode.Vfx;

[GlobalClass]
[ScriptPath("res://SlimeBossCode/Vfx/NSlimeCreatureVisuals.cs")]
public class NSlimeCreatureVisuals : NCreatureVisuals, IAnimatedVisuals
{
	public class MethodName : MethodName
	{
		public static readonly StringName OnAnimationTrigger = StringName.op_Implicit("OnAnimationTrigger");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName SetupBones = StringName.op_Implicit("SetupBones");

		public static readonly StringName OnWorldTransformsChanged = StringName.op_Implicit("OnWorldTransformsChanged");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _playback = StringName.op_Implicit("_playback");
	}

	public class SignalName : SignalName
	{
	}

	private readonly List<(MegaBone Bone, Node2D Node)> _attachments = new List<(MegaBone, Node2D)>();

	private AnimationNodeStateMachinePlayback? _playback;

	public void OnAnimationTrigger(string trigger)
	{
		if (_playback != null)
		{
			string text = ((trigger == "Idle") ? "idle" : ((!(trigger == "Attack")) ? "idle" : "attack"));
			string text2 = text;
			_playback.Travel(StringName.op_Implicit(text2), true);
		}
	}

	public override void _Ready()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		((NCreatureVisuals)this)._Ready();
		CanvasItemMaterial val = new CanvasItemMaterial
		{
			BlendMode = (BlendModeEnum)4
		};
		if (((NCreatureVisuals)this).SpineBody != null)
		{
			((NCreatureVisuals)this).SpineBody.SetNormalMaterial((Material)(object)val);
		}
		else
		{
			((CanvasItem)((NCreatureVisuals)this).GetCurrentBody()).Material = (Material)(object)val;
		}
		AnimationTree node = ((Node)this).GetNode<AnimationTree>(NodePath.op_Implicit("%AnimationTree"));
		((AnimationMixer)node).Active = true;
		_playback = (AnimationNodeStateMachinePlayback)(GodotObject)((GodotObject)node).Get(StringName.op_Implicit("parameters/playback"));
		((Node)this).GetTree().ProcessFrame += SetupBones;
	}

	private void SetupBones()
	{
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		((Node)this).GetTree().ProcessFrame -= SetupBones;
		MegaSprite spineBody = ((NCreatureVisuals)this).SpineBody;
		MegaSkeleton skeleton = ((spineBody != null) ? spineBody.GetSkeleton() : null);
		TryAttach(skeleton, "eyeback1", "%LeftStick");
		TryAttach(skeleton, "eyeback4", "%RightStick");
		TryAttach(skeleton, "eyeshadow", "%ScrapVfx");
		TryAttach(skeleton, "bone7", "%Antennae");
		TryAttach(skeleton, "bone4", "%Stopwatch");
		TryAttach(skeleton, "bone8", "%Crown");
		TryAttach(skeleton, "eye", "%Eye");
		MegaSprite spineBody2 = ((NCreatureVisuals)this).SpineBody;
		if (spineBody2 != null)
		{
			spineBody2.ConnectWorldTransformsChanged(Callable.From<Variant>((Action<Variant>)OnWorldTransformsChanged));
		}
	}

	private void TryAttach(MegaSkeleton? skeleton, string boneName, string nodePath)
	{
		Node2D nodeOrNull = ((Node)this).GetNodeOrNull<Node2D>(NodePath.op_Implicit(nodePath));
		if (nodeOrNull != null)
		{
			MegaBone val = ((skeleton != null) ? skeleton.FindBone(boneName) : null);
			if (val != null)
			{
				_attachments.Add((val, nodeOrNull));
			}
		}
	}

	private void OnWorldTransformsChanged(Variant _)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		foreach (var attachment in _attachments)
		{
			MegaBone item = attachment.Bone;
			Node2D item2 = attachment.Node;
			Variant val = ((MegaSpineBinding)item).BoundObject.Call(StringName.op_Implicit("get_world_x"), Array.Empty<Variant>());
			float num = ((Variant)(ref val)).As<float>();
			val = ((MegaSpineBinding)item).BoundObject.Call(StringName.op_Implicit("get_world_y"), Array.Empty<Variant>());
			float num2 = ((Variant)(ref val)).As<float>();
			item2.Position = new Vector2(num, num2);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(4)
		{
			new MethodInfo(MethodName.OnAnimationTrigger, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)4, StringName.op_Implicit("trigger"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetupBones, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnWorldTransformsChanged, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)0, StringName.op_Implicit("_"), (PropertyHint)0, "", (PropertyUsageFlags)131078, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.OnAnimationTrigger && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnAnimationTrigger(VariantUtils.ConvertTo<string>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetupBones && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			SetupBones();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnWorldTransformsChanged && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			OnWorldTransformsChanged(VariantUtils.ConvertTo<Variant>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((NCreatureVisuals)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.OnAnimationTrigger)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.SetupBones)
		{
			return true;
		}
		if ((ref method) == MethodName.OnWorldTransformsChanged)
		{
			return true;
		}
		return ((NCreatureVisuals)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._playback)
		{
			_playback = VariantUtils.ConvertTo<AnimationNodeStateMachinePlayback>(ref value);
			return true;
		}
		return ((NCreatureVisuals)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._playback)
		{
			value = VariantUtils.CreateFrom<AnimationNodeStateMachinePlayback>(ref _playback);
			return true;
		}
		return ((NCreatureVisuals)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._playback, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		((NCreatureVisuals)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._playback, Variant.From<AnimationNodeStateMachinePlayback>(ref _playback));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NCreatureVisuals)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._playback, ref val))
		{
			_playback = ((Variant)(ref val)).As<AnimationNodeStateMachinePlayback>();
		}
	}
}
