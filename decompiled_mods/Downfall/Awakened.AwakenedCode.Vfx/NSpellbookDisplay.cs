using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Interfaces;
using Awakened.AwakenedCode.Piles;
using Downfall.DownfallCode.Utils.UI;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Awakened.AwakenedCode.Vfx;

[ScriptPath("res://AwakenedCode/Vfx/NSpellbookDisplay.cs")]
public class NSpellbookDisplay : Control
{
	private class SpellIconControl : NClickableControl
	{
		public class MethodName : MethodName
		{
			public static readonly StringName SetReticle = StringName.op_Implicit("SetReticle");

			public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

			public static readonly StringName OnFocus = StringName.op_Implicit("OnFocus");

			public static readonly StringName OnUnfocus = StringName.op_Implicit("OnUnfocus");
		}

		public class PropertyName : PropertyName
		{
			public static readonly StringName _reticle = StringName.op_Implicit("_reticle");
		}

		public class SignalName : SignalName
		{
		}

		private NSelectionReticle? _reticle;

		private IHoverTip? _tip;

		private Func<IHoverTip>? _tipProvider;

		public void SetTipProvider(Func<IHoverTip> provider)
		{
			_tipProvider = provider;
		}

		public void SetReticle(NSelectionReticle reticle)
		{
			_reticle = reticle;
		}

		public override void _Ready()
		{
			((NClickableControl)this).ConnectSignals();
		}

		protected override void OnFocus()
		{
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			NControllerManager instance = NControllerManager.Instance;
			if (instance != null && instance.IsUsingController)
			{
				NSelectionReticle? reticle = _reticle;
				if (reticle != null)
				{
					reticle.OnSelect();
				}
			}
			_tip = _tipProvider?.Invoke();
			if (_tip != null)
			{
				NHoverTipSet obj = NHoverTipSet.CreateAndShow((Control)(object)this, _tip, (HoverTipAlignment)0);
				if (obj != null)
				{
					((Control)obj).SetGlobalPosition(((Control)this).GlobalPosition + new Vector2(0f, ((Control)this).Size.Y + 20f), false);
				}
			}
		}

