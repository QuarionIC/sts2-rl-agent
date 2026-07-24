using System;
using System.Collections.Generic;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Acts.Exordium;

public class ExordiumBackground : NCombatBackground
{
	private const string LOG_TAG = "[ActsFromThePast]";

	private const string AtlasPath = "res://ActsFromThePast/backgrounds/exordium/scene.atlas";

	private TextureRect _bg;

	private TextureRect _mg;

	private TextureRect _fg;

	private TextureRect _ceiling;

	private TextureRect _leftWall;

	private TextureRect _hollowWall;

	private TextureRect _solidWall;

	private TextureRect _ceilingMod1;

	private TextureRect _ceilingMod2;

	private TextureRect _ceilingMod3;

	private TextureRect _ceilingMod4;

	private TextureRect _ceilingMod5;

	private TextureRect _ceilingMod6;

	private bool _renderLeftWall;

	private bool _renderHollowMid;

	private bool _renderSolidMid;

	private List<DustEffect> _dust = new List<DustEffect>();

	private List<BottomFogEffect> _fog = new List<BottomFogEffect>();

	private const int MaxDust = 96;

	private const int MaxFog = 50;

	private List<InteractableTorchEffect> _torches = new List<InteractableTorchEffect>();

	private ColorRect _overlayRect;

	private Color _overlayColor;

	private bool _initialized = false;

	public override void _Ready()
	{
		((Node)this)._Ready();
		Initialize();
	}

	public void Initialize()
	{
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Expected O, but got Unknown
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Expected O, but got Unknown
		if (_initialized)
		{
			return;
		}
		_initialized = true;
		try
		{
			((Control)this).SetAnchorsPreset((LayoutPreset)15, false);
			((Control)this).MouseFilter = (MouseFilterEnum)2;
			((CanvasItem)this).ZIndex = -100;
			_bg = CreateTextureRect("bg", -50);
			_mg = CreateTextureRect("mod/mg", -40);
			_solidWall = CreateTextureRect("mod/midWall", -30);
			_hollowWall = CreateTextureRect("mod/mod2", -29);
			_leftWall = CreateTextureRect("mod/mod1", -28);
			_ceiling = CreateTextureRect("mod/ceiling", -20);
			_ceilingMod1 = CreateTextureRect("mod/ceilingMod1", -19);
			_ceilingMod2 = CreateTextureRect("mod/ceilingMod2", -18);
			_ceilingMod3 = CreateTextureRect("mod/ceilingMod3", -17);
			_ceilingMod4 = CreateTextureRect("mod/ceilingMod4", -16);
			_ceilingMod5 = CreateTextureRect("mod/ceilingMod5", -15);
			_ceilingMod6 = CreateTextureRect("mod/ceilingMod6", -14);
			_fg = CreateTextureRect("mod/fg", -10);
			_overlayRect = new ColorRect();
			((Control)_overlayRect).MouseFilter = (MouseFilterEnum)2;
			((CanvasItem)_overlayRect).ZIndex = -5;
			((Control)_overlayRect).Position = new Vector2(-983f, -568f);
			((Control)_overlayRect).Size = new Vector2(1920f, 1136f);
			CanvasItemMaterial val = new CanvasItemMaterial();
			val.BlendMode = (BlendModeEnum)1;
			((CanvasItem)_overlayRect).Material = (Material)(object)val;
			((Node)this).AddChild((Node)(object)_overlayRect, false, (InternalMode)0);
			RandomizeScene();
		}
		catch (Exception)
		{
		}
	}

	public void _DeferredInit()
	{
		if (((Node)this).IsInsideTree())
		{
			((Node)this).GetTree().ProcessFrame += OnProcessFrame;
		}
		Initialize();
	}

	public override void _EnterTree()
	{
		((Node)this)._EnterTree();
		((Node)this).GetTree().ProcessFrame += OnProcessFrame;
	}

	private void OnProcessFrame()
	{
		if (_initialized && ((Node)this).IsInsideTree())
		{
			UpdateDust();
			UpdateFog();
		}
	}

	public override void _ExitTree()
	{
		((Node)this).GetTree().ProcessFrame -= OnProcessFrame;
		((Node)this)._ExitTree();
	}

