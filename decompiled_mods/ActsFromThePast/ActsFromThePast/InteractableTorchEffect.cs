using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public class InteractableTorchEffect : Control
{
	public enum TorchSize
	{
		S,
		M,
		L
	}

	private const string LOG_TAG = "[ActsFromThePast]";

	private const string AtlasPath = "res://ActsFromThePast/vfx/vfx.atlas";

	private float _x;

	private float _y;

	private bool _activated = true;

	private float _particleTimer = 0f;

	private const float ParticleEmitInterval = 0.1f;

	private TorchSize _size;

	private float _scale;

	private Sprite2D _sprite;

	private Color _color;

	private bool _initialized = false;

	private bool _mouseWasPressed = false;

	private List<TorchParticleSEffect> _particlesS = new List<TorchParticleSEffect>();

	private List<LightFlareSEffect> _flaresS = new List<LightFlareSEffect>();

	private List<TorchParticleMEffect> _particlesM = new List<TorchParticleMEffect>();

	private List<LightFlareMEffect> _flaresM = new List<LightFlareMEffect>();

	private List<TorchParticleLEffect> _particlesL = new List<TorchParticleLEffect>();

	private List<LightFlareLEffect> _flaresL = new List<LightFlareLEffect>();

	public static bool RenderGreen;

	public static InteractableTorchEffect Create(float x, float y, TorchSize size = TorchSize.M)
	{
		InteractableTorchEffect interactableTorchEffect = new InteractableTorchEffect();
		interactableTorchEffect._x = x;
		interactableTorchEffect._y = y;
		interactableTorchEffect._size = size;
		return interactableTorchEffect;
	}

	public void Initialize()
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Expected O, but got Unknown
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ba: Unknown result type (might be due to invalid IL or missing references)
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/vfx/vfx.atlas", "env/torch");
		if (region.HasValue)
		{
			_color = new Color(1f, 1f, 1f, 0.4f);
			switch (_size)
			{
			case TorchSize.S:
				_scale = 0.6f;
				break;
			case TorchSize.M:
				_scale = 1f;
				break;
			case TorchSize.L:
				_scale = 1.4f;
				break;
			}
			float num = 960f;
			float num2 = 568f;
			float num3 = _x - num - 23f;
			float num4 = num2 - _y;
			Vector2 val = default(Vector2);
			((Vector2)(ref val))._002Ector(50f, 60f);
			((Control)this).Size = val;
			((Control)this).Position = new Vector2(num3 - val.X / 2f, num4 - val.Y / 2f);
			_sprite = new Sprite2D();
			_sprite.Texture = region.Value.Texture;
			_sprite.RegionEnabled = true;
			_sprite.RegionRect = region.Value.Region;
			_sprite.Centered = true;
			((Node2D)_sprite).Position = new Vector2(val.X / 2f, val.Y / 2f + 24f);
			((Node2D)_sprite).Scale = new Vector2(_scale, _scale);
			((CanvasItem)_sprite).Modulate = _color;
			((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
			((Node)this).GetTree().ProcessFrame += OnProcessFrame;
		}
	}

	public override void _ExitTree()
	{
		if (_initialized)
		{
			((Node)this).GetTree().ProcessFrame -= OnProcessFrame;
		}
		((Node)this)._ExitTree();
	}

	private void OnProcessFrame()
	{
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		if (!_initialized || !((Node)this).IsInsideTree())
		{
			return;
		}
		float num = (float)((Node)this).GetProcessDeltaTime();
		UpdateParticles();
		bool flag = Input.IsMouseButtonPressed((MouseButton)1);
		if (flag && !_mouseWasPressed)
		{
			Vector2 mousePosition = ((Node)this).GetViewport().GetMousePosition();
			Rect2 globalRect = ((Control)this).GetGlobalRect();
			if (((Rect2)(ref globalRect)).HasPoint(mousePosition) && IsCombatInFocus())
			{
				_activated = !_activated;
				if (_activated)
				{
					AFTPModAudio.Play("general", "fire_ignite", -10f, 0.4f);
				}
				else
				{
					AFTPModAudio.Play("general", "torch_extinguish", -10f);
				}
			}
		}
		_mouseWasPressed = flag;
		if (_activated)
		{
			_particleTimer -= num;
			if (_particleTimer < 0f)
			{
				_particleTimer = 0.1f;
				SpawnParticles();
			}
		}
	}

	private bool IsCombatInFocus()
	{
		Control val = ((Node)this).GetViewport().GuiGetHoveredControl();
		if (val == null)
		{
			return true;
		}
		for (Node val2 = (Node)(object)val; val2 != null; val2 = val2.GetParent())
		{
			if (val2 is NCombatRoom)
			{
				return true;
			}
		}
		return false;
	}

	private void UpdateParticles()
	{
		for (int num = _particlesS.Count - 1; num >= 0; num--)
		{
			TorchParticleSEffect torchParticleSEffect = _particlesS[num];
			if (torchParticleSEffect.IsDone || !GodotObject.IsInstanceValid((GodotObject)(object)torchParticleSEffect))
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)torchParticleSEffect))
				{
					((Node)torchParticleSEffect).QueueFree();
				}
				_particlesS.RemoveAt(num);
			}
		}
		for (int num2 = _flaresS.Count - 1; num2 >= 0; num2--)
		{
			LightFlareSEffect lightFlareSEffect = _flaresS[num2];
			if (lightFlareSEffect.IsDone || !GodotObject.IsInstanceValid((GodotObject)(object)lightFlareSEffect))
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)lightFlareSEffect))
				{
					((Node)lightFlareSEffect).QueueFree();
				}
				_flaresS.RemoveAt(num2);
			}
		}
		for (int num3 = _particlesM.Count - 1; num3 >= 0; num3--)
		{
			TorchParticleMEffect torchParticleMEffect = _particlesM[num3];
			if (torchParticleMEffect.IsDone || !GodotObject.IsInstanceValid((GodotObject)(object)torchParticleMEffect))
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)torchParticleMEffect))
				{
					((Node)torchParticleMEffect).QueueFree();
				}
				_particlesM.RemoveAt(num3);
			}
		}
		for (int num4 = _flaresM.Count - 1; num4 >= 0; num4--)
		{
			LightFlareMEffect lightFlareMEffect = _flaresM[num4];
			if (lightFlareMEffect.IsDone || !GodotObject.IsInstanceValid((GodotObject)(object)lightFlareMEffect))
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)lightFlareMEffect))
				{
					((Node)lightFlareMEffect).QueueFree();
				}
				_flaresM.RemoveAt(num4);
			}
		}
		for (int num5 = _particlesL.Count - 1; num5 >= 0; num5--)
		{
			TorchParticleLEffect torchParticleLEffect = _particlesL[num5];
			if (torchParticleLEffect.IsDone || !GodotObject.IsInstanceValid((GodotObject)(object)torchParticleLEffect))
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)torchParticleLEffect))
				{
					((Node)torchParticleLEffect).QueueFree();
				}
				_particlesL.RemoveAt(num5);
			}
		}
		for (int num6 = _flaresL.Count - 1; num6 >= 0; num6--)
		{
			LightFlareLEffect lightFlareLEffect = _flaresL[num6];
			if (lightFlareLEffect.IsDone || !GodotObject.IsInstanceValid((GodotObject)(object)lightFlareLEffect))
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)lightFlareLEffect))
				{
					((Node)lightFlareLEffect).QueueFree();
				}
				_flaresL.RemoveAt(num6);
			}
		}
	}

	private void SpawnParticles()
	{
		float x = _x;
		float y = _y;
		switch (_size)
		{
		case TorchSize.S:
		{
			y -= 10f;
			TorchParticleSEffect torchParticleSEffect = TorchParticleSEffect.Create(x, y, RenderGreen);
			((CanvasItem)torchParticleSEffect).ZIndex = -1;
			((Node)this).GetParent().AddChild((Node)(object)torchParticleSEffect, false, (InternalMode)0);
			_particlesS.Add(torchParticleSEffect);
			LightFlareSEffect lightFlareSEffect = LightFlareSEffect.Create(x, y, RenderGreen);
			((CanvasItem)lightFlareSEffect).ZIndex = -1;
			((Node)this).GetParent().AddChild((Node)(object)lightFlareSEffect, false, (InternalMode)0);
			_flaresS.Add(lightFlareSEffect);
			break;
		}
		case TorchSize.M:
		{
			TorchParticleMEffect torchParticleMEffect = TorchParticleMEffect.Create(x, y, RenderGreen);
			((CanvasItem)torchParticleMEffect).ZIndex = -1;
			((Node)this).GetParent().AddChild((Node)(object)torchParticleMEffect, false, (InternalMode)0);
			_particlesM.Add(torchParticleMEffect);
			LightFlareMEffect lightFlareMEffect = LightFlareMEffect.Create(x, y, RenderGreen);
			((CanvasItem)lightFlareMEffect).ZIndex = -1;
			((Node)this).GetParent().AddChild((Node)(object)lightFlareMEffect, false, (InternalMode)0);
			_flaresM.Add(lightFlareMEffect);
			break;
		}
		case TorchSize.L:
		{
			y += 14f;
			TorchParticleLEffect torchParticleLEffect = TorchParticleLEffect.Create(x, y, RenderGreen);
			((CanvasItem)torchParticleLEffect).ZIndex = -1;
			((Node)this).GetParent().AddChild((Node)(object)torchParticleLEffect, false, (InternalMode)0);
			_particlesL.Add(torchParticleLEffect);
			LightFlareLEffect lightFlareLEffect = LightFlareLEffect.Create(x, y, RenderGreen);
			((CanvasItem)lightFlareLEffect).ZIndex = -1;
			((Node)this).GetParent().AddChild((Node)(object)lightFlareLEffect, false, (InternalMode)0);
			_flaresL.Add(lightFlareLEffect);
			break;
		}
		}
	}
}
