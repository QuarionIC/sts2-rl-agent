using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;
using Godot.Bridge;
using Godot.NativeInterop;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Helpers;

namespace BaseLib.BaseLibScenes;

[ScriptPath("res://BaseLibScenes/NHorizontalScrollContainer.cs")]
public class NHorizontalScrollContainer : Control
{
	public class MethodName : MethodName
	{
		public static readonly StringName _GuiInput = StringName.op_Implicit("_GuiInput");

		public static readonly StringName _Input = StringName.op_Implicit("_Input");

		public static readonly StringName InitFocusScrolling = StringName.op_Implicit("InitFocusScrolling");

		public static readonly StringName ProcessMouseEvent = StringName.op_Implicit("ProcessMouseEvent");

		public static readonly StringName ProcessScrollEvent = StringName.op_Implicit("ProcessScrollEvent");

		public static readonly StringName _Process = StringName.op_Implicit("_Process");

		public static readonly StringName UpdateScrollPosition = StringName.op_Implicit("UpdateScrollPosition");
	}

	public class PropertyName : PropertyName
	{
		public static readonly StringName IsDragging = StringName.op_Implicit("IsDragging");

		public static readonly StringName ScrollContents = StringName.op_Implicit("ScrollContents");

		public static readonly StringName ContentSize = StringName.op_Implicit("ContentSize");

		public static readonly StringName ScrollLimit = StringName.op_Implicit("ScrollLimit");

		public static readonly StringName ScrollPosition = StringName.op_Implicit("ScrollPosition");

		public static readonly StringName TargetPosition = StringName.op_Implicit("TargetPosition");

		public static readonly StringName _controllerScrollAmount = StringName.op_Implicit("_controllerScrollAmount");

		public static readonly StringName _startDragPosX = StringName.op_Implicit("_startDragPosX");

		public static readonly StringName _targetDragPosX = StringName.op_Implicit("_targetDragPosX");
	}

	public class SignalName : SignalName
	{
	}

	private float _controllerScrollAmount = 400f;

	private float _startDragPosX;

	private float _targetDragPosX;

	public bool IsDragging { get; protected set; }

	public Control? ScrollContents { get; set; }

