using System.Collections.Generic;
using ActsFromThePast.Patches.Acts;
using Godot;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Acts.TheCity;

public class TheCityBackground : NCombatBackground
{
	private enum PillarConfig
	{
		Open,
		SidesOnly,
		Full,
		Left1,
		Left2
	}

	private const string AtlasPath = "res://ActsFromThePast/backgrounds/city/scene.atlas";

	private TextureRect _bg;

	private TextureRect _bgGlow;

	private TextureRect _bgGlow2;

	private TextureRect _bg2;

	private TextureRect _bg2Glow;

	private TextureRect _bgGlow2Double;

	private TextureRect _floor;

	private TextureRect _ceiling;

	private TextureRect _wall;

	private TextureRect _chains;

	private TextureRect _chainsGlow;

	private TextureRect _chainsGlow2;

	private TextureRect _mg;

	private TextureRect _mgGlow;

	private TextureRect _mgGlow2;

	private TextureRect _mgAlt;

	private TextureRect _fg;

	private TextureRect _fgGlow;

	private TextureRect _fg2;

	private TextureRect _throne;

	private TextureRect _throneGlow;

	private TextureRect _pillar1;

	private TextureRect _pillar2;

	private TextureRect _pillar3;

	private TextureRect _pillar4;

	private TextureRect _pillar5;

	private bool _renderAltBg;

	private bool _renderMg;

	private bool _renderMgGlow;

	private bool _renderMgAlt;

	private bool _renderWall;

	private bool _renderChains;

	private bool _renderThrone;

	private bool _renderFg2;

	private bool _darkDay;

	private PillarConfig _pillarConfig = PillarConfig.Open;

	private List<FireFlyEffect> _fireFlies = new List<FireFlyEffect>();

	private bool _hasFlies;

	private bool _blueFlies;

	private List<NSts1Effect> _ceilingDustEffects = new List<NSts1Effect>();

	private float _ceilingDustTimer = 1f;

	private Color _overlayColor = Colors.White;

	private ColorRect _overlayRect;

	private bool _initialized = false;

	public override void _Ready()
	{
		((Node)this)._Ready();
		Initialize();
	}

	public void Initialize()
	{
		if (!_initialized)
		{
			_initialized = true;
			((Control)this).SetAnchorsPreset((LayoutPreset)15, false);
			((Control)this).MouseFilter = (MouseFilterEnum)2;
			((CanvasItem)this).ZIndex = -100;
			_bg = CreateTextureRect("mod/bg1", -50);
			_bgGlow = CreateTextureRect("mod/bgGlowv2", -49);
			_bgGlow2 = CreateTextureRect("mod/bgGlowBlur", -48);
			_bg2 = CreateTextureRect("mod/bg2", -46);
			_bg2Glow = CreateTextureRect("mod/bg2Glow", -45);
			_bgGlow2 = CreateTextureRect("mod/bgGlowBlur", -48);
			_bgGlow2Double = CreateTextureRect("mod/bgGlowBlur", -47);
			_floor = CreateTextureRect("mod/floor", -45);
			_ceiling = CreateTextureRect("mod/ceiling", -44);
			_wall = CreateTextureRect("mod/wall", -43);
			_chains = CreateTextureRect("mod/chains", -42);
			_chainsGlow = CreateTextureRect("mod/chainsGlow", -41);
			_chainsGlow2 = CreateTextureRect("mod/chainsGlow", -40);
			_mg = CreateTextureRect("mod/mg1", -39);
			_mgGlow = CreateTextureRect("mod/mg1Glow", -38);
			_mgGlow2 = CreateTextureRect("mod/mg1Glow", -37);
			_mgAlt = CreateTextureRect("mod/mg2", -36);
			_pillar1 = CreateTextureRect("mod/p1", -35);
			_pillar2 = CreateTextureRect("mod/p2", -34);
			_pillar3 = CreateTextureRect("mod/p3", -33);
			_pillar4 = CreateTextureRect("mod/p4", -32);
			_pillar5 = CreateTextureRect("mod/p5", -31);
			_throne = CreateTextureRect("mod/throne", -30);
			_throneGlow = CreateTextureRect("mod/throneGlow", -29);
			_fg = CreateTextureRect("mod/fg", -20);
			_fgGlow = CreateTextureRect("mod/fgGlow", -19);
			_fg2 = CreateTextureRect("mod/fgHideWindow", -18);
			SetAdditiveBlend(_bgGlow);
			SetAdditiveBlend(_bgGlow2);
			SetAdditiveBlend(_bgGlow2Double);
			SetAdditiveBlend(_bg2Glow);
			SetAdditiveBlend(_chainsGlow);
			SetAdditiveBlend(_chainsGlow2);
			SetAdditiveBlend(_mgGlow);
			SetAdditiveBlend(_mgGlow2);
			SetAdditiveBlend(_throneGlow);
			SetAdditiveBlend(_fgGlow);
			RandomizeScene();
		}
	}

