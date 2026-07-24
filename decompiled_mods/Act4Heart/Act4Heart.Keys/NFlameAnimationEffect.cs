using System;
using System.Collections.Generic;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;

namespace Act4Heart.Keys;

public class NFlameAnimationEffect : TextureRect
{
	private static bool alternator;

	private readonly Texture2D[] flames;

	private float duration;

	private Color color;

	private byte current_img;

	public NFlameAnimationEffect(Texture2D[] flames, Control parent)
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		this.flames = flames;
		duration = 0.5f;
		bool flag = (alternator = !alternator);
		((TextureRect)this).ExpandMode = (ExpandModeEnum)1;
		Vector2 val = parent.Size / 128f;
		((Control)this).Size = parent.Size;
		((Control)this).PivotOffset = ((Control)this).Size / 2f;
		((Control)this).Scale = random_single(0.9f, 1.3f) * new Vector2(1.33f / val.X, 1.33f / val.Y);
		((Control)this).RotationDegrees = random_single(-30f, 30f);
		((Control)this).Position = new Vector2((flag ? 1f : (-1f)) * random_single(0f, 8f), random_single(-3f, 12f) - ((Control)this).Size.Y / 6f);
		color = new Color(0.34f, 0.34f, 0.34f, duration);
		((CanvasItem)this).SelfModulate = color;
		current_img = 0;
		((TextureRect)this).Texture = flames[0];
		((TextureRect)this).FlipH = flag;
		((Control)this).MouseBehaviorRecursive = (MouseBehaviorRecursiveEnum)1;
		static float random_single(float min, float max)
		{
			return Random.Shared.NextSingle() * (max - min) + min;
		}
	}

	public override void _Process(double delta)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		int num = current_img;
		color.A = duration;
		((CanvasItem)this).SelfModulate = color;
		if (duration < 0f)
		{
			current_img = 5;
		}
		else if (duration < 0.1f)
		{
			current_img = 4;
		}
		else if (duration < 0.2f)
		{
			current_img = 3;
		}
		else if (duration < 0.3f)
		{
			current_img = 2;
		}
		else if (duration < 0.4f)
		{
			current_img = 1;
		}
		else
		{
			current_img = 0;
		}
		if (current_img != num)
		{
			((TextureRect)this).Texture = ((current_img >= 0) ? flames[current_img] : null);
		}
		duration -= (float)delta;
		if (duration < 0f)
		{
			((Node)this).QueueFree();
		}
	}

	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(1)
		{
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((TextureRect)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		return ((TextureRect)this).HasGodotClassMethod(ref method);
	}
}