	public float ContentSize
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			if (ScrollContents == null)
			{
				return 0f;
			}
			return ScrollContents.Size.X;
		}
	}

	public float ScrollLimit => Math.Min(0f, ((Control)this).Size.X - ContentSize);

	public float ScrollPosition
	{
		get
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			if (ScrollContents == null)
			{
				return 0f;
			}
			return ScrollContents.Position.X;
		}
	}

	public float TargetPosition
	{
		get
		{
			return _targetDragPosX;
		}
		set
		{
			_targetDragPosX = Math.Clamp(value, ScrollLimit, 0f);
		}
	}

	public Action<NHorizontalScrollContainer>? CustomProcess { get; set; }

	public static NHorizontalScrollContainer Create(string name, Control contents, Action<Control> setupPositionAndSize)
	{
		NHorizontalScrollContainer nHorizontalScrollContainer = new NHorizontalScrollContainer();
		((Node)nHorizontalScrollContainer).Name = StringName.op_Implicit(name);
		((Control)nHorizontalScrollContainer).MouseFilter = (MouseFilterEnum)1;
		setupPositionAndSize((Control)(object)nHorizontalScrollContainer);
		nHorizontalScrollContainer.ScrollContents = contents;
		return nHorizontalScrollContainer;
	}

	public override void _GuiInput(InputEvent inputEvent)
	{
		if (((CanvasItem)this).IsVisibleInTree())
		{
			ProcessMouseEvent(inputEvent);
			ProcessScrollEvent(inputEvent);
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (((CanvasItem)this).IsVisibleInTree())
		{
			Viewport viewport = ((Node)this).GetViewport();
			Control val = ((viewport != null) ? viewport.GuiGetFocusOwner() : null);
			if (val != null && ((Node)this).IsAncestorOf((Node)(object)val) && (inputEvent.IsActionPressed(MegaInput.left, false, false) || inputEvent.IsActionPressed(MegaInput.right, false, false)))
			{
				((Node)this).GetViewport().SetInputAsHandled();
			}
		}
	}

	public void InitFocusScrolling()
	{
		Control? scrollContents = ScrollContents;
		foreach (Control item in ((scrollContents != null) ? ((IEnumerable)((Node)scrollContents).GetChildren(false)).OfType<Control>() : null) ?? Enumerable.Empty<Control>())
		{
			Control c = item;
			c.FocusEntered += delegate
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_0018: Unknown result type (might be due to invalid IL or missing references)
				//IL_002a: Unknown result type (might be due to invalid IL or missing references)
				float x = c.Position.X;
				float num = x + c.Size.X;
				float x2 = ((Control)this).Size.X;
				float scrollPosition = ScrollPosition;
				if (x + scrollPosition < 0f)
				{
					TargetPosition = 0f - x;
				}
				else if (num + scrollPosition > x2)
				{
					TargetPosition = x2 - num;
				}
			};
		}
	}

	public void ProcessMouseEvent(InputEvent inputEvent)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		if (ScrollContents == null)
		{
			return;
		}
		InputEventMouseMotion val = (InputEventMouseMotion)(object)((inputEvent is InputEventMouseMotion) ? inputEvent : null);
		if (val == null)
		{
			InputEventMouseButton val2 = (InputEventMouseButton)(object)((inputEvent is InputEventMouseButton) ? inputEvent : null);
			if (val2 != null)
			{
				IsDragging = val2.Pressed;
				if (val2.Pressed)
				{
					_startDragPosX = ScrollPosition;
					_targetDragPosX = _startDragPosX;
				}
			}
		}
		else if (IsDragging)
		{
			_targetDragPosX += val.Relative.X;
		}
	}

	public void ProcessScrollEvent(InputEvent inputEvent)
	{
		_targetDragPosX += ScrollHelper.GetDragForScrollEvent(inputEvent);
	}

	public override void _Process(double delta)
	{
		if (((CanvasItem)this).IsVisibleInTree())
		{
			CustomProcess?.Invoke(this);
			UpdateScrollPosition(delta);
		}
	}

	protected void UpdateScrollPosition(double delta)
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		if (ScrollContents == null)
		{
			return;
		}
		float targetDragPosX = _targetDragPosX;
		if (!Mathf.IsEqualApprox(ScrollPosition, targetDragPosX))
		{
			float x = Mathf.Lerp(ScrollPosition, targetDragPosX, (float)delta * 15f);
			Control? scrollContents = ScrollContents;
			Vector2 position = ScrollContents.Position;
			position.X = x;
			scrollContents.Position = position;
			if ((double)Mathf.Abs(ScrollContents.Position.X - targetDragPosX) < 0.5)
			{
				Control? scrollContents2 = ScrollContents;
				position = ScrollContents.Position;
				position.X = targetDragPosX;
				scrollContents2.Position = position;
			}
		}
		if (!IsDragging)
		{
			if (_targetDragPosX > 0f)
			{
				_targetDragPosX = Mathf.Lerp(_targetDragPosX, 0f, (float)delta * 12f);
			}
			else if (_targetDragPosX < ScrollLimit)
			{
				_targetDragPosX = Mathf.Lerp(_targetDragPosX, ScrollLimit, (float)delta * 12f);
			}
		}
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
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Expected O, but got Unknown
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Expected O, but got Unknown
		//IL_013d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a1: Expected O, but got Unknown
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0221: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Unknown result type (might be due to invalid IL or missing references)
		//IL_024f: Unknown result type (might be due to invalid IL or missing references)
		return new List<MethodInfo>(7)
		{
			new MethodInfo(MethodName._GuiInput, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("inputEvent"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Input, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("inputEvent"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.InitFocusScrolling, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, (List<PropertyInfo>)null, (List<Variant>)null),
			new MethodInfo(MethodName.ProcessMouseEvent, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("inputEvent"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.ProcessScrollEvent, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)24, StringName.op_Implicit("inputEvent"), (PropertyHint)0, "", (PropertyUsageFlags)6, new StringName("InputEvent"), false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName._Process, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null),
			new MethodInfo(MethodName.UpdateScrollPosition, new PropertyInfo((Type)0, StringName.op_Implicit(""), (PropertyHint)0, "", (PropertyUsageFlags)6, false), (MethodFlags)1, new List<PropertyInfo>
			{
				new PropertyInfo((Type)3, StringName.op_Implicit("delta"), (PropertyHint)0, "", (PropertyUsageFlags)6, false)
			}, (List<Variant>)null)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool InvokeGodotClassMethod(in godot_string_name method, NativeVariantPtrArgs args, out godot_variant ret)
	{
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		if ((ref method) == MethodName._GuiInput && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Control)this)._GuiInput(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Input && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Input(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.InitFocusScrolling && ((NativeVariantPtrArgs)(ref args)).Count == 0)
		{
			InitFocusScrolling();
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ProcessMouseEvent && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ProcessMouseEvent(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.ProcessScrollEvent && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			ProcessScrollEvent(VariantUtils.ConvertTo<InputEvent>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName._Process && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			((Node)this)._Process(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		if ((ref method) == MethodName.UpdateScrollPosition && ((NativeVariantPtrArgs)(ref args)).Count == 1)
		{
			UpdateScrollPosition(VariantUtils.ConvertTo<double>(ref ((NativeVariantPtrArgs)(ref args))[0]));
			ret = default(godot_variant);
			return true;
		}
		return ((Control)this).InvokeGodotClassMethod(ref method, args, ref ret);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool HasGodotClassMethod(in godot_string_name method)
	{
		if ((ref method) == MethodName._GuiInput)
		{
			return true;
		}
		if ((ref method) == MethodName._Input)
		{
			return true;
		}
		if ((ref method) == MethodName.InitFocusScrolling)
		{
			return true;
		}
		if ((ref method) == MethodName.ProcessMouseEvent)
		{
			return true;
		}
		if ((ref method) == MethodName.ProcessScrollEvent)
		{
			return true;
		}
		if ((ref method) == MethodName._Process)
		{
			return true;
		}
		if ((ref method) == MethodName.UpdateScrollPosition)
		{
			return true;
		}
		return ((Control)this).HasGodotClassMethod(ref method);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool SetGodotClassPropertyValue(in godot_string_name name, in godot_variant value)
	{
		if ((ref name) == PropertyName.IsDragging)
		{
			IsDragging = VariantUtils.ConvertTo<bool>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.ScrollContents)
		{
			ScrollContents = VariantUtils.ConvertTo<Control>(ref value);
			return true;
		}
		if ((ref name) == PropertyName.TargetPosition)
		{
			TargetPosition = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._controllerScrollAmount)
		{
			_controllerScrollAmount = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._startDragPosX)
		{
			_startDragPosX = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		if ((ref name) == PropertyName._targetDragPosX)
		{
			_targetDragPosX = VariantUtils.ConvertTo<float>(ref value);
			return true;
		}
		return ((GodotObject)this).SetGodotClassPropertyValue(ref name, ref value);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override bool GetGodotClassPropertyValue(in godot_string_name name, out godot_variant value)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0126: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		if ((ref name) == PropertyName.IsDragging)
		{
			bool isDragging = IsDragging;
			value = VariantUtils.CreateFrom<bool>(ref isDragging);
			return true;
		}
		if ((ref name) == PropertyName.ScrollContents)
		{
			Control scrollContents = ScrollContents;
			value = VariantUtils.CreateFrom<Control>(ref scrollContents);
			return true;
		}
		if ((ref name) == PropertyName.ContentSize)
		{
			float contentSize = ContentSize;
			value = VariantUtils.CreateFrom<float>(ref contentSize);
			return true;
		}
		if ((ref name) == PropertyName.ScrollLimit)
		{
			float contentSize = ScrollLimit;
			value = VariantUtils.CreateFrom<float>(ref contentSize);
			return true;
		}
		if ((ref name) == PropertyName.ScrollPosition)
		{
			float contentSize = ScrollPosition;
			value = VariantUtils.CreateFrom<float>(ref contentSize);
			return true;
		}
		if ((ref name) == PropertyName.TargetPosition)
		{
			float contentSize = TargetPosition;
			value = VariantUtils.CreateFrom<float>(ref contentSize);
			return true;
		}
		if ((ref name) == PropertyName._controllerScrollAmount)
		{
			value = VariantUtils.CreateFrom<float>(ref _controllerScrollAmount);
			return true;
		}
		if ((ref name) == PropertyName._startDragPosX)
		{
			value = VariantUtils.CreateFrom<float>(ref _startDragPosX);
			return true;
		}
		if ((ref name) == PropertyName._targetDragPosX)
		{
			value = VariantUtils.CreateFrom<float>(ref _targetDragPosX);
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
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_011c: Unknown result type (might be due to invalid IL or missing references)
		return new List<PropertyInfo>
		{
			new PropertyInfo((Type)3, PropertyName._controllerScrollAmount, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._startDragPosX, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName._targetDragPosX, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)1, PropertyName.IsDragging, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)24, PropertyName.ScrollContents, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.ContentSize, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.ScrollLimit, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.ScrollPosition, (PropertyHint)0, "", (PropertyUsageFlags)4096, false),
			new PropertyInfo((Type)3, PropertyName.TargetPosition, (PropertyHint)0, "", (PropertyUsageFlags)4096, false)
		};
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void SaveGodotObjectData(GodotSerializationInfo info)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		((GodotObject)this).SaveGodotObjectData(info);
		StringName isDragging = PropertyName.IsDragging;
		bool isDragging2 = IsDragging;
		info.AddProperty(isDragging, Variant.From<bool>(ref isDragging2));
		StringName scrollContents = PropertyName.ScrollContents;
		Control scrollContents2 = ScrollContents;
		info.AddProperty(scrollContents, Variant.From<Control>(ref scrollContents2));
		StringName targetPosition = PropertyName.TargetPosition;
		float targetPosition2 = TargetPosition;
		info.AddProperty(targetPosition, Variant.From<float>(ref targetPosition2));
		info.AddProperty(PropertyName._controllerScrollAmount, Variant.From<float>(ref _controllerScrollAmount));
		info.AddProperty(PropertyName._startDragPosX, Variant.From<float>(ref _startDragPosX));
		info.AddProperty(PropertyName._targetDragPosX, Variant.From<float>(ref _targetDragPosX));
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	protected override void RestoreGodotObjectData(GodotSerializationInfo info)
	{
		((GodotObject)this).RestoreGodotObjectData(info);
		Variant val = default(Variant);
		if (info.TryGetProperty(PropertyName.IsDragging, ref val))
		{
			IsDragging = ((Variant)(ref val)).As<bool>();
		}
		Variant val2 = default(Variant);
		if (info.TryGetProperty(PropertyName.ScrollContents, ref val2))
		{
			ScrollContents = ((Variant)(ref val2)).As<Control>();
		}
		Variant val3 = default(Variant);
		if (info.TryGetProperty(PropertyName.TargetPosition, ref val3))
		{
			TargetPosition = ((Variant)(ref val3)).As<float>();
		}
		Variant val4 = default(Variant);
		if (info.TryGetProperty(PropertyName._controllerScrollAmount, ref val4))
		{
			_controllerScrollAmount = ((Variant)(ref val4)).As<float>();
		}
		Variant val5 = default(Variant);
		if (info.TryGetProperty(PropertyName._startDragPosX, ref val5))
		{
			_startDragPosX = ((Variant)(ref val5)).As<float>();
		}
		Variant val6 = default(Variant);
		if (info.TryGetProperty(PropertyName._targetDragPosX, ref val6))
		{
			_targetDragPosX = ((Variant)(ref val6)).As<float>();
		}
	}
}
