using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using MegaCrit.Sts2.Core.TestSupport;

namespace Downfall.DownfallCode.Nodes;

[GlobalClass]
[ScriptPath("res://DownfallCode/Nodes/NCustomCardHolder.cs")]
public class NCustomCardHolder : NCardHolder, IPoolable
{
	public class MethodName : MethodName
	{
		public static readonly StringName OnInstantiated = StringName.op_Implicit("OnInstantiated");

		public static readonly StringName OnReturnedFromPool = StringName.op_Implicit("OnReturnedFromPool");

		public static readonly StringName OnFreedToPool = StringName.op_Implicit("OnFreedToPool");

		public static readonly StringName InitPool = StringName.op_Implicit("InitPool");

		public static readonly StringName Create = StringName.op_Implicit("Create");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

		public static readonly StringName UpdateName = StringName.op_Implicit("UpdateName");

		public static readonly StringName UpdateCardModel = StringName.op_Implicit("UpdateCardModel");

		public static readonly StringName SetIsPreviewingUpgrade = StringName.op_Implicit("SetIsPreviewingUpgrade");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName SmallScale = StringName.op_Implicit("SmallScale");

		public static readonly StringName HoverScale = StringName.op_Implicit("HoverScale");

		public static readonly StringName _hoverScale = StringName.op_Implicit("_hoverScale");

		public static readonly StringName _isPreviewingUpgrade = StringName.op_Implicit("_isPreviewingUpgrade");

		public static readonly StringName _smallScale = StringName.op_Implicit("_smallScale");
	}

	public class SignalName : SignalName
	{
	}

	private CardModel? _baseCard;

	private float _hoverScale;

	private bool _isPreviewingUpgrade;

	private float _smallScale;

	private CardModel? _upgradedCard;

	public override Vector2 SmallScale => Vector2.One * _smallScale;

	protected override Vector2 HoverScale => Vector2.One * _hoverScale;

	public override CardModel? CardModel => _baseCard;

	private static string ScenePath => "res://Downfall/scenes/screens/custom_card_holder.tscn";

	public void OnInstantiated()
	{
	}

