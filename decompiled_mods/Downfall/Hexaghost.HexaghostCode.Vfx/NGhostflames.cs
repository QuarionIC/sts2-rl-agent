using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Downfall.DownfallCode.Utils.UI;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Hexaghost.HexaghostCode.Vfx;

[ScriptPath("res://HexaghostCode/Vfx/NGhostflames.cs")]
public class NGhostflames : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName Track = StringName.op_Implicit("Track");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName SetFirePosition = StringName.op_Implicit("SetFirePosition");

		public static readonly StringName GetFlameWorldPosition = StringName.op_Implicit("GetFlameWorldPosition");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName AllFires = StringName.op_Implicit("AllFires");

		public static readonly StringName _creatureNode = StringName.op_Implicit("_creatureNode");

		public static readonly StringName _fire1 = StringName.op_Implicit("_fire1");

		public static readonly StringName _fire2 = StringName.op_Implicit("_fire2");

		public static readonly StringName _fire3 = StringName.op_Implicit("_fire3");

		public static readonly StringName _fire4 = StringName.op_Implicit("_fire4");

		public static readonly StringName _fire5 = StringName.op_Implicit("_fire5");

		public static readonly StringName _fire6 = StringName.op_Implicit("_fire6");

		public static readonly StringName _hitboxAnchors = StringName.op_Implicit("_hitboxAnchors");

		public static readonly StringName _hitboxes = StringName.op_Implicit("_hitboxes");

		public static readonly StringName _intents = StringName.op_Implicit("_intents");

		public static readonly StringName _intentTween = StringName.op_Implicit("_intentTween");

		public static readonly StringName _loggedTrackState = StringName.op_Implicit("_loggedTrackState");

		public static readonly StringName _positionTween = StringName.op_Implicit("_positionTween");

		public static readonly StringName _reticles = StringName.op_Implicit("_reticles");

		public static readonly StringName _vfxContainer = StringName.op_Implicit("_vfxContainer");
	}

	public class SignalName : SignalName
	{
	}

	private static readonly Vector2 ReticleVisualSize = new Vector2(44f, 44f);

	private static readonly Vector2 ReticleCenterOffset = new Vector2(0f, -22f);

	private NCreature? _creatureNode;

	private GhostflameModel[]? _currentWheel;

	private NFire? _fire1;

	private NFire? _fire2;

	private NFire? _fire3;

	private NFire? _fire4;

	private NFire? _fire5;

	private NFire? _fire6;

	private Node2D?[] _hitboxAnchors = Array.Empty<Node2D>();

	private Control?[] _hitboxes = Array.Empty<Control>();

	private NIntent?[] _intents = Array.Empty<NIntent>();

	private Tween? _intentTween;

	private bool _loggedTrackState;

	private Player? _player;

	private Tween? _positionTween;

	private List<Control> _reachableHitboxes = new List<Control>();

	private NSelectionReticle?[] _reticles = Array.Empty<NSelectionReticle>();

	private Control? _vfxContainer;

	private NFire?[] AllFires => new NFire[6] { _fire1, _fire2, _fire3, _fire4, _fire5, _fire6 };

	public override void _Ready()
	{
		_fire1 = ((Node)this).GetNode<NFire>(NodePath.op_Implicit("%fire1"));
		_fire2 = ((Node)this).GetNode<NFire>(NodePath.op_Implicit("%fire2"));
		_fire3 = ((Node)this).GetNode<NFire>(NodePath.op_Implicit("%fire3"));
		_fire4 = ((Node)this).GetNode<NFire>(NodePath.op_Implicit("%fire4"));
		_fire5 = ((Node)this).GetNode<NFire>(NodePath.op_Implicit("%fire5"));
		_fire6 = ((Node)this).GetNode<NFire>(NodePath.op_Implicit("%fire6"));
		_intents = ((IEnumerable<NFire>)AllFires).Select((Func<NFire, int, NIntent>)delegate(NFire fire, int i)
		{
			if (fire == null)
			{
				return (NIntent)null;
			}
			NIntent val2 = NIntent.Create((float)i * 0.3f);
			((CanvasItem)val2).Visible = false;
			((Control)val2).MouseFilter = (MouseFilterEnum)2;
			((Node)this).AddChild((Node)(object)val2, false, (InternalMode)0);
			return val2;
		}).ToArray();
		_hitboxes = (Control?[])(object)new Control[AllFires.Length];
		_reticles = (NSelectionReticle?[])(object)new NSelectionReticle[AllFires.Length];
		_hitboxAnchors = ((IEnumerable<NFire>)AllFires).Select((Func<NFire, int, Node2D>)delegate(NFire fire, int i)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Expected O, but got Unknown
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected O, but got Unknown
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			if (fire == null)
			{
				return (Node2D)null;
			}
			Node2D val2 = new Node2D();
			((Node)this).AddChild((Node)(object)val2, false, (InternalMode)0);
			Control val3 = new Control();
			val3.CustomMinimumSize = new Vector2(80f, 80f);
			val3.Position = -val3.CustomMinimumSize / 2f;
			val3.MouseFilter = (MouseFilterEnum)0;
			((Node)val2).AddChild((Node)(object)val3, false, (InternalMode)0);
			_hitboxes[i] = val3;
			_reticles[i] = DownfallControllerNav.AttachFocusReticle((Node)(object)val2, ReticleCenterOffset, ReticleVisualSize, 4f);
			return val2;
		}).ToArray();
		_reachableHitboxes = _hitboxes.Where((Control h) => h != null).Cast<Control>().ToList();
		for (int num = 0; num < _hitboxes.Length; num++)
		{
			Control val = _hitboxes[num];
			if (val == null)
			{
				continue;
			}
			int index = num;
			NSelectionReticle reticle = _reticles[num];
			DownfallControllerNav.WireHover(val, delegate
			{
				//IL_006b: Unknown result type (might be due to invalid IL or missing references)
				NControllerManager instance = NControllerManager.Instance;
				if (instance != null && instance.IsUsingController)
				{
					NSelectionReticle obj = reticle;
					if (obj != null)
					{
						obj.OnSelect();
					}
				}
				GhostflameModel ghostflameModel = _currentWheel?.ElementAtOrDefault(index);
				if (ghostflameModel != null)
				{
					NCombatRoom instance2 = NCombatRoom.Instance;
					if (instance2 != null)
					{
						NCreature creatureNode = instance2.GetCreatureNode(_player.Creature);
						if (creatureNode != null)
						{
							creatureNode.ShowHoverTips((IEnumerable<IHoverTip>)new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>((IHoverTip)(object)ghostflameModel.HoverTip));
						}
					}
				}
			}, delegate
			{
				NSelectionReticle obj = reticle;
				if (obj != null)
				{
					obj.OnDeselect();
				}
				NCombatRoom instance = NCombatRoom.Instance;
				if (instance != null)
				{
					NCreature creatureNode = instance.GetCreatureNode(_player.Creature);
					if (creatureNode != null)
					{
						creatureNode.HideHoverTips();
					}
				}
			});
		}
		DownfallControllerNav.WireChain(_reachableHitboxes, wrap: true);
	}

	public void Track(NCreature creatureNode, Control vfxContainer)
	{
		_creatureNode = creatureNode;
		_vfxContainer = vfxContainer;
		DownfallControllerNav.LinkAbove(_reachableHitboxes, creatureNode.Hitbox);
	}

	public override void _Process(double delta)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_015e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_018a: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		if (_creatureNode == null || _vfxContainer == null)
		{
			return;
		}
		Transform2D globalTransform = ((CanvasItem)_creatureNode).GetGlobalTransform();
		Transform2D globalTransform2 = ((CanvasItem)_vfxContainer).GetGlobalTransform();
		Vector2 scale = ((Transform2D)(ref globalTransform2)).Scale;
		float num = Mathf.Abs(((Transform2D)(ref globalTransform)).Scale.X / scale.X);
		float num2 = Mathf.Abs(((Transform2D)(ref globalTransform)).Scale.Y / scale.Y);
		float num3 = _creatureNode._tempScale * num;
		float num4 = _creatureNode._tempScale * num2;
		if (GodotObject.IsInstanceValid((GodotObject)(object)_creatureNode) && _vfxContainer != null)
		{
			((Control)this).Scale = new Vector2(num3, num4);
			Vector2 val = ((Control)_creatureNode).GlobalPosition + Vector2.Up * 216f * num4;
			globalTransform2 = ((CanvasItem)_vfxContainer).GetGlobalTransform();
			((Control)this).Position = ((Transform2D)(ref globalTransform2)).AffineInverse() * val;
		}
		if (!_loggedTrackState)
		{
			_loggedTrackState = true;
			GD.Print($"[Ghostflames] tracking={_creatureNode != null && GodotObject.IsInstanceValid((GodotObject)(object)_creatureNode)}");
		}
		for (int i = 0; i < _intents.Length; i++)
		{
			NFire nFire = AllFires[i];
			if (nFire != null)
			{
				Vector2 globalPosition = ((Node2D)nFire).GlobalPosition + Vector2.Up * 130f * num4 + Vector2.Left * 25f * num3;
				NIntent val2 = _intents[i];
				if (val2 != null)
				{
					((Control)val2).GlobalPosition = globalPosition;
					((Control)val2).Rotation = 0f - ((Control)this).Rotation;
				}
				if (_hitboxAnchors[i] != null)
				{
					_hitboxAnchors[i].GlobalPosition = ((Node2D)nFire).GlobalPosition;
					_hitboxAnchors[i].Rotation = 0f - ((Control)this).Rotation;
				}
			}
		}
	}

	private void SetFirePosition(int fireIndex, float duration = 0.5f)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		Tween? positionTween = _positionTween;
		if (positionTween != null)
		{
			positionTween.Kill();
		}
		double num = (0.0 - ((double)fireIndex - 0.5)) * 6.2831854820251465 / 6.0;
		float rotation = ((Control)this).Rotation;
		double num2 = Mathf.AngleDifference((double)rotation, num);
		double num3 = (double)rotation + num2;
		_positionTween = ((Node)this).CreateTween().SetParallel(true);
		_positionTween.TweenProperty((GodotObject)(object)this, NodePath.op_Implicit("rotation"), Variant.op_Implicit(num3), (double)duration).SetTrans((TransitionType)1).SetEase((EaseType)2);
		NFire[] allFires = AllFires;
		foreach (NFire nFire in allFires)
		{
			_positionTween.TweenProperty((GodotObject)(object)nFire, NodePath.op_Implicit("rotation"), Variant.op_Implicit(0.0 - num3), (double)duration).SetTrans((TransitionType)1).SetEase((EaseType)2);
		}
		NIntent[] intents = _intents;
		foreach (NIntent val in intents)
		{
			if (val != null)
			{
				_positionTween.TweenProperty((GodotObject)(object)val, NodePath.op_Implicit("rotation"), Variant.op_Implicit(0.0 - num3), (double)duration).SetTrans((TransitionType)1).SetEase((EaseType)2);
			}
		}
	}

	public void RefreshWheel(GhostflameModel[] wheel, int currentIndex, Player player)
	{
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		_currentWheel = wheel;
		_player = player;
		for (int i = 0; i < wheel.Length; i++)
		{
			AllFires[i]?.SetState(wheel[i].FireColor, (!wheel[i].IsIgnited) ? NFire.FireSize.Small : NFire.FireSize.Large);
			if (_intents[i] != null)
			{
				_intents[i].UpdateIntent(wheel[i].Intent, (IEnumerable<Creature>)Array.Empty<Creature>(), player.Creature);
			}
		}
		Tween? intentTween = _intentTween;
		if (intentTween != null)
		{
			intentTween.Kill();
		}
		_intentTween = ((Node)this).CreateTween().SetParallel(true);
		for (int j = 0; j < _intents.Length; j++)
		{
			if (_intents[j] != null)
			{
				float num = ((j == currentIndex) ? 1f : 0f);
				((CanvasItem)_intents[j]).Visible = true;
				_intentTween.TweenProperty((GodotObject)(object)_intents[j], NodePath.op_Implicit("modulate:a"), Variant.op_Implicit(num), 0.30000001192092896).SetTrans((TransitionType)1).SetEase((EaseType)2);
			}
		}
		SetFirePosition(currentIndex);
		if (_creatureNode != null)
		{
			DownfallControllerNav.LinkAbove(_reachableHitboxes, _creatureNode.Hitbox, currentIndex);
		}
	}

	public void RefreshCurrentIntent(GhostflameModel[] wheel, int currentIndex, Player player)
	{
		_intents[currentIndex].UpdateIntent(wheel[currentIndex].Intent, (IEnumerable<Creature>)Array.Empty<Creature>(), player.Creature);
	}

	public Vector2 GetFlameWorldPosition(int index)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		NFire? obj = AllFires[index];
		if (obj == null)
		{
			return ((Control)this).GlobalPosition;
		}
		return ((Node2D)obj).GlobalPosition;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_010a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_017f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(5)
		{
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.Track, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("creatureNode"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false),
				new PropertyInfo((Type)24, StringName.op_Implicit("vfxContainer"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Control"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.SetFirePosition, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("fireIndex"), (PropertyHint)0, "", (PropertyUsageFlags)6, false),
				new PropertyInfo((Type)3, StringName.op_Implicit("duration"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.GetFlameWorldPosition, new PropertyInfo((Type)5, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)2, StringName.op_Implicit("index"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.Track && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			Track(VariantUtils.ConvertTo<NCreature>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<Control>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.SetFirePosition && ((NativeVariantPtrArgs)(ref args)).Count == 2)
		{
			SetFirePosition(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]), VariantUtils.ConvertTo<float>(ref ((NativeVariantPtrArgs)(ref args))[1]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetFlameWorldPosition && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Vector2 flameWorldPosition = GetFlameWorldPosition(VariantUtils.ConvertTo<int>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<Vector2>(ref flameWorldPosition);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName.Track)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.SetFirePosition)
		{
			return true;
		}
		if ((ref method) == MethodName.GetFlameWorldPosition)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName._creatureNode)
		{
			_creatureNode = VariantUtils.ConvertTo<NCreature>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fire1)
		{
			_fire1 = VariantUtils.ConvertTo<NFire>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fire2)
		{
			_fire2 = VariantUtils.ConvertTo<NFire>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fire3)
		{
			_fire3 = VariantUtils.ConvertTo<NFire>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fire4)
		{
			_fire4 = VariantUtils.ConvertTo<NFire>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fire5)
		{
			_fire5 = VariantUtils.ConvertTo<NFire>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._fire6)
		{
			_fire6 = VariantUtils.ConvertTo<NFire>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hitboxAnchors)
		{
			_hitboxAnchors = VariantUtils.ConvertToSystemArrayOfGodotObject<Node2D>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._hitboxes)
		{
			_hitboxes = VariantUtils.ConvertToSystemArrayOfGodotObject<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._intents)
		{
			_intents = VariantUtils.ConvertToSystemArrayOfGodotObject<NIntent>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._intentTween)
		{
			_intentTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._loggedTrackState)
		{
			_loggedTrackState = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._positionTween)
		{
			_positionTween = VariantUtils.ConvertTo<Tween>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._reticles)
		{
			_reticles = VariantUtils.ConvertToSystemArrayOfGodotObject<NSelectionReticle>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._vfxContainer)
		{
			_vfxContainer = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.AllFires)
		{
			GodotObject[] allFires = (GodotObject[])(object)AllFires;
			value = VariantUtils.CreateFromSystemArrayOfGodotObject(allFires);
			return true;
		}
		if ((ref name) == PropertyName._creatureNode)
		{
			value = VariantUtils.CreateFrom<NCreature>(ref _creatureNode);
			return true;
		}
		if ((ref name) == PropertyName._fire1)
		{
			value = VariantUtils.CreateFrom<NFire>(ref _fire1);
			return true;
		}
		if ((ref name) == PropertyName._fire2)
		{
			value = VariantUtils.CreateFrom<NFire>(ref _fire2);
			return true;
		}
		if ((ref name) == PropertyName._fire3)
		{
			value = VariantUtils.CreateFrom<NFire>(ref _fire3);
			return true;
		}
		if ((ref name) == PropertyName._fire4)
		{
			value = VariantUtils.CreateFrom<NFire>(ref _fire4);
			return true;
		}
		if ((ref name) == PropertyName._fire5)
		{
			value = VariantUtils.CreateFrom<NFire>(ref _fire5);
			return true;
		}
		if ((ref name) == PropertyName._fire6)
		{
			value = VariantUtils.CreateFrom<NFire>(ref _fire6);
			return true;
		}
		if ((ref name) == PropertyName._hitboxAnchors)
		{
			GodotObject[] allFires = (GodotObject[])(object)_hitboxAnchors;
			value = VariantUtils.CreateFromSystemArrayOfGodotObject(allFires);
			return true;
		}
		if ((ref name) == PropertyName._hitboxes)
		{
			GodotObject[] allFires = (GodotObject[])(object)_hitboxes;
			value = VariantUtils.CreateFromSystemArrayOfGodotObject(allFires);
			return true;
		}
		if ((ref name) == PropertyName._intents)
		{
			GodotObject[] allFires = (GodotObject[])(object)_intents;
			value = VariantUtils.CreateFromSystemArrayOfGodotObject(allFires);
			return true;
		}
		if ((ref name) == PropertyName._intentTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _intentTween);
			return true;
		}
		if ((ref name) == PropertyName._loggedTrackState)
		{
			value = VariantUtils.CreateFrom<bool>(ref _loggedTrackState);
			return true;
		}
		if ((ref name) == PropertyName._positionTween)
		{
			value = VariantUtils.CreateFrom<Tween>(ref _positionTween);
			return true;
		}
		if ((ref name) == PropertyName._reticles)
		{
			GodotObject[] allFires = (GodotObject[])(object)_reticles;
			value = VariantUtils.CreateFromSystemArrayOfGodotObject(allFires);
			return true;
		}
		if ((ref name) == PropertyName._vfxContainer)
		{
			value = VariantUtils.CreateFrom<Control>(ref _vfxContainer);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0103: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_020a: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)24, PropertyName._creatureNode, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fire1, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fire2, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fire3, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fire4, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fire5, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._fire6, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)28, PropertyName._hitboxAnchors, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)28, PropertyName._hitboxes, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)28, PropertyName._intents, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._intentTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName._loggedTrackState, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._positionTween, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)28, PropertyName._reticles, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._vfxContainer, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)28, PropertyName.AllFires, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0139: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._creatureNode, Variant.From<NCreature>(ref _creatureNode));
		info.AddProperty(PropertyName._fire1, Variant.From<NFire>(ref _fire1));
		info.AddProperty(PropertyName._fire2, Variant.From<NFire>(ref _fire2));
		info.AddProperty(PropertyName._fire3, Variant.From<NFire>(ref _fire3));
		info.AddProperty(PropertyName._fire4, Variant.From<NFire>(ref _fire4));
		info.AddProperty(PropertyName._fire5, Variant.From<NFire>(ref _fire5));
		info.AddProperty(PropertyName._fire6, Variant.From<NFire>(ref _fire6));
		StringName hitboxAnchors = PropertyName._hitboxAnchors;
		GodotObject[] hitboxAnchors2 = (GodotObject[])(object)_hitboxAnchors;
		info.AddProperty(hitboxAnchors, Variant.CreateFrom(hitboxAnchors2));
		StringName hitboxes = PropertyName._hitboxes;
		hitboxAnchors2 = (GodotObject[])(object)_hitboxes;
		info.AddProperty(hitboxes, Variant.CreateFrom(hitboxAnchors2));
		StringName intents = PropertyName._intents;
		hitboxAnchors2 = (GodotObject[])(object)_intents;
		info.AddProperty(intents, Variant.CreateFrom(hitboxAnchors2));
		info.AddProperty(PropertyName._intentTween, Variant.From<Tween>(ref _intentTween));
		info.AddProperty(PropertyName._loggedTrackState, Variant.From<bool>(ref _loggedTrackState));
		info.AddProperty(PropertyName._positionTween, Variant.From<Tween>(ref _positionTween));
		StringName reticles = PropertyName._reticles;
		hitboxAnchors2 = (GodotObject[])(object)_reticles;
		info.AddProperty(reticles, Variant.CreateFrom(hitboxAnchors2));
		info.AddProperty(PropertyName._vfxContainer, Variant.From<Control>(ref _vfxContainer));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._creatureNode, ref val))
		{
			_creatureNode = ((Variant)(ref val)).As<NCreature>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._fire1, ref val2))
		{
			_fire1 = ((Variant)(ref val2)).As<NFire>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._fire2, ref val3))
		{
			_fire2 = ((Variant)(ref val3)).As<NFire>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._fire3, ref val4))
		{
			_fire3 = ((Variant)(ref val4)).As<NFire>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._fire4, ref val5))
		{
			_fire4 = ((Variant)(ref val5)).As<NFire>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._fire5, ref val6))
		{
			_fire5 = ((Variant)(ref val6)).As<NFire>();
		}
		Variant val7 = default(Variant);
		if (info.TryGetProperty(PropertyName._fire6, ref val7))
		{
			_fire6 = ((Variant)(ref val7)).As<NFire>();
		}
		Variant val8 = default(Variant);
		if (info.TryGetProperty(PropertyName._hitboxAnchors, ref val8))
		{
			_hitboxAnchors = ((Variant)(ref val8)).AsGodotObjectArray<Node2D>();
		}
		Variant val9 = default(Variant);
		if (info.TryGetProperty(PropertyName._hitboxes, ref val9))
		{
			_hitboxes = ((Variant)(ref val9)).AsGodotObjectArray<Control>();
		}
		Variant val10 = default(Variant);
		if (info.TryGetProperty(PropertyName._intents, ref val10))
		{
			_intents = ((Variant)(ref val10)).AsGodotObjectArray<NIntent>();
		}
		Variant val11 = default(Variant);
		if (info.TryGetProperty(PropertyName._intentTween, ref val11))
		{
			_intentTween = ((Variant)(ref val11)).As<Tween>();
		}
		Variant val12 = default(Variant);
		if (info.TryGetProperty(PropertyName._loggedTrackState, ref val12))
		{
			_loggedTrackState = ((Variant)(ref val12)).As<bool>();
		}
		Variant val13 = default(Variant);
		if (info.TryGetProperty(PropertyName._positionTween, ref val13))
		{
			_positionTween = ((Variant)(ref val13)).As<Tween>();
		}
		Variant val14 = default(Variant);
		if (info.TryGetProperty(PropertyName._reticles, ref val14))
		{
			_reticles = ((Variant)(ref val14)).AsGodotObjectArray<NSelectionReticle>();
		}
		Variant val15 = default(Variant);
		if (info.TryGetProperty(PropertyName._vfxContainer, ref val15))
		{
			_vfxContainer = ((Variant)(ref val15)).As<Control>();
		}
	}
}
