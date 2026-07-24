using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Interfaces;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;

namespace Snecko.SneckoCode.Vfx;

[ScriptPath("res://SneckoCode/Vfx/NSneckoCharacterSelect.cs")]
public class NSneckoCharacterSelect : Control, IOverlayScreen, IScreenContext
{
	public class MethodName : MethodName
	{
		public static readonly StringName AfterOverlayOpened = StringName.op_Implicit("AfterOverlayOpened");

		public static readonly StringName AfterOverlayClosed = StringName.op_Implicit("AfterOverlayClosed");

		public static readonly StringName AfterOverlayShown = StringName.op_Implicit("AfterOverlayShown");

		public static readonly StringName AfterOverlayHidden = StringName.op_Implicit("AfterOverlayHidden");

		public static readonly StringName _Ready = StringName.op_Implicit("_Ready");

		public static readonly StringName _Input = StringName.op_Implicit("_Input");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName GetBoundsRect = StringName.op_Implicit("GetBoundsRect");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName DefaultFocusedControl = StringName.op_Implicit("DefaultFocusedControl");

		public static readonly StringName ScreenType = StringName.op_Implicit("ScreenType");

		public static readonly StringName UseSharedBackstop = StringName.op_Implicit("UseSharedBackstop");

		public static readonly StringName _animating = StringName.op_Implicit("_animating");

		public static readonly StringName _bounds1 = StringName.op_Implicit("_bounds1");

		public static readonly StringName _bounds2 = StringName.op_Implicit("_bounds2");

		public static readonly StringName _controllerIndex = StringName.op_Implicit("_controllerIndex");

		public static readonly StringName _visuals1 = StringName.op_Implicit("_visuals1");

		public static readonly StringName _visuals2 = StringName.op_Implicit("_visuals2");
	}

	public class SignalName : SignalName
	{
	}

	private static readonly Vector2 Slot1Offset = new Vector2(-250f, 100f);

	private static readonly Vector2 Slot2Offset = new Vector2(250f, 100f);

	private bool _animating;

	private Rect2 _bounds1;

	private Rect2 _bounds2;

	private int _controllerIndex;

	private TaskCompletionSource<int>? _selectionTcs;

	private Action<string> _trigger1 = delegate
	{
	};

	private Action<string> _trigger2 = delegate
	{
	};

	private NCreatureVisuals? _visuals1;

	private NCreatureVisuals? _visuals2;

	public Control? DefaultFocusedControl => null;

	public NetScreenType ScreenType => (NetScreenType)0;

	public bool UseSharedBackstop => true;

	public void AfterOverlayOpened()
	{
	}

	public void AfterOverlayClosed()
	{
	}

	public void AfterOverlayShown()
	{
		((CanvasItem)this).Visible = true;
	}

	public void AfterOverlayHidden()
	{
		((CanvasItem)this).Visible = false;
	}

	public override void _Ready()
	{
		((Control)this).SetAnchorsPreset((LayoutPreset)15, false);
		((Control)this).MouseFilter = (MouseFilterEnum)0;
	}

