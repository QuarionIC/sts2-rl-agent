using System;
using System.Collections.Generic;
using System.Reflection;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Godot;
using Godot.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Vfx.Utilities;
using MegaCrit.Sts2.addons.mega_text;

namespace BaseLib.Utils.NodeFactories;

internal class NEnergyCounterFactory : NodeFactory<NEnergyCounter>
{
	private const string DefaultLabelFontPath = "res://themes/kreon_bold_shared.tres";

	private static readonly FieldInfo? ParticlesField = AccessTools.Field(typeof(NParticlesContainer), "_particles");

	private static readonly StringName ShadowOffsetX = StringName.op_Implicit("shadow_offset_x");

	private static readonly StringName ShadowOffsetY = StringName.op_Implicit("shadow_offset_y");

	private static readonly StringName ShadowOutlineSize = StringName.op_Implicit("shadow_outline_size");

	public NEnergyCounterFactory()
		: base((IEnumerable<INodeInfo>)new _003C_003Ez__ReadOnlyArray<INodeInfo>(new INodeInfo[6]
		{
			new NodeInfo<NParticlesContainer>("%EnergyVfxBack"),
			new NodeInfo<Control>("%Layers"),
			new NodeInfo<Control>("%RotationLayers"),
			new NodeInfo<NParticlesContainer>("%EnergyVfxFront"),
			new NodeInfo<MegaLabel>("Label"),
			new NodeInfo<NParticlesContainer>("%StarAnchor")
		}))
	{
	}

	protected override NEnergyCounter CreateBareFromResource(object resource)
	{
		if (resource is CustomEnergyCounter)
		{
			return FromLegacy((CustomEnergyCounter)resource);
		}
		return base.CreateBareFromResource(resource);
	}