	private TextureRect CreateTextureRect(string regionName, int zIndex)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_006f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		TextureRect val = new TextureRect();
		((Control)val).MouseFilter = (MouseFilterEnum)2;
		((CanvasItem)val).ZIndex = zIndex;
		LibGdxAtlas.RegionInfo? regionData = LibGdxAtlas.GetRegionData("res://ActsFromThePast/backgrounds/exordium/scene.atlas", regionName);
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/backgrounds/exordium/scene.atlas", regionName);
		if (region.HasValue && regionData.HasValue)
		{
			AtlasTexture val2 = new AtlasTexture();
			val2.Atlas = region.Value.Texture;
			val2.Region = region.Value.Region;
			val.Texture = (Texture2D)(object)val2;
			val.StretchMode = (StretchModeEnum)2;
			float num = (float)regionData.Value.OffsetX - (float)regionData.Value.OrigWidth / 2f - 23f;
			float num2 = (float)(regionData.Value.OrigHeight - regionData.Value.OffsetY - regionData.Value.Height) - (float)regionData.Value.OrigHeight / 2f;
			((Control)val).Position = new Vector2(num, num2);
			((Control)val).Size = new Vector2((float)regionData.Value.Width, (float)regionData.Value.Height);
		}
		((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
		return val;
	}

	private void RandomizeTorches()
	{
		foreach (InteractableTorchEffect torch in _torches)
		{
			if (GodotObject.IsInstanceValid((GodotObject)(object)torch))
			{
				((Node)torch).QueueFree();
			}
		}
		_torches.Clear();
		if (GD.Randf() < 0.1f)
		{
			_torches.Add(InteractableTorchEffect.Create(1790f, 850f, InteractableTorchEffect.TorchSize.S));
		}
		if (_renderHollowMid && !_renderSolidMid)
		{
			switch ((int)(GD.Randi() % 3))
			{
			case 0:
				_torches.Add(InteractableTorchEffect.Create(800f, 768f));
				_torches.Add(InteractableTorchEffect.Create(1206f, 768f));
				break;
			case 1:
				_torches.Add(InteractableTorchEffect.Create(328f, 865f, InteractableTorchEffect.TorchSize.S));
				break;
			}
		}
		else if (!_renderLeftWall && !_renderHollowMid)
		{
			if (GD.Randf() < 0.75f)
			{
				_torches.Add(InteractableTorchEffect.Create(613f, 860f));
				_torches.Add(InteractableTorchEffect.Create(613f, 672f));
				if (GD.Randf() < 0.3f)
				{
					_torches.Add(InteractableTorchEffect.Create(1482f, 860f));
					_torches.Add(InteractableTorchEffect.Create(1482f, 672f));
				}
			}
		}
		else if (_renderSolidMid && _renderHollowMid)
		{
			if (!_renderLeftWall)
			{
				int num = (int)(GD.Randi() % 4);
				if (num == 0)
				{
					_torches.Add(InteractableTorchEffect.Create(912f, 790f));
					_torches.Add(InteractableTorchEffect.Create(912f, 526f));
					_torches.Add(InteractableTorchEffect.Create(844f, 658f, InteractableTorchEffect.TorchSize.S));
					_torches.Add(InteractableTorchEffect.Create(980f, 658f, InteractableTorchEffect.TorchSize.S));
				}
				else if (num == 1 || num == 2)
				{
					_torches.Add(InteractableTorchEffect.Create(1828f, 720f));
				}
			}
			else if (GD.Randf() < 0.75f)
			{
				_torches.Add(InteractableTorchEffect.Create(970f, 874f, InteractableTorchEffect.TorchSize.L));
			}
		}
		else if (_renderLeftWall && !_renderHollowMid && GD.Randf() < 0.75f)
		{
			_torches.Add(InteractableTorchEffect.Create(970f, 873f, InteractableTorchEffect.TorchSize.L));
			_torches.Add(InteractableTorchEffect.Create(616f, 813f));
			_torches.Add(InteractableTorchEffect.Create(1266f, 708f));
		}
		InteractableTorchEffect.RenderGreen = GD.Randf() > 0.5f;
		foreach (InteractableTorchEffect torch2 in _torches)
		{
			((CanvasItem)torch2).ZIndex = -15;
			((Node)this).AddChild((Node)(object)torch2, false, (InternalMode)0);
			torch2.Initialize();
		}
	}

	public void RandomizeScene()
	{
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0181: Unknown result type (might be due to invalid IL or missing references)
		if (GD.Randf() > 0.5f)
		{
			_renderSolidMid = false;
			_renderLeftWall = false;
			_renderHollowMid = true;
			if (GD.Randf() > 0.5f)
			{
				_renderSolidMid = true;
				if (GD.Randf() > 0.5f)
				{
					_renderLeftWall = true;
				}
			}
		}
		else
		{
			_renderLeftWall = false;
			_renderHollowMid = false;
			_renderSolidMid = true;
			if (GD.Randf() > 0.5f)
			{
				_renderLeftWall = true;
			}
		}
		((CanvasItem)_ceilingMod1).Visible = GD.Randf() > 0.5f;
		((CanvasItem)_ceilingMod2).Visible = GD.Randf() > 0.5f;
		((CanvasItem)_ceilingMod3).Visible = GD.Randf() > 0.5f;
		((CanvasItem)_ceilingMod4).Visible = GD.Randf() > 0.5f;
		((CanvasItem)_ceilingMod5).Visible = GD.Randf() > 0.5f;
		((CanvasItem)_ceilingMod6).Visible = GD.Randf() > 0.5f;
		_overlayColor = new Color((float)GD.RandRange(0.0, 0.05) * 0.2f, (float)GD.RandRange(0.0, 0.2) * 0.2f, (float)GD.RandRange(0.0, 0.2) * 0.2f, 1f);
		RandomizeTorches();
		UpdateVisibility();
	}

	private void UpdateVisibility()
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		((CanvasItem)_solidWall).Visible = _renderSolidMid;
		((CanvasItem)_hollowWall).Visible = _renderHollowMid;
		((CanvasItem)_leftWall).Visible = _renderLeftWall;
		if (_renderHollowMid && (_renderSolidMid || _renderLeftWall))
		{
			((CanvasItem)_solidWall).Modulate = Colors.Gray;
		}
		else
		{
			((CanvasItem)_solidWall).Modulate = Colors.White;
		}
		_overlayRect.Color = _overlayColor;
	}