	public override void _Input(InputEvent @event)
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Invalid comparison between Unknown and I8
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		if (_animating)
		{
			return;
		}
		if (@event.IsActionPressed(MegaInput.left, false, false) || @event.IsActionPressed(MegaInput.right, false, false))
		{
			_controllerIndex = ((_controllerIndex == 0) ? 1 : 0);
			((Node)this).GetViewport().SetInputAsHandled();
			return;
		}
		if (@event.IsActionPressed(MegaInput.select, false, false))
		{
			((Node)this).GetViewport().SetInputAsHandled();
			_selectionTcs?.TrySetResult(_controllerIndex);
			return;
		}
		InputEventMouseButton val = (InputEventMouseButton)(object)((@event is InputEventMouseButton) ? @event : null);
		if (val != null && val.Pressed && (long)val.ButtonIndex == 1)
		{
			Vector2 globalPosition = ((InputEventMouse)val).GlobalPosition;
			if (_bounds1 != default(Rect2) && ((Rect2)(ref _bounds1)).HasPoint(globalPosition))
			{
				((Node)this).GetViewport().SetInputAsHandled();
				_selectionTcs?.TrySetResult(0);
			}
			else if (_bounds2 != default(Rect2) && ((Rect2)(ref _bounds2)).HasPoint(globalPosition))
			{
				((Node)this).GetViewport().SetInputAsHandled();
				_selectionTcs?.TrySetResult(1);
			}
		}
	}

	public override void _Process(double delta)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		if (!_animating)
		{
			Vector2 globalMousePosition = ((CanvasItem)this).GetGlobalMousePosition();
			bool flag = (_bounds1 != default(Rect2) && ((Rect2)(ref _bounds1)).HasPoint(globalMousePosition)) || _controllerIndex == 0;
			bool flag2 = (_bounds2 != default(Rect2) && ((Rect2)(ref _bounds2)).HasPoint(globalMousePosition)) || _controllerIndex == 1;
			if (_visuals1 != null)
			{
				((Node2D)_visuals1).Scale = (flag ? new Vector2(1.1f, 1.1f) : new Vector2(1f, 1f));
			}
			if (_visuals2 != null)
			{
				((Node2D)_visuals2).Scale = (flag2 ? new Vector2(-1.1f, 1.1f) : new Vector2(-1f, 1f));
			}
		}
	}

	public async Task<int> SelectOne(CharacterModel left, CharacterModel right)
	{
		NCreatureVisuals? visuals = _visuals1;
		if (visuals != null)
		{
			((Node)visuals).QueueFree();
		}
		NCreatureVisuals? visuals2 = _visuals2;
		if (visuals2 != null)
		{
			((Node)visuals2).QueueFree();
		}
		_bounds1 = default(Rect2);
		_bounds2 = default(Rect2);
		_animating = false;
		_controllerIndex = 0;
		Rect2 viewportRect = ((CanvasItem)this).GetViewportRect();
		Vector2 val = ((Rect2)(ref viewportRect)).Size / 2f;
		_visuals1 = TrySpawnVisuals(left, val + Slot1Offset, flipX: false);
		_visuals2 = TrySpawnVisuals(right, val + Slot2Offset, flipX: true);
		await ((GodotObject)this).ToSignal((GodotObject)(object)((Node)this).GetTree(), SignalName.ProcessFrame);
		await ((GodotObject)this).ToSignal((GodotObject)(object)((Node)this).GetTree(), SignalName.ProcessFrame);
		_trigger1 = ((_visuals1 != null) ? BuildTrigger(left, _visuals1) : ((Action<string>)delegate
		{
		}));
		_trigger2 = ((_visuals2 != null) ? BuildTrigger(right, _visuals2) : ((Action<string>)delegate
		{
		}));
		_trigger1("Idle");
		_trigger2("Idle");
		if (_visuals1 != null)
		{
			_bounds1 = GetBoundsRect(_visuals1);
		}
		if (_visuals2 != null)
		{
			_bounds2 = GetBoundsRect(_visuals2);
		}
		GD.Print($"Presenting {((AbstractModel)left).Id} vs {((AbstractModel)right).Id}");
		_selectionTcs = new TaskCompletionSource<int>();
		int chosen = await _selectionTcs.Task;
		GD.Print($"Player chose slot {chosen}");
		_animating = true;
		NCreatureVisuals val2 = ((chosen == 0) ? _visuals1 : _visuals2);
		Action<string> action = ((chosen == 0) ? _trigger1 : _trigger2);
		Action<string> action2 = ((chosen == 0) ? _trigger2 : _trigger1);
		CharacterModel val3 = ((chosen == 0) ? left : right);
		try
		{
			SfxCmd.Play(val3.AttackSfx, 1f);
		}
		catch (Exception ex)
		{
			GD.PrintErr("SFX failed: " + ex.Message);
		}
		action("Attack");
		action2("Hit");
		float num = 1.5f;
		try
		{
			if (val2 != null && GodotObject.IsInstanceValid((GodotObject)(object)val2) && val2.SpineBody != null)
			{
				SpineAnimationAccess spineAnimation = val2.SpineAnimation;
				MegaTrackEntry currentTrack = ((SpineAnimationAccess)(ref spineAnimation)).GetCurrentTrack(0);
				if (currentTrack != null)
				{
					num = Math.Max(currentTrack.GetAnimation().GetDuration(), 1f);
				}
			}
		}
		catch (Exception ex2)
		{
			GD.PrintErr("Failed to get anim length: " + ex2.Message);
		}
		await Cmd.Wait(num, false);
		return chosen;
	}

	private NCreatureVisuals? TrySpawnVisuals(CharacterModel character, Vector2 position, bool flipX)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return SpawnVisuals(character, position, flipX);
		}
		catch (Exception ex)
		{
			GD.PrintErr($"Failed to spawn visuals for {((AbstractModel)character).Id}: {ex.Message}");
			return null;
		}
	}

	private NCreatureVisuals SpawnVisuals(CharacterModel character, Vector2 position, bool flipX)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		NCreatureVisuals val = character.CreateVisuals();
		((Node2D)val).Position = position;
		if (flipX)
		{
			((Node2D)val).Scale = new Vector2(-1f, 1f);
		}
		((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
		return val;
	}

	private static Action<string> BuildTrigger(CharacterModel character, NCreatureVisuals visuals)
	{
		IAnimatedVisuals animatedVisuals = visuals as IAnimatedVisuals;
		if (animatedVisuals == null)
		{
			if (visuals != null && visuals.HasSpineAnimation && visuals.SpineBody != null)
			{
				CreatureAnimator animator = null;
				try
				{
					CustomCharacterModel val = (CustomCharacterModel)(object)((character is CustomCharacterModel) ? character : null);
					if (val != null)
					{
						animator = val.SetupCustomAnimationStates(visuals.SpineBody);
					}
					if (animator == null)
					{
						animator = character.GenerateAnimator(visuals.SpineBody);
					}
				}
				catch (Exception ex)
				{
					GD.PrintErr($"Animator setup failed for {((AbstractModel)character).Id}: {ex.Message}");
					return delegate
					{
					};
				}
				return delegate(string trigger)
				{
					if (!GodotObject.IsInstanceValid((GodotObject)(object)visuals) || visuals.SpineBody == null)
					{
						return;
					}
					try
					{
						animator.SetTrigger(trigger);
					}
					catch (Exception ex2)
					{
						GD.PrintErr("Trigger " + trigger + " failed: " + ex2.Message);
					}
				};
			}
			return delegate(string trigger)
			{
				if (!GodotObject.IsInstanceValid((GodotObject)(object)visuals))
				{
					return;
				}
				string text = trigger switch
				{
					"Idle" => "idle", 
					"Attack" => "attack", 
					"Cast" => "cast", 
					"Hit" => "hurt", 
					"Dead" => "die", 
					_ => trigger.ToLowerInvariant(), 
				};
				try
				{
					CustomAnimation.PlayCustomAnimation((Node)(object)visuals, new string[2] { text, trigger });
				}
				catch (Exception ex2)
				{
					GD.PrintErr("Custom animation " + trigger + " failed: " + ex2.Message);
				}
			};
		}
		return delegate(string trigger)
		{
			if (!GodotObject.IsInstanceValid((GodotObject)(object)visuals))
			{
				return;
			}
			try
			{
				animatedVisuals.OnAnimationTrigger(trigger);
			}
			catch (Exception ex2)
			{
				GD.PrintErr("OnAnimationTrigger " + trigger + " failed: " + ex2.Message);
			}
		};
	}

	private static Rect2 GetBoundsRect(NCreatureVisuals visuals)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		Control bounds = visuals.Bounds;
		Vector2 size = bounds.Size;
		Vector2 scale = ((Node2D)visuals).Scale;
		Vector2 val = size * ((Vector2)(ref scale)).Abs();
		Vector2 globalPosition = bounds.GlobalPosition;
		if (((Node2D)visuals).Scale.X < 0f)
		{
			globalPosition.X -= val.X;
		}
		Vector2 val2 = default(Vector2);
		((Vector2)(ref val2))._002Ector(20f, 20f);
		return new Rect2(globalPosition - val2, val + val2 * 2f);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<MethodInfo> GetGodotMethodList()
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Expected O, but got Unknown
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Expected O, but got Unknown
		//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(8)
		{
			new MethodInfo(MethodName.AfterOverlayOpened, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AfterOverlayClosed, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AfterOverlayShown, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.AfterOverlayHidden, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Ready, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("event"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.GetBoundsRect, new PropertyInfo((Type)7, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)33, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("visuals"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("Node2D"), false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0151: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.AfterOverlayOpened && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AfterOverlayOpened();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayClosed && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AfterOverlayClosed();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayShown && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AfterOverlayShown();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayHidden && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			AfterOverlayHidden();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Ready && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			((Node)this)._Ready();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Input && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Input(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.GetBoundsRect && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Rect2 boundsRect = GetBoundsRect(VariantUtils.ConvertTo<NCreatureVisuals>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<Rect2>(ref boundsRect);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static bool InvokeGodotClassStaticMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName.GetBoundsRect && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			Rect2 boundsRect = GetBoundsRect(VariantUtils.ConvertTo<NCreatureVisuals>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = VariantUtils.CreateFrom<Rect2>(ref boundsRect);
			return true;
		}
		ret = default(godot_variant);
		return false;
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName.AfterOverlayOpened)
		{
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayClosed)
		{
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayShown)
		{
			return true;
		}
		if ((ref method) == MethodName.AfterOverlayHidden)
		{
			return true;
		}
		if ((ref method) == MethodName._Ready)
		{
			return true;
		}
		if ((ref method) == MethodName._Input)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.GetBoundsRect)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName._animating)
		{
			_animating = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._bounds1)
		{
			_bounds1 = VariantUtils.ConvertTo<Rect2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._bounds2)
		{
			_bounds2 = VariantUtils.ConvertTo<Rect2>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._controllerIndex)
		{
			_controllerIndex = VariantUtils.ConvertTo<int>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._visuals1)
		{
			_visuals1 = VariantUtils.ConvertTo<NCreatureVisuals>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._visuals2)
		{
			_visuals2 = VariantUtils.ConvertTo<NCreatureVisuals>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.DefaultFocusedControl)
		{
			Control defaultFocusedControl = DefaultFocusedControl;
			value = VariantUtils.CreateFrom<Control>(ref defaultFocusedControl);
			return true;
		}
		if ((ref name) == PropertyName.ScreenType)
		{
			NetScreenType screenType = ScreenType;
			value = VariantUtils.CreateFrom<NetScreenType>(ref screenType);
			return true;
		}
		if ((ref name) == PropertyName.UseSharedBackstop)
		{
			bool useSharedBackstop = UseSharedBackstop;
			value = VariantUtils.CreateFrom<bool>(ref useSharedBackstop);
			return true;
		}
		if ((ref name) == PropertyName._animating)
		{
			value = VariantUtils.CreateFrom<bool>(ref _animating);
			return true;
		}
		if ((ref name) == PropertyName._bounds1)
		{
			value = VariantUtils.CreateFrom<Rect2>(ref _bounds1);
			return true;
		}
		if ((ref name) == PropertyName._bounds2)
		{
			value = VariantUtils.CreateFrom<Rect2>(ref _bounds2);
			return true;
		}
		if ((ref name) == PropertyName._controllerIndex)
		{
			value = VariantUtils.CreateFrom<int>(ref _controllerIndex);
			return true;
		}
		if ((ref name) == PropertyName._visuals1)
		{
			value = VariantUtils.CreateFrom<NCreatureVisuals>(ref _visuals1);
			return true;
		}
		if ((ref name) == PropertyName._visuals2)
		{
			value = VariantUtils.CreateFrom<NCreatureVisuals>(ref _visuals2);
			return true;
		}
		return ((GodotObject)this).GetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	internal static List<PropertyInfo> GetGodotPropertyList()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)1, PropertyName._animating, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)7, PropertyName._bounds1, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)7, PropertyName._bounds2, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName._controllerIndex, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._visuals1, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName._visuals2, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.DefaultFocusedControl, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)2, PropertyName.ScreenType, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.UseSharedBackstop, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
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
		((GodotObject)this).SaveGodotObjectData(info);
		info.AddProperty(PropertyName._animating, Variant.From<bool>(ref _animating));
		info.AddProperty(PropertyName._bounds1, Variant.From<Rect2>(ref _bounds1));
		info.AddProperty(PropertyName._bounds2, Variant.From<Rect2>(ref _bounds2));
		info.AddProperty(PropertyName._controllerIndex, Variant.From<int>(ref _controllerIndex));
		info.AddProperty(PropertyName._visuals1, Variant.From<NCreatureVisuals>(ref _visuals1));
		info.AddProperty(PropertyName._visuals2, Variant.From<NCreatureVisuals>(ref _visuals2));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName._animating, ref val))
		{
			_animating = ((Variant)(ref val)).As<bool>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName._bounds1, ref val2))
		{
			_bounds1 = ((Variant)(ref val2)).As<Rect2>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName._bounds2, ref val3))
		{
			_bounds2 = ((Variant)(ref val3)).As<Rect2>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._controllerIndex, ref val4))
		{
			_controllerIndex = ((Variant)(ref val4)).As<int>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._visuals1, ref val5))
		{
			_visuals1 = ((Variant)(ref val5)).As<NCreatureVisuals>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._visuals2, ref val6))
		{
			_visuals2 = ((Variant)(ref val6)).As<NCreatureVisuals>();
		}
	}
}