	public void OnReturnedFromPool()
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (((Node)this).IsNodeReady())
		{
			((Control)this).Position = Vector2.Zero;
			((Control)this).Rotation = 0f;
			((Control)this).Scale = Vector2.One;
			((CanvasItem)this).Modulate = Colors.White;
			((CanvasItem)this).Visible = true;
			((NCardHolder)this).SetClickable(true);
			((Control)((NCardHolder)this).Hitbox).MouseDefaultCursorShape = (CursorShape)0;
			_isPreviewingUpgrade = false;
		}
	}

	public void OnFreedToPool()
	{
	}

	public static void InitPool()
	{
		NodePool.Init<NCustomCardHolder>(ScenePath, 30);
	}

	public static NCustomCardHolder? Create(NCard cardNode, float customSmallScale = 1f, float customHoverScale = 1f)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		if (TestMode.IsOn)
		{
			return null;
		}
		NCustomCardHolder nCustomCardHolder = NodePool.Get<NCustomCardHolder>();
		nCustomCardHolder._smallScale = customSmallScale;
		nCustomCardHolder._hoverScale = customHoverScale;
		((NCardHolder)nCustomCardHolder).SetCard(cardNode);
		nCustomCardHolder.UpdateName();
		((Control)nCustomCardHolder).Scale = ((NCardHolder)nCustomCardHolder).SmallScale;
		return nCustomCardHolder;
	}

	public override void _Ready()
	{
		bool isPreviewingUpgrade = _isPreviewingUpgrade;
		_isPreviewingUpgrade = false;
		SetIsPreviewingUpgrade(isPreviewingUpgrade);
		((NCardHolder)this).ConnectSignals();
	}

	protected override void OnFocus()
	{
		((NCardHolder)this).OnFocus();
		((CanvasItem)this).MoveToFront();
	}

	private void UpdateName()
	{
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(17, 1);
		defaultInterpolatedStringHandler.AppendLiteral("CustomCardHolder-");
		NCard cardNode = ((NCardHolder)this).CardNode;
		object value;
		if (cardNode == null)
		{
			value = null;
		}
		else
		{
			CardModel model = cardNode.Model;
			value = ((model != null) ? ((AbstractModel)model).Id : null);
		}
		defaultInterpolatedStringHandler.AppendFormatted<ModelId>((ModelId)value);
		((Node)this).Name = StringName.op_Implicit(defaultInterpolatedStringHandler.ToStringAndClear());
	}

	private void UpdateCardModel()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		NCard cardNode = ((NCardHolder)this).CardNode;
		CardModel val = (_baseCard = ((cardNode != null) ? cardNode.Model : null));
		if (val != null && val.IsUpgradable)
		{
			_upgradedCard = (CardModel)((AbstractModel)val).MutableClone();
			_upgradedCard.UpgradeInternal();
			if (((Node)this).IsNodeReady())
			{
				bool isPreviewingUpgrade = _isPreviewingUpgrade;
				_isPreviewingUpgrade = false;
				SetIsPreviewingUpgrade(isPreviewingUpgrade);
			}
		}
	}

	private void SetIsPreviewingUpgrade(bool showUpgradePreview)
	{
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		if (!((CanvasItem)this).Visible || _baseCard == null || ((NCardHolder)this).CardNode == null)
		{
			return;
		}
		if (!_baseCard.IsUpgradable && showUpgradePreview)
		{
			throw new InvalidExpressionException($"{((AbstractModel)_baseCard).Id} is not upgradable.");
		}
		if (_isPreviewingUpgrade != showUpgradePreview)
		{
			if (showUpgradePreview && _upgradedCard != null)
			{
				((NCardHolder)this).CardNode.Model = _upgradedCard;
				((NCardHolder)this).CardNode.ShowUpgradePreview();
			}
			else
			{
				((NCardHolder)this).CardNode.Model = _baseCard;
				((NCardHolder)this).CardNode.UpdateVisuals(((NCardHolder)this).CardNode.DisplayingPile, (CardPreviewMode)1);
			}
			_isPreviewingUpgrade = showUpgradePreview;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected O, but got Unknown
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Expected O, but got Unknown
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0223: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(10)
		{
			new MethodInfo(MethodName.OnInstantiated, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnReturnedFromPool, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnFreedToPool, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.InitPool, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.Create, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("cardNode"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
				new PropertyInfo((Type)3, StringName.op_Implicit("customSmallScale"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)3, StringName.op_Implicit("customHoverScale"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateName, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateCardModel, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.SetIsPreviewingUpgrade, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)1, StringName.op_Implicit("showUpgradePreview"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01af: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.OnInstantiated && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnInstantiated();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnReturnedFromPool && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnReturnedFromPool();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnFreedToPool && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			OnFreedToPool();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.InitPool && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			InitPool();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			NCustomCardHolder nCustomCardHolder = Create(VariantUtils.ConvertTo<NCard>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = VariantUtils.CreateFrom<NCustomCardHolder>(ref nCustomCardHolder);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.OnFocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((NCardHolder)this).OnFocus();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateName && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateName();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateCardModel && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			UpdateCardModel();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetIsPreviewingUpgrade && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			SetIsPreviewingUpgrade(VariantUtils.ConvertTo<bool>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((NCardHolder)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.InitPool && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			InitPool();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Create && ((NativeVariantPtrArgs)(ref args)).Count == 3)
		{
			NCustomCardHolder nCustomCardHolder = Create(VariantUtils.ConvertTo<NCard>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[2]));
			ret = VariantUtils.CreateFrom<NCustomCardHolder>(ref nCustomCardHolder);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.OnInstantiated)
		{
			return true;
		}
		if ((ref method) == MethodName.OnReturnedFromPool)
		{
			return true;
		}
		if ((ref method) == MethodName.OnFreedToPool)
		{
			return true;
		}
		if ((ref method) == MethodName.InitPool)
		{
			return true;
		}
		if ((ref method) == MethodName.Create)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.OnFocus)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateName)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateCardModel)
		{
			return true;
		}
		if ((ref method) == MethodName.SetIsPreviewingUpgrade)
		{
			return true;
		}
		return ((NCardHolder)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._hoverScale)
		{
			_hoverScale = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._isPreviewingUpgrade)
		{
			_isPreviewingUpgrade = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._smallScale)
		{
			_smallScale = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		return ((NCardHolder)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.SmallScale)
		{
			Vector2 smallScale = ((NCardHolder)this).SmallScale;
			value = VariantUtils.CreateFrom<Vector2>(ref smallScale);
			return true;
		}
		if ((ref name) == PropertyName.HoverScale)
		{
			Vector2 smallScale = ((NCardHolder)this).HoverScale;
			value = VariantUtils.CreateFrom<Vector2>(ref smallScale);
			return true;
		}
		if ((ref name) == PropertyName._hoverScale)
		{
			value = VariantUtils.CreateFrom<float>(ref _hoverScale);
			return true;
		}
		if ((ref name) == PropertyName._isPreviewingUpgrade)
		{
			value = VariantUtils.CreateFrom<bool>(ref _isPreviewingUpgrade);
			return true;
		}
		if ((ref name) == PropertyName._smallScale)
		{
			value = VariantUtils.CreateFrom<float>(ref _smallScale);
			return true;
		}
		return ((NCardHolder)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName._hoverScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._isPreviewingUpgrade, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._smallScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName.SmallScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)5, PropertyName.HoverScale, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		((NCardHolder)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._hoverScale, Variant.From<float>(ref _hoverScale));
		info.AddProperty(PropertyName._isPreviewingUpgrade, Variant.From<bool>(ref _isPreviewingUpgrade));
		info.AddProperty(PropertyName._smallScale, Variant.From<float>(ref _smallScale));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((NCardHolder)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._hoverScale, ref val))
		{
			_hoverScale = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._isPreviewingUpgrade, ref val2))
		{
			_isPreviewingUpgrade = ((Variant)(ref val2)).As<bool>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._smallScale, ref val3))
		{
			_smallScale = ((Variant)(ref val3)).As<float>();
		}
	}
}