	private void UpdateDust()
	{
		for (int num = _dust.Count - 1; num >= 0; num--)
		{
			DustEffect dustEffect = _dust[num];
			if (dustEffect.IsDone || !GodotObject.IsInstanceValid((GodotObject)(object)dustEffect))
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)dustEffect))
				{
					((Node)dustEffect).QueueFree();
				}
				_dust.RemoveAt(num);
			}
		}
		while (_dust.Count < 96)
		{
			DustEffect dustEffect2 = DustEffect.Create();
			((CanvasItem)dustEffect2).ZIndex = -11;
			((Node)this).AddChild((Node)(object)dustEffect2, false, (InternalMode)0);
			_dust.Add(dustEffect2);
		}
	}

	private void UpdateFog()
	{
		for (int num = _fog.Count - 1; num >= 0; num--)
		{
			BottomFogEffect bottomFogEffect = _fog[num];
			if (bottomFogEffect.IsDone || !GodotObject.IsInstanceValid((GodotObject)(object)bottomFogEffect))
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)bottomFogEffect))
				{
					((Node)bottomFogEffect).QueueFree();
				}
				_fog.RemoveAt(num);
			}
		}
		while (_fog.Count < 50)
		{
			BottomFogEffect bottomFogEffect2 = BottomFogEffect.Create();
			((CanvasItem)bottomFogEffect2).ZIndex = -45;
			((Node)this).AddChild((Node)(object)bottomFogEffect2, false, (InternalMode)0);
			_fog.Add(bottomFogEffect2);
		}
	}

	public void OnTreeEntered()
	{
		//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		((Node)this).TreeEntered -= OnTreeEntered;
		((Node)this).GetTree().ProcessFrame += OnProcessFrame;
		Initialize();
		Node parent = ((Node)this).GetParent();
		while (parent != null && !(parent is NCombatRoom))
		{
			parent = parent.GetParent();
		}
		NCombatRoom val = (NCombatRoom)(object)((parent is NCombatRoom) ? parent : null);
		if (val != null)
		{
			Control nodeOrNull = ((Node)val).GetNodeOrNull<Control>(NodePath.op_Implicit("%AllyContainer"));
			Control nodeOrNull2 = ((Node)val).GetNodeOrNull<Control>(NodePath.op_Implicit("%EnemyContainer"));
			if (nodeOrNull != null)
			{
				nodeOrNull.Position += Vector2.Down * 30f;
			}
			if (nodeOrNull2 != null)
			{
				nodeOrNull2.Position += Vector2.Down * 30f;
			}
		}
	}
}