		protected override void OnUnfocus()
		{
			NSelectionReticle? reticle = _reticle;
			if (reticle != null)
			{
				reticle.OnDeselect();
			}
			NHoverTipSet.Remove((Control)(object)this);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<MethodInfo> GetGodotMethodList()
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Expected O, but got Unknown
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			return new List<MethodInfo>(4)
			{
				new MethodInfo(MethodName.SetReticle, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
				{
					new PropertyInfo((Type)24, StringName.op_Implicit("reticle"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
				}, (List<Variant>)null),
				new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnFocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
				new MethodInfo(MethodName.OnUnfocus, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
		{
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			if ((ref method) == MethodName.SetReticle && ((NativeVariantPtrArgs)(ref args)).Count == 1)
			{
				SetReticle(VariantUtils.ConvertTo<NSelectionReticle>(ref ((NativeVariantPtrArgs)(ref args))[0]));
				ret = default(godot_variant);
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
				((NClickableControl)this).OnFocus();
				ret = default(godot_variant);
				return true;
			}
			if ((ref method) == MethodName.OnUnfocus && ((NativeVariantPtrArgs)(ref args)).Count == 0)
			{
				((NClickableControl)this).OnUnfocus();
				ret = default(godot_variant);
				return true;
			}
			return ((NClickableControl)this).InvokeGodotClassMethod(ref method, args, ref ret);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool HasGodotClassMethod(in godot_string_name method)
		{
			if ((ref method) == MethodName.SetReticle)
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
			if ((ref method) == MethodName.OnUnfocus)
			{
				return true;
			}
			return ((NClickableControl)this).HasGodotClassMethod(ref method);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
		{
			if ((ref name) == PropertyName._reticle)
			{
				_reticle = VariantUtils.ConvertTo<NSelectionReticle>(ref value);
				return true;
			}
			return ((NClickableControl)this).SetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			if ((ref name) == PropertyName._reticle)
			{
				value = VariantUtils.CreateFrom<NSelectionReticle>(ref _reticle);
				return true;
			}
			return ((NClickableControl)this).GetGodotClassPropertyValue(ref name, ref value);
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		internal static List<PropertyInfo> GetGodotPropertyList()
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			return new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, PropertyName._reticle, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
			};
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void SaveGodotObjectData(GodotSerializationInfo info)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			((NClickableControl)this).SaveGodotObjectData(info);
			info.AddProperty(PropertyName._reticle, Variant.From<NSelectionReticle>(ref _reticle));
		}

		[EditorBrowsable(EditorBrowsableState.Never)]
		protected override void RestoreGodotObjectData(GodotSerializationInfo info)
		{
			((NClickableControl)this).RestoreGodotObjectData(info);
			Variant val = default(Variant);
			if (info.TryGetProperty(PropertyName._reticle, ref val))
			{
				_reticle = ((Variant)(ref val)).As<NSelectionReticle>();
			}
		}
	}

	public class MethodName : MethodName
	{
		public static readonly StringName BuildNextSpellBorderStyle = StringName.op_Implicit("BuildNextSpellBorderStyle");

		public static readonly StringName Refresh = StringName.op_Implicit("Refresh");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName _bobOffsets = StringName.op_Implicit("_bobOffsets");

		public static readonly StringName _bobSpeeds = StringName.op_Implicit("_bobSpeeds");

		public static readonly StringName _bobTime = StringName.op_Implicit("_bobTime");

		public static readonly StringName _creatureHitbox = StringName.op_Implicit("_creatureHitbox");
	}

	public class SignalName : SignalName
	{
	}

	private const float IconSize = 64f;

	private const float IconDistance = 76f;

	private const int NextSpellBorderWidth = 3;

	private const int NextSpellBorderRadius = 10;

	private const float NextSpellBorderPadding = 2f;

	private static readonly Color NextSpellBorderColor = Colors.Gold;

	private static readonly StyleBoxFlat NextSpellBorderStyle = BuildNextSpellBorderStyle();

	private readonly float[] _bobOffsets = new float[8];

	private readonly float[] _bobSpeeds = new float[8] { 1.1f, 0.9f, 1.05f, 0.95f, 1f, 0.85f, 1.15f, 0.98f };

	private readonly List<TextureRect> _iconNodes = new List<TextureRect>();

	private readonly List<SpellIconControl> _iconWrappers = new List<SpellIconControl>();

	private float _bobTime;

	private Player? _trackedPlayer;

	private Control? _creatureHitbox;

	public static NSpellbookDisplay Create(Player player)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(player.Creature) : null);
		NSpellbookDisplay obj = new NSpellbookDisplay
		{
			_trackedPlayer = player
		};
		((Control)obj).Position = Vector2.Zero;
		obj._creatureHitbox = ((val != null) ? val.Hitbox : null);
		return obj;
	}

	private static StyleBoxFlat BuildNextSpellBorderStyle()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		StyleBoxFlat val = new StyleBoxFlat
		{
			DrawCenter = false,
			BorderColor = NextSpellBorderColor
		};
		val.SetBorderWidthAll(3);
		val.SetCornerRadiusAll(10);
		return val;
	}

	public void Refresh()
	{
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Expected O, but got Unknown
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_021a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_022a: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Expected O, but got Unknown
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0335: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_034b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0350: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Expected O, but got Unknown
		if (_trackedPlayer == null)
		{
			return;
		}
		foreach (TextureRect iconNode in _iconNodes)
		{
			((Node)iconNode).QueueFree();
		}
		_iconNodes.Clear();
		foreach (SpellIconControl iconWrapper in _iconWrappers)
		{
			((Node)iconWrapper).QueueFree();
		}
		_iconWrappers.Clear();
		AwakenedPile spellbookOrThrow = AwakenedCmd.GetSpellbookOrThrow(_trackedPlayer);
		List<IGrouping<ModelId, CardModel>> list = (from c in ((CardPile)spellbookOrThrow).Cards
			group c by ((AbstractModel)c).Id).ToList();
		Vector2 val = default(Vector2);
		Vector2 position = default(Vector2);
		Vector2 val4 = default(Vector2);
		for (int num = 0; num < list.Count; num++)
		{
			IGrouping<ModelId, CardModel> source = list[num];
			CardModel firstCard = source.First();
			int num2 = source.Count();
			if (!(firstCard is ISpell spell))
			{
				continue;
			}
			string spellIconPath = spell.SpellIconPath;
			if (ResourceLoader.Exists(spellIconPath, ""))
			{
				bool flag = firstCard == spellbookOrThrow.NextSpell || source.Contains(spellbookOrThrow.NextSpell);
				((Vector2)(ref val))._002Ector(64f + (float)(flag ? 12 : 0), 64f + (float)(flag ? 12 : 0));
				((Vector2)(ref position))._002Ector((float)num * 76f - (float)(flag ? 6 : 0), (float)(flag ? (-6) : 0));
				TextureRect val2 = new TextureRect
				{
					Texture = ResourceLoader.Load<Texture2D>(spellIconPath, (string)null, (CacheMode)1),
					StretchMode = (StretchModeEnum)4,
					CustomMinimumSize = val,
					Size = val
				};
				SpellIconControl spellIconControl = new SpellIconControl();
				((Control)spellIconControl).Size = val;
				((Control)spellIconControl).CustomMinimumSize = val;
				((Control)spellIconControl).Position = position;
				((Control)spellIconControl).MouseFilter = (MouseFilterEnum)0;
				SpellIconControl spellIconControl2 = spellIconControl;
				if (num2 > 1)
				{
					Label val3 = new Label
					{
						Text = $"{num2}x",
						HorizontalAlignment = (HorizontalAlignment)2,
						VerticalAlignment = (VerticalAlignment)2,
						Size = ((Control)val2).CustomMinimumSize,
						Position = new Vector2(4f, 4f)
					};
					((Control)val3).AddThemeColorOverride(StringName.op_Implicit("font_outline_color"), Colors.Black);
					((Control)val3).AddThemeConstantOverride(StringName.op_Implicit("outline_size"), 4);
					((Node)val2).AddChild((Node)(object)val3, false, (InternalMode)0);
				}
				spellIconControl2.SetTipProvider(() => HoverTipFactory.FromCard(firstCard, false));
				((Node)spellIconControl2).AddChild((Node)(object)val2, false, (InternalMode)0);
				if (flag)
				{
					((Vector2)(ref val4))._002Ector(2f, 2f);
					Panel val5 = new Panel
					{
						Position = -val4,
						Size = val + val4 * 2f,
						MouseFilter = (MouseFilterEnum)2
					};
					((Control)val5).AddThemeStyleboxOverride(StringName.op_Implicit("panel"), (StyleBox)(object)NextSpellBorderStyle);
					((Node)spellIconControl2).AddChild((Node)(object)val5, false, (InternalMode)0);
				}
				((Node)this).AddChild((Node)(object)spellIconControl2, false, (InternalMode)0);
				_iconNodes.Add(val2);
				_iconWrappers.Add(spellIconControl2);
				NSelectionReticle reticle = DownfallControllerNav.AttachFocusReticle((Node)(object)spellIconControl2, val / 2f + new Vector2(-1f, -3f), val, 1f);
				spellIconControl2.SetReticle(reticle);
			}
		}
		DownfallControllerNav.WireChain((IReadOnlyList<Control>)_iconWrappers, wrap: true);
		if (_creatureHitbox != null)
		{
			DownfallControllerNav.LinkAbove((IReadOnlyList<Control>)_iconWrappers, _creatureHitbox);
		}
	}

	public override void _Process(double delta)
	{
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		if (_trackedPlayer != null && CombatManager.Instance.IsInProgress)
		{
			_bobTime += (float)delta;
			for (int i = 0; i < _bobOffsets.Length; i++)
			{
				_bobOffsets[i] = Mathf.Sin(_bobTime * _bobSpeeds[i] * (float)Math.PI) * 4f;
			}
			for (int j = 0; j < _iconWrappers.Count; j++)
			{
				bool flag = ((Control)_iconWrappers[j]).CustomMinimumSize.X > 64f;
				((Control)_iconWrappers[j]).Position = new Vector2((float)j * 76f - (float)(flag ? 6 : 0), ((j < _bobOffsets.Length) ? _bobOffsets[j] : 0f) - (float)(flag ? 6 : 0));
			}
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(3)
		{
			new MethodInfo(MethodName.BuildNextSpellBorderStyle, new PropertyInfo((Type)24, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("StyleBoxFlat"), false), (MethodFlags)33, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.Refresh, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
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
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.BuildNextSpellBorderStyle && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			StyleBoxFlat val = BuildNextSpellBorderStyle();
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(ref val);
			return true;
		}
		if ((ref method) == MethodName.Refresh && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			Refresh();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.BuildNextSpellBorderStyle && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			StyleBoxFlat val = BuildNextSpellBorderStyle();
			ret = VariantUtils.CreateFrom<StyleBoxFlat>(ref val);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.BuildNextSpellBorderStyle)
		{
			return true;
		}
		if ((ref method) == MethodName.Refresh)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._bobTime)
		{
			_bobTime = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._creatureHitbox)
		{
			_creatureHitbox = VariantUtils.ConvertTo<Control>(ref value);
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
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._bobOffsets)
		{
			value = VariantUtils.CreateFrom<float[]>(ref _bobOffsets);
			return true;
		}
		if ((ref name) == PropertyName._bobSpeeds)
		{
			value = VariantUtils.CreateFrom<float[]>(ref _bobSpeeds);
			return true;
		}
		if ((ref name) == PropertyName._bobTime)
		{
			value = VariantUtils.CreateFrom<float>(ref _bobTime);
			return true;
		}
		if ((ref name) == PropertyName._creatureHitbox)
		{
			value = VariantUtils.CreateFrom<Control>(ref _creatureHitbox);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)32, PropertyName._bobOffsets, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)32, PropertyName._bobSpeeds, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._bobTime, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._creatureHitbox, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._bobTime, Variant.From<float>(ref _bobTime));
		info.AddProperty(PropertyName._creatureHitbox, Variant.From<Control>(ref _creatureHitbox));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._bobTime, ref val))
		{
			_bobTime = ((Variant)(ref val)).As<float>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._creatureHitbox, ref val2))
		{
			_creatureHitbox = ((Variant)(ref val2)).As<Control>();
		}
	}
}