	protected override void ConvertScene(NEnergyCounter target, Node? source)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (source != null)
		{
			((Node)target).Name = source.Name;
			if (target != null)
			{
				Control val = (Control)(object)((source is Control) ? source : null);
				if (val == null)
				{
					CanvasItem target2 = (CanvasItem)(object)target;
					CanvasItem val2 = (CanvasItem)(object)((source is CanvasItem) ? source : null);
					if (val2 != null)
					{
						NodeFactory.CopyCanvasItemProperties(target2, val2);
					}
					else
					{
						((Control)target).Size = new Vector2(128f, 128f);
						((Control)target).PivotOffset = ((Control)target).Size * 0.5f;
					}
				}
				else
				{
					NodeFactory.CopyControlProperties((Control)(object)target, val);
				}
			}
		}
		TransferAndCreateNodes(target, source);
	}

	protected override void GenerateNode(Node target, INodeInfo required)
	{
		switch (required.Path)
		{
		case "Label":
		{
			MegaLabel val = CreateDefaultLabel();
			target.AddChild((Node)(object)val, false, (InternalMode)0);
			break;
		}
		case "%RotationLayers":
		{
			Control child3 = CreateFullRectControl(null);
			target.AddUnique((Node)(object)child3, "RotationLayers");
			break;
		}
		case "%EnergyVfxBack":
		{
			NParticlesContainer child2 = CreateParticlesContainer(null, StringName.op_Implicit("EnergyVfxBack"));
			target.AddUnique((Node)(object)child2);
			break;
		}
		case "%EnergyVfxFront":
		{
			NParticlesContainer child = CreateParticlesContainer(null, StringName.op_Implicit("EnergyVfxFront"));
			target.AddUnique((Node)(object)child);
			break;
		}
		}
	}

	protected override Node ConvertNodeType(Node node, Type targetType)
	{
		if (targetType == typeof(NParticlesContainer))
		{
			return (Node)(object)CreateParticlesContainer(node, node.Name);
		}
		if (targetType == typeof(Control))
		{
			return (Node)(object)CreateFullRectControl(node);
		}
		if (targetType == typeof(MegaLabel))
		{
			return (Node)(((object)CreateLabel(node)) ?? ((object)base.ConvertNodeType(node, targetType)));
		}
		return base.ConvertNodeType(node, targetType);
	}

	private static Control CreateFullRectControl(Node? n)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		Control val = new Control
		{
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2,
			MouseFilter = (MouseFilterEnum)2
		};
		if (n == null)
		{
			return val;
		}
		((Node)val).Name = n.Name;
		n.ReplaceBy((Node)(object)val, false);
		n.Name = StringName.op_Implicit("_" + StringName.op_Implicit(n.Name));
		((Node)val).AddChild(n, false, (InternalMode)0);
		return val;
	}

	private static NParticlesContainer CreateParticlesContainer(Node? source, StringName name)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected O, but got Unknown
		NParticlesContainer val = new NParticlesContainer
		{
			Name = name,
			UniqueNameInOwner = true
		};
		if (source != null)
		{
			source.Name = StringName.op_Implicit("_" + StringName.op_Implicit(source.Name));
		}
		CanvasItem val2 = (CanvasItem)(object)((source is CanvasItem) ? source : null);
		if (val2 != null)
		{
			NodeFactory.CopyCanvasItemProperties((CanvasItem)(object)val, val2);
		}
		GpuParticles2D val3 = (GpuParticles2D)(object)((source is GpuParticles2D) ? source : null);
		if (val3 != null)
		{
			source.ReplaceBy((Node)(object)val, false);
			((Node)val).AddChild((Node)(object)val3, false, (InternalMode)0);
			((Node)val3).Owner = (Node)(object)val;
			SetParticles(val);
			return val;
		}
		if (source != null)
		{
			source.ReplaceBy((Node)(object)val, false);
			((Node)val).AddChild(source, false, (InternalMode)0);
		}
		SetParticles(val);
		return val;
	}

	private static void SetParticles(NParticlesContainer container)
	{
		Array<GpuParticles2D> val = new Array<GpuParticles2D>();
		CollectParticles((Node)(object)container, val);
		ParticlesField?.SetValue(container, val);
	}

	private static void CollectParticles(Node node, Array<GpuParticles2D> particles)
	{
		foreach (Node child in node.GetChildren(false))
		{
			GpuParticles2D val = (GpuParticles2D)(object)((child is GpuParticles2D) ? child : null);
			if (val != null)
			{
				particles.Add(val);
			}
			CollectParticles(child, particles);
		}
	}

	public static NEnergyCounter FromLegacy(CustomEnergyCounter counter)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected O, but got Unknown
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Unknown result type (might be due to invalid IL or missing references)
		//IL_0154: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Expected O, but got Unknown
		NEnergyCounter val = new NEnergyCounter
		{
			Name = StringName.op_Implicit("LegacyEnergyCounter"),
			Size = new Vector2(128f, 128f),
			PivotOffset = new Vector2(64f, 64f)
		};
		NParticlesContainer val2 = new NParticlesContainer
		{
			Name = StringName.op_Implicit("EnergyVfxBack"),
			Position = new Vector2(64f, 64f),
			Modulate = counter.BurstColor
		};
		((Node)(object)val).AddUnique((Node)(object)val2, "EnergyVfxBack");
		SetParticles(val2);
		Control val3 = CreateFullRectControl(null);
		Control val4 = CreateFullRectControl(null);
		val4.PivotOffset = new Vector2(64f, 64f);
		((Node)(object)val3).AddUnique((Node)(object)val4, "RotationLayers");
		AddLayer(val3, "Layer1", counter.LayerImagePath(1));
		AddLayer(val4, "Layer2", counter.LayerImagePath(2), rotates: true);
		AddLayer(val4, "Layer3", counter.LayerImagePath(3), rotates: true);
		AddLayer(val3, "Layer4", counter.LayerImagePath(4));
		AddLayer(val3, "Layer5", counter.LayerImagePath(5));
		((Node)(object)val).AddUnique((Node)(object)val3, "Layers");
		NParticlesContainer val5 = new NParticlesContainer
		{
			Name = StringName.op_Implicit("EnergyVfxFront"),
			Position = new Vector2(64f, 64f),
			Modulate = counter.BurstColor
		};
		((Node)(object)val).AddUnique((Node)(object)val5, "EnergyVfxFront");
		SetParticles(val5);
		MegaLabel val6 = CreateDefaultLabel();
		((Node)val).AddChild((Node)(object)val6, false, (InternalMode)0);
		((Node)val6).Owner = (Node)(object)val;
		return val;
	}

	private static void AddLayer(Control parent, string name, string texturePath, bool rotates = false)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Expected O, but got Unknown
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		TextureRect val = new TextureRect
		{
			Name = StringName.op_Implicit(name),
			AnchorRight = 1f,
			AnchorBottom = 1f,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2,
			MouseFilter = (MouseFilterEnum)2,
			Texture = ResourceLoader.Load<Texture2D>(texturePath, (string)null, (CacheMode)1),
			ExpandMode = (ExpandModeEnum)1,
			StretchMode = (StretchModeEnum)5
		};
		if (rotates)
		{
			((Control)val).PivotOffset = new Vector2(64f, 64f);
		}
		((Node)parent).AddChild((Node)(object)val, false, (InternalMode)0);
		((Node)val).Owner = (Node)(object)parent;
	}

	private static MegaLabel? CreateLabel(Node? source)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		Label val = (Label)(object)((source is Label) ? source : null);
		if (val != null)
		{
			MegaLabel val2 = new MegaLabel
			{
				Name = source.Name
			};
			NodeFactory.CopyControlProperties((Control)(object)val2, (Control)(object)val);
			((Label)val2).Text = val.Text;
			((Label)val2).HorizontalAlignment = val.HorizontalAlignment;
			((Label)val2).VerticalAlignment = val.VerticalAlignment;
			((Label)val2).AutowrapMode = val.AutowrapMode;
			((Label)val2).ClipText = val.ClipText;
			((Label)val2).Uppercase = val.Uppercase;
			((Label)val2).VisibleCharactersBehavior = val.VisibleCharactersBehavior;
			EnsureLabelFont(val2, val);
			CopyLabelThemeOverrides(val2, val);
			MegaLabel val3 = (MegaLabel)(object)((val is MegaLabel) ? val : null);
			if (val3 != null)
			{
				val2.AutoSizeEnabled = val3.AutoSizeEnabled;
				val2.MinFontSize = val3.MinFontSize;
				val2.MaxFontSize = val3.MaxFontSize;
			}
			else
			{
				val2.AutoSizeEnabled = true;
				val2.MinFontSize = 32;
				val2.MaxFontSize = Math.Max(36, ((Control)val).GetThemeFontSize(Label.FontSize, StringName.op_Implicit("Label")));
			}
			source.ReplaceBy((Node)(object)val2, false);
			source.QueueFree();
			return val2;
		}
		return null;
	}

	private static MegaLabel CreateDefaultLabel()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_010c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_013e: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Expected O, but got Unknown
		MegaLabel val = new MegaLabel
		{
			Name = StringName.op_Implicit("Label"),
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 16f,
			OffsetTop = -29f,
			OffsetRight = -16f,
			OffsetBottom = 29f,
			GrowHorizontal = (GrowDirection)2,
			GrowVertical = (GrowDirection)2,
			HorizontalAlignment = (HorizontalAlignment)1,
			VerticalAlignment = (VerticalAlignment)1,
			Text = "3/3",
			AutoSizeEnabled = true,
			MinFontSize = 32,
			MaxFontSize = 36
		};
		EnsureLabelFont(val, null);
		((Control)val).AddThemeColorOverride(Label.FontColor, new Color(1f, 0.964706f, 0.886275f, 1f));
		((Control)val).AddThemeColorOverride(Label.FontShadowColor, new Color(0f, 0f, 0f, 0.188235f));
		((Control)val).AddThemeColorOverride(Label.FontOutlineColor, new Color(0.3f, 0.0759f, 0.051f, 1f));
		((Control)val).AddThemeConstantOverride(ShadowOffsetX, 3);
		((Control)val).AddThemeConstantOverride(ShadowOffsetY, 2);
		((Control)val).AddThemeConstantOverride(Label.OutlineSize, 16);
		((Control)val).AddThemeConstantOverride(ShadowOutlineSize, 16);
		((Control)val).AddThemeFontSizeOverride(Label.FontSize, 36);
		return val;
	}

	private static void EnsureLabelFont(MegaLabel target, Label? source)
	{
		Font val = ((source != null) ? ((Control)source).GetThemeFont(Label.Font, (StringName)null) : null);
		if (val == ((source != null) ? ((Control)source).GetThemeDefaultFont() : null))
		{
			val = PreloadManager.Cache.GetAsset<Font>("res://themes/kreon_bold_shared.tres");
		}
		((Control)target).AddThemeFontOverride(Label.Font, val);
	}

	private static void CopyLabelThemeOverrides(MegaLabel target, Label source)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		((Control)target).AddThemeColorOverride(Label.FontColor, ((Control)source).GetThemeColor(Label.FontColor, (StringName)null));
		((Control)target).AddThemeColorOverride(Label.FontShadowColor, ((Control)source).GetThemeColor(Label.FontShadowColor, (StringName)null));
		((Control)target).AddThemeColorOverride(Label.FontOutlineColor, ((Control)source).GetThemeColor(Label.FontOutlineColor, (StringName)null));
		((Control)target).AddThemeConstantOverride(ShadowOffsetX, ((Control)source).GetThemeConstant(ShadowOffsetX, (StringName)null));
		((Control)target).AddThemeConstantOverride(ShadowOffsetY, ((Control)source).GetThemeConstant(ShadowOffsetY, (StringName)null));
		((Control)target).AddThemeConstantOverride(Label.OutlineSize, ((Control)source).GetThemeConstant(Label.OutlineSize, (StringName)null));
		((Control)target).AddThemeConstantOverride(ShadowOutlineSize, ((Control)source).GetThemeConstant(ShadowOutlineSize, (StringName)null));
		((Control)target).AddThemeFontSizeOverride(Label.FontSize, ((Control)source).GetThemeFontSize(Label.FontSize, (StringName)null));
	}
}
