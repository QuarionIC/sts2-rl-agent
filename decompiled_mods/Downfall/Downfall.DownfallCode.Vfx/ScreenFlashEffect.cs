using System.Collections.Generic;
using System.ComponentModel;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;

namespace Downfall.DownfallCode.Vfx;

[GlobalClass]
[ScriptPath("res://DownfallCode/Vfx/ScreenFlashEffect.cs")]
public class ScreenFlashEffect : CanvasLayer
{
	public class MethodName : MethodName
	{
		public static readonly StringName Play = StringName.op_Implicit("Play");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _elapsed = StringName.op_Implicit("_elapsed");

		public static readonly StringName _flashColor = StringName.op_Implicit("_flashColor");

		public static readonly StringName _tex = StringName.op_Implicit("_tex");
	}

	public class SignalName : SignalName
	{
	}

	private const float FadeInDuration = 0.12f;

	private const float FlashDuration = 0.7f;

	private float _elapsed;

	private Color _flashColor;

	private TextureRect _tex;

	public static void Play(Color color)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		if (!NonInteractiveMode.IsActive)
		{
			ScreenFlashEffect screenFlashEffect = new ScreenFlashEffect();
			screenFlashEffect._flashColor = color;
			((CanvasLayer)screenFlashEffect).Layer = 100;
			MainLoop mainLoop = Engine.GetMainLoop();
			MainLoop obj = ((mainLoop is SceneTree) ? mainLoop : null);
			if (obj != null)
			{
				((Node)((SceneTree)obj).Root).AddChild((Node)(object)screenFlashEffect, false, (InternalMode)0);
			}
		}
	}

	public override void _Ready()
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		_tex = new TextureRect();
		((Control)_tex).MouseFilter = (MouseFilterEnum)2;
		_tex.Texture = PreloadManager.Cache.GetAsset<Texture2D>("res://Downfall/images/vfx/screenflash.png");
		((CanvasItem)_tex).Material = (Material)new CanvasItemMaterial
		{
			BlendMode = (BlendModeEnum)1
		};
		_tex.StretchMode = (StretchModeEnum)0;
		((Control)_tex).AnchorRight = 1f;
		((Control)_tex).AnchorBottom = 1f;
		((CanvasItem)_tex).Modulate = _flashColor;
		((Node)this).AddChild((Node)(object)_tex, false, (InternalMode)0);
	}

	public override void _Process(double delta)
	{
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		_elapsed += (float)delta;
		if (_elapsed >= 0.7f)
		{
			((Node)this).QueueFree();
			return;
		}
		float num;
		if (_elapsed < 0.12f)
		{
			num = _elapsed / 0.12f;
		}
		else
		{
			float num2 = (_elapsed - 0.12f) / 0.58f;
			num = 1f - num2 * num2;
		}
		((CanvasItem)_tex).Modulate = new Color(_flashColor.R, _flashColor.G, _flashColor.B, num);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName.Play, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)20, StringName.op_Implicit("color"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Play && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Play(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((CanvasLayer)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.Play && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Play(VariantUtils.ConvertTo<Color>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.Play)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		return ((CanvasLayer)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._elapsed)
		{
			_elapsed = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._flashColor)
		{
			_flashColor = VariantUtils.ConvertTo<Color>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._tex)
		{
			_tex = VariantUtils.ConvertTo<TextureRect>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._elapsed)
		{
			value = VariantUtils.CreateFrom<float>(ref _elapsed);
			return true;
		}
		if ((ref name) == PropertyName._flashColor)
		{
			value = VariantUtils.CreateFrom<Color>(ref _flashColor);
			return true;
		}
		if ((ref name) == PropertyName._tex)
		{
			value = VariantUtils.CreateFrom<TextureRect>(ref _tex);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName._elapsed, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)20, PropertyName._flashColor, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._tex, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._elapsed, Variant.From<float>(ref _elapsed));
		info.AddProperty(PropertyName._flashColor, Variant.From<Color>(ref _flashColor));
		info.AddProperty(PropertyName._tex, Variant.From<TextureRect>(ref _tex));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._elapsed, ref val))
		{
			_elapsed = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._flashColor, ref val2))
		{
			_flashColor = ((Variant)(ref val2)).As<Color>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._tex, ref val3))
		{
			_tex = ((Variant)(ref val3)).As<TextureRect>();
		}
	}
}