	private void SetAdditiveBlend(TextureRect rect)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		CanvasItemMaterial val = new CanvasItemMaterial();
		val.BlendMode = (BlendModeEnum)1;
		((CanvasItem)rect).Material = (Material)(object)val;
	}

	public override void _EnterTree()
	{
		((Node)this)._EnterTree();
		((Node)this).GetTree().ProcessFrame += OnProcessFrame;
	}

	public override void _ExitTree()
	{
		((Node)this).GetTree().ProcessFrame -= OnProcessFrame;
		((Node)this)._ExitTree();
	}

	private void OnProcessFrame()
	{
		if (_initialized && ((Node)this).IsInsideTree())
		{
			UpdateFireFlies();
			UpdateGlowAnimations();
			UpdateCeilingDust((float)((Node)this).GetProcessDeltaTime());
		}
	}

	private void UpdateCeilingDust(float delta)
	{
		for (int num = _ceilingDustEffects.Count - 1; num >= 0; num--)
		{
			NSts1Effect nSts1Effect = _ceilingDustEffects[num];
			if (nSts1Effect.IsDone || !GodotObject.IsInstanceValid((GodotObject)(object)nSts1Effect))
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)nSts1Effect))
				{
					((Node)nSts1Effect).QueueFree();
				}
				_ceilingDustEffects.RemoveAt(num);
			}
		}
		_ceilingDustTimer -= delta;
		if (_ceilingDustTimer < 0f)
		{
			switch ((int)(GD.Randi() % 5))
			{
			case 0:
				SpawnCeilingDust();
				PlayDustSfx(boom: false);
				break;
			case 1:
				SpawnCeilingDust();
				SpawnCeilingDust();
				PlayDustSfx(boom: false);
				break;
			default:
				SpawnCeilingDust();
				SpawnCeilingDust();
				SpawnCeilingDust();
				PlayDustSfx(boom: true);
				break;
			}
			_ceilingDustTimer = (float)GD.RandRange(0.5, 60.0);
		}
	}

	private void PlayDustSfx(bool boom)
	{
		int num = (int)(GD.Randi() % 3);
		if (boom)
		{
			if (1 == 0)
			{
			}
			string text = num switch
			{
				0 => "ceiling_boom_1", 
				1 => "ceiling_boom_2", 
				_ => "ceiling_boom_3", 
			};
			if (1 == 0)
			{
			}
			string soundName = text;
			AFTPModAudio.Play("general", soundName, 0f, 0.2f);
		}
		else
		{
			if (1 == 0)
			{
			}
			string text = num switch
			{
				0 => "ceiling_dust_1", 
				1 => "ceiling_dust_2", 
				_ => "ceiling_dust_3", 
			};
			if (1 == 0)
			{
			}
			string soundName2 = text;
			AFTPModAudio.Play("general", soundName2, 0f, 0.2f);
		}
	}

	private void SpawnCeilingDust()
	{
		CeilingDustEffect ceilingDustEffect = CeilingDustEffect.Create(AddCeilingDustEffect);
		((CanvasItem)ceilingDustEffect).ZIndex = -10;
		((Node)this).AddChild((Node)(object)ceilingDustEffect, false, (InternalMode)0);
		_ceilingDustEffects.Add(ceilingDustEffect);
	}

	private void AddCeilingDustEffect(NSts1Effect effect)
	{
		((CanvasItem)effect).ZIndex = -10;
		((Node)this).AddChild((Node)(object)effect, false, (InternalMode)0);
		_ceilingDustEffects.Add(effect);
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
		LibGdxAtlas.RegionInfo? regionData = LibGdxAtlas.GetRegionData("res://ActsFromThePast/backgrounds/city/scene.atlas", regionName);
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/backgrounds/city/scene.atlas", regionName);
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

	public void RandomizeScene()
	{
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		_hasFlies = GD.Randf() > 0.5f;
		_blueFlies = GD.Randf() > 0.5f;
		_overlayColor = new Color((float)GD.RandRange(0.8, 0.9), (float)GD.RandRange(0.8, 0.9), (float)GD.RandRange(0.95, 1.0), 1f);
		_darkDay = GD.Randf() < 0.33f;
		if (_darkDay)
		{
			_overlayColor = new Color(0.6f, (float)GD.RandRange(0.7, 0.8), (float)GD.RandRange(0.8, 0.95), 1f);
		}
		_renderAltBg = GD.Randf() > 0.5f;
		_renderMg = true;
		if (_renderMg)
		{
			_renderMgAlt = GD.Randf() > 0.5f;
			if (!_renderMgAlt)
			{
				_renderMgGlow = GD.Randf() > 0.5f;
			}
		}
		_renderWall = GD.Randi() % 5 == 4;
		_renderChains = _renderWall && GD.Randf() > 0.5f;
		_renderFg2 = GD.Randf() > 0.5f;
		if (_renderWall)
		{
			int num = (int)(GD.Randi() % 3);
			if (1 == 0)
			{
			}
			PillarConfig pillarConfig = num switch
			{
				0 => PillarConfig.Open, 
				1 => PillarConfig.Left1, 
				_ => PillarConfig.Left2, 
			};
			if (1 == 0)
			{
			}
			_pillarConfig = pillarConfig;
		}
		else
		{
			int num2 = (int)(GD.Randi() % 3);
			if (1 == 0)
			{
			}
			PillarConfig pillarConfig = num2 switch
			{
				0 => PillarConfig.Open, 
				1 => PillarConfig.SidesOnly, 
				_ => PillarConfig.Full, 
			};
			if (1 == 0)
			{
			}
			_pillarConfig = pillarConfig;
		}
		_renderThrone = false;
		UpdateVisibility();
	}

	private void UpdateVisibility()
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		((CanvasItem)_bg).Modulate = _overlayColor;
		((CanvasItem)_floor).Modulate = _overlayColor;
		((CanvasItem)_ceiling).Modulate = _overlayColor;
		((CanvasItem)_wall).Modulate = _overlayColor;
		((CanvasItem)_chains).Modulate = _overlayColor;
		((CanvasItem)_mg).Modulate = _overlayColor;
		((CanvasItem)_bg2).Visible = _renderAltBg;
		((CanvasItem)_bg2Glow).Visible = _renderAltBg;
		((CanvasItem)_bgGlow2).Visible = _darkDay;
		((CanvasItem)_bgGlow2Double).Visible = _darkDay;
		((CanvasItem)_bgGlow2Double).Modulate = new Color(1f, 1f, 1f, 0.7f);
		((CanvasItem)_wall).Visible = _renderWall;
		((CanvasItem)_chains).Visible = _renderChains;
		((CanvasItem)_chainsGlow).Visible = _renderChains;
		((CanvasItem)_chainsGlow2).Visible = _renderChains;
		((CanvasItem)_mg).Visible = _renderMg;
		((CanvasItem)_mgGlow).Visible = _renderMg;
		((CanvasItem)_mgGlow2).Visible = _renderMg && _renderMgGlow;
		((CanvasItem)_mgAlt).Visible = _renderMgAlt;
		((CanvasItem)_mgAlt).Modulate = (Color)(_renderMgGlow ? new Color(1f, 1f, 0.9f, 1f) : Colors.White);
		((CanvasItem)_fg2).Visible = _renderFg2;
		((CanvasItem)_throne).Visible = _renderThrone;
		((CanvasItem)_throneGlow).Visible = _renderThrone;
		TextureRect pillar = _pillar1;
		PillarConfig pillarConfig = _pillarConfig;
		bool visible = (uint)(pillarConfig - 1) <= 3u;
		((CanvasItem)pillar).Visible = visible;
		TextureRect pillar2 = _pillar2;
		pillarConfig = _pillarConfig;
		visible = ((pillarConfig == PillarConfig.Full || pillarConfig == PillarConfig.Left2) ? true : false);
		((CanvasItem)pillar2).Visible = visible;
		((CanvasItem)_pillar3).Visible = _pillarConfig == PillarConfig.Full;
		((CanvasItem)_pillar4).Visible = _pillarConfig == PillarConfig.Full;
		TextureRect pillar3 = _pillar5;
		pillarConfig = _pillarConfig;
		visible = (uint)(pillarConfig - 1) <= 1u;
		((CanvasItem)pillar3).Visible = visible;
	}

	private void UpdateFireFlies()
	{
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		if (!_hasFlies)
		{
			return;
		}
		for (int num = _fireFlies.Count - 1; num >= 0; num--)
		{
			FireFlyEffect fireFlyEffect = _fireFlies[num];
			if (fireFlyEffect.IsDone || !GodotObject.IsInstanceValid((GodotObject)(object)fireFlyEffect))
			{
				if (GodotObject.IsInstanceValid((GodotObject)(object)fireFlyEffect))
				{
					((Node)fireFlyEffect).QueueFree();
				}
				_fireFlies.RemoveAt(num);
			}
		}
		if (_fireFlies.Count < 9 && GD.Randf() < 0.1f)
		{
			Color color = default(Color);
			if (_blueFlies)
			{
				((Color)(ref color))._002Ector((float)GD.RandRange(0.1, 0.2), (float)GD.RandRange(0.6, 0.8), (float)GD.RandRange(0.8, 1.0), 1f);
			}
			else
			{
				((Color)(ref color))._002Ector((float)GD.RandRange(0.8, 1.0), (float)GD.RandRange(0.5, 0.8), (float)GD.RandRange(0.3, 0.5), 1f);
			}
			FireFlyEffect fireFlyEffect2 = FireFlyEffect.Create(color);
			((CanvasItem)fireFlyEffect2).ZIndex = -15;
			((Node)this).AddChild((Node)(object)fireFlyEffect2, false, (InternalMode)0);
			_fireFlies.Add(fireFlyEffect2);
		}
	}

	private void UpdateGlowAnimations()
	{
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		if (_renderChains)
		{
			float num = Time.GetTicksMsec() % 360;
			float num2 = Mathf.Cos(Mathf.DegToRad(num)) / 10f + 0.9f;
			Color modulate = default(Color);
			((Color)(ref modulate))._002Ector(1f, 1f, 1f, num2);
			((CanvasItem)_chainsGlow).Modulate = modulate;
			((CanvasItem)_chainsGlow2).Modulate = modulate;
		}
		if (_renderMg)
		{
			if (_renderMgGlow)
			{
				float num3 = Time.GetTicksMsec() / 10 % 360;
				float num4 = Mathf.Cos(Mathf.DegToRad(num3)) / 2f + 0.5f;
				Color modulate2 = default(Color);
				((Color)(ref modulate2))._002Ector(1f, 1f, 0.9f, num4);
				((CanvasItem)_mgGlow).Modulate = modulate2;
				((CanvasItem)_mgGlow2).Modulate = modulate2;
			}
			else
			{
				((CanvasItem)_mgGlow).Modulate = Colors.White;
			}
		}
	}

	public void OnTreeEntered()
	{
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_011f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0133: Unknown result type (might be due to invalid IL or missing references)
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
			Control nodeOrNull = ((Node)val).GetNodeOrNull<Control>(NodePath.op_Implicit("%CombatSceneContainer"));
			if (nodeOrNull != null)
			{
				ReparentToContainer(_fg, nodeOrNull, -3);
				ReparentToContainer(_fgGlow, nodeOrNull, -3);
				ReparentToContainer(_fg2, nodeOrNull, -3);
			}
			Control nodeOrNull2 = ((Node)val).GetNodeOrNull<Control>(NodePath.op_Implicit("%AllyContainer"));
			Control nodeOrNull3 = ((Node)val).GetNodeOrNull<Control>(NodePath.op_Implicit("%EnemyContainer"));
			if (nodeOrNull2 != null)
			{
				nodeOrNull2.Position += Vector2.Down * 30f;
			}
			if (nodeOrNull3 != null)
			{
				nodeOrNull3.Position += Vector2.Down * 30f;
			}
			if (LegacyActTracker.IsCollectorEncounter)
			{
				SetBossMode(isCollector: true);
			}
		}
	}

	private void LogSceneTree(Node node, int depth = 0)
	{
		string value = new string(' ', depth * 2);
		CanvasItem val = (CanvasItem)(object)((node is CanvasItem) ? node : null);
		string value2 = ((val != null) ? $" [Z:{val.ZIndex}, ZRelative:{val.ZAsRelative}]" : "");
		Log.Info($"{value}{node.Name} ({((object)node).GetType().Name}){value2}", 2);
		foreach (Node child in node.GetChildren(false))
		{
			LogSceneTree(child, depth + 1);
		}
	}

	private void ReparentToContainer(TextureRect layer, Control container, int zIndex)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		Vector2 globalPosition = ((Control)layer).GlobalPosition;
		((Node)layer).GetParent().RemoveChild((Node)(object)layer);
		((Node)container).AddChild((Node)(object)layer, false, (InternalMode)0);
		((Control)layer).GlobalPosition = globalPosition;
		((CanvasItem)layer).ZIndex = zIndex;
	}

	public void SetBossMode(bool isCollector)
	{
		_renderWall = false;
		_renderChains = false;
		int num = (int)(GD.Randi() % 3);
		if (1 == 0)
		{
		}
		PillarConfig pillarConfig = num switch
		{
			0 => PillarConfig.Open, 
			1 => PillarConfig.SidesOnly, 
			_ => PillarConfig.Full, 
		};
		if (1 == 0)
		{
		}
		_pillarConfig = pillarConfig;
		_renderThrone = isCollector;
		UpdateVisibility();
	}
}
