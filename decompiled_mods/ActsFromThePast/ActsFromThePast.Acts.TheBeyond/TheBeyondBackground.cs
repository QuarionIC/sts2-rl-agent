using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Acts.TheBeyond;

public class TheBeyondBackground : NCombatBackground
{
	private enum ColumnConfig
	{
		Open,
		SmallOnly,
		SmallPlusLeft,
		SmallPlusRight
	}

	private const string AtlasPath = "res://ActsFromThePast/backgrounds/beyond/scene.atlas";

	private TextureRect _bg1;

	private TextureRect _bg1Glow;

	private TextureRect _bg2;

	private TextureRect _bg2Glow;

	private TextureRect _floor;

	private TextureRect _ceiling;

	private TextureRect _fg;

	private TextureRect _mg1;

	private TextureRect _mg1Glow;

	private TextureRect _mg2;

	private TextureRect _mg2Glow;

	private TextureRect _mg3;

	private TextureRect _mg3Glow;

	private TextureRect _mg4;

	private TextureRect _mg4Glow;

	private TextureRect _c1;

	private TextureRect _c1Glow;

	private TextureRect _c2;

	private TextureRect _c2Glow;

	private TextureRect _c3;

	private TextureRect _c3Glow;

	private TextureRect _c4;

	private TextureRect _c4Glow;

	private TextureRect _f1;

	private TextureRect _f2;

	private TextureRect _f3;

	private TextureRect _f4;

	private TextureRect _f5;

	private Vector2 _f1Base;

	private Vector2 _f2Base;

	private Vector2 _f3Base;

	private Vector2 _f4Base;

	private Vector2 _f5Base;

	private TextureRect _i1;

	private TextureRect _i2;

	private TextureRect _i3;

	private TextureRect _i4;

	private TextureRect _i5;

	private TextureRect _s1;

	private TextureRect _s1Glow;

	private TextureRect _s2;

	private TextureRect _s2Glow;

	private TextureRect _s3;

	private TextureRect _s3Glow;

	private TextureRect _s4;

	private TextureRect _s4Glow;

	private TextureRect _s5;

	private TextureRect _s5Glow;

	private bool _renderAltBg;

	private bool _renderM1;

	private bool _renderM2;

	private bool _renderM3;

	private bool _renderM4;

	private bool _renderF1;

	private bool _renderF2;

	private bool _renderF3;

	private bool _renderF4;

	private bool _renderF5;

	private bool _renderIce;

	private bool _renderI1;

	private bool _renderI2;

	private bool _renderI3;

	private bool _renderI4;

	private bool _renderI5;

	private bool _renderStalactites;

	private bool _renderS1;

	private bool _renderS2;

	private bool _renderS3;

	private bool _renderS4;

	private bool _renderS5;

	private ColumnConfig _columnConfig = ColumnConfig.Open;

	private Color _overlayColor;

	private float _overlayGlowAlpha;

	private bool _initialized = false;

	public override void _Ready()
	{
		((Node)this)._Ready();
		Initialize();
	}

	public void Initialize()
	{
		//IL_042f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0434: Unknown result type (might be due to invalid IL or missing references)
		//IL_0440: Unknown result type (might be due to invalid IL or missing references)
		//IL_0445: Unknown result type (might be due to invalid IL or missing references)
		//IL_0451: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Unknown result type (might be due to invalid IL or missing references)
		//IL_0462: Unknown result type (might be due to invalid IL or missing references)
		//IL_0467: Unknown result type (might be due to invalid IL or missing references)
		//IL_0473: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Unknown result type (might be due to invalid IL or missing references)
		if (!_initialized)
		{
			_initialized = true;
			((Control)this).SetAnchorsPreset((LayoutPreset)15, false);
			((Control)this).MouseFilter = (MouseFilterEnum)2;
			((CanvasItem)this).ZIndex = -100;
			_floor = CreateTextureRect("mod/floor", -65);
			_ceiling = CreateTextureRect("mod/ceiling", -64);
			_bg1 = CreateTextureRect("mod/bg1", -63);
			_bg2 = CreateTextureRect("mod/bg2", -62);
			_mg2 = CreateTextureRect("mod/mod2", -59);
			_mg1 = CreateTextureRect("mod/mod1", -58);
			_mg3 = CreateTextureRect("mod/mod3", -57);
			_mg4 = CreateTextureRect("mod/mod4", -56);
			_c1 = CreateTextureRect("mod/c1", -50);
			_c2 = CreateTextureRect("mod/c2", -49);
			_c3 = CreateTextureRect("mod/c3", -48);
			_c4 = CreateTextureRect("mod/c4", -47);
			_s1 = CreateTextureRect("mod/s1", -45);
			_s2 = CreateTextureRect("mod/s2", -44);
			_s3 = CreateTextureRect("mod/s3", -43);
			_s4 = CreateTextureRect("mod/s4", -42);
			_s5 = CreateTextureRect("mod/s5", -41);
			_bg1Glow = CreateTextureRect("mod/bg1", -39);
			_bg2Glow = CreateTextureRect("mod/bg2", -38);
			_mg2Glow = CreateTextureRect("mod/mod2", -37);
			_mg1Glow = CreateTextureRect("mod/mod1", -36);
			_mg3Glow = CreateTextureRect("mod/mod3", -35);
			_mg4Glow = CreateTextureRect("mod/mod4", -34);
			_c1Glow = CreateTextureRect("mod/c1", -33);
			_c2Glow = CreateTextureRect("mod/c2", -32);
			_c3Glow = CreateTextureRect("mod/c3", -31);
			_c4Glow = CreateTextureRect("mod/c4", -30);
			_s1Glow = CreateTextureRect("mod/s1", -29);
			_s2Glow = CreateTextureRect("mod/s2", -28);
			_s3Glow = CreateTextureRect("mod/s3", -27);
			_s4Glow = CreateTextureRect("mod/s4", -26);
			_s5Glow = CreateTextureRect("mod/s5", -25);
			_i1 = CreateTextureRect("mod/i1", -23);
			_i2 = CreateTextureRect("mod/i2", -22);
			_i3 = CreateTextureRect("mod/i3", -21);
			_i4 = CreateTextureRect("mod/i4", -20);
			_i5 = CreateTextureRect("mod/i5", -19);
			_f1 = CreateTextureRect("mod/f1", -15);
			_f2 = CreateTextureRect("mod/f2", -14);
			_f3 = CreateTextureRect("mod/f3", -13);
			_f4 = CreateTextureRect("mod/f4", -12);
			_f5 = CreateTextureRect("mod/f5", -11);
			_fg = CreateTextureRect("mod/fg", -5);
			SetAdditiveBlend(_bg1Glow);
			SetAdditiveBlend(_bg2Glow);
			SetAdditiveBlend(_mg1Glow);
			SetAdditiveBlend(_mg2Glow);
			SetAdditiveBlend(_mg3Glow);
			SetAdditiveBlend(_mg4Glow);
			SetAdditiveBlend(_c1Glow);
			SetAdditiveBlend(_c2Glow);
			SetAdditiveBlend(_c3Glow);
			SetAdditiveBlend(_c4Glow);
			SetAdditiveBlend(_s1Glow);
			SetAdditiveBlend(_s2Glow);
			SetAdditiveBlend(_s3Glow);
			SetAdditiveBlend(_s4Glow);
			SetAdditiveBlend(_s5Glow);
			_f1Base = ((Control)_f1).Position;
			_f2Base = ((Control)_f2).Position;
			_f3Base = ((Control)_f3).Position;
			_f4Base = ((Control)_f4).Position;
			_f5Base = ((Control)_f5).Position;
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
			UpdateFloaterAnimations();
		}
	}

	private void UpdateFloaterAnimations()
	{
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_0111: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_023d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0249: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		ulong ticksMsec = Time.GetTicksMsec();
		float num = 1f;
		if (_renderF1)
		{
			float num2 = Mathf.Cos(Mathf.DegToRad((float)((ticksMsec + 180) / 180 % 360))) * 40f * num;
			float num3 = Mathf.Cos(Mathf.DegToRad((float)((ticksMsec + 500) / 72 % 360))) * 20f * num;
			float num4 = Mathf.Cos(Mathf.DegToRad((float)((ticksMsec + 180) / 180 % 360)));
			((Control)_f1).Position = _f1Base + new Vector2(num2, num3);
			((Control)_f1).Rotation = Mathf.DegToRad(num4);
		}
		if (_renderF2)
		{
			float num5 = Mathf.Cos(Mathf.DegToRad((float)((ticksMsec + 91723) / 72 % 360))) * 20f;
			float num6 = ticksMsec / 120 % 360;
			((Control)_f2).Position = _f2Base + new Vector2(num5, 0f);
			((Control)_f2).Rotation = Mathf.DegToRad(num6);
		}
		if (_renderF3)
		{
			float num7 = -80f * num;
			float num8 = Mathf.Cos(Mathf.DegToRad((float)((ticksMsec + 73) / 2000 % 360))) * 10f - 90f * num;
			float num9 = (float)(ticksMsec / 1000 % 360) * 2f;
			((Control)_f3).Position = _f3Base + new Vector2(num7, num8);
			((Control)_f3).Rotation = Mathf.DegToRad(num9);
		}
		if (_renderF4)
		{
			float num10 = Mathf.Cos(Mathf.DegToRad((float)((ticksMsec + 4442) / 20 % 360))) * 30f * num;
			float num11 = Mathf.Cos(Mathf.DegToRad((float)((ticksMsec + 4442) / 10 % 360))) * 20f;
			((Control)_f4).Position = _f4Base + new Vector2(0f, num10);
			((Control)_f4).Rotation = Mathf.DegToRad(num11);
		}
		if (_renderF5)
		{
			float num12 = Mathf.Cos(Mathf.DegToRad((float)(ticksMsec / 48 % 360))) * 20f;
			((Control)_f5).Position = _f5Base + new Vector2(0f, num12);
			((Control)_f5).Rotation = 0f;
		}
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
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0134: Unknown result type (might be due to invalid IL or missing references)
		TextureRect val = new TextureRect();
		((Control)val).MouseFilter = (MouseFilterEnum)2;
		((CanvasItem)val).ZIndex = zIndex;
		LibGdxAtlas.RegionInfo? regionData = LibGdxAtlas.GetRegionData("res://ActsFromThePast/backgrounds/beyond/scene.atlas", regionName);
		LibGdxAtlas.TextureRegion? region = LibGdxAtlas.GetRegion("res://ActsFromThePast/backgrounds/beyond/scene.atlas", regionName);
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
			((Control)val).PivotOffset = ((Control)val).Size / 2f;
		}
		((Node)this).AddChild((Node)(object)val, false, (InternalMode)0);
		return val;
	}

	public void RandomizeScene()
	{
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		_overlayColor = new Color((float)GD.RandRange(0.7, 0.9), (float)GD.RandRange(0.7, 0.9), (float)GD.RandRange(0.7, 1.0), 1f);
		_overlayGlowAlpha = (float)GD.RandRange(0.0, 0.2);
		_renderAltBg = GD.Randf() < 0.2f;
		_renderM1 = false;
		_renderM2 = false;
		_renderM3 = false;
		_renderM4 = false;
		if (!_renderAltBg && GD.Randf() < 0.8f)
		{
			_renderM1 = GD.Randf() > 0.5f;
			_renderM2 = GD.Randf() > 0.5f;
			_renderM3 = GD.Randf() > 0.5f;
			if (!_renderM3)
			{
				_renderM4 = GD.Randf() > 0.5f;
			}
		}
		if (GD.Randf() < 0.6f)
		{
			_columnConfig = ColumnConfig.Open;
		}
		else if (GD.Randf() > 0.5f)
		{
			_columnConfig = ColumnConfig.SmallOnly;
		}
		else if (GD.Randf() > 0.5f)
		{
			_columnConfig = ColumnConfig.SmallPlusLeft;
		}
		else
		{
			_columnConfig = ColumnConfig.SmallPlusRight;
		}
		_renderF1 = false;
		_renderF2 = false;
		_renderF3 = false;
		_renderF4 = false;
		_renderF5 = false;
		int num = 0;
		_renderF1 = GD.Randf() < 0.25f;
		if (_renderF1)
		{
			num++;
		}
		_renderF2 = GD.Randf() < 0.25f;
		if (_renderF2)
		{
			num++;
		}
		if (num < 2)
		{
			_renderF3 = GD.Randf() < 0.25f;
			if (_renderF3)
			{
				num++;
			}
		}
		if (num < 2)
		{
			_renderF4 = GD.Randf() < 0.25f;
			if (_renderF4)
			{
				num++;
			}
		}
		if (num < 2)
		{
			_renderF5 = GD.Randf() < 0.25f;
		}
		if (GD.Randf() < 0.3f)
		{
			_renderF1 = false;
			_renderF2 = false;
			_renderF3 = false;
			_renderF4 = false;
			_renderF5 = false;
		}
		_renderIce = GD.Randf() > 0.5f;
		if (_renderIce)
		{
			_renderI1 = GD.Randf() > 0.5f;
			_renderI2 = GD.Randf() > 0.5f;
			_renderI3 = GD.Randf() > 0.5f;
			_renderI4 = GD.Randf() > 0.5f;
			_renderI5 = GD.Randf() > 0.5f;
		}
		else
		{
			_renderI1 = false;
			_renderI2 = false;
			_renderI3 = false;
			_renderI4 = false;
			_renderI5 = false;
		}
		_renderStalactites = GD.Randf() > 0.5f;
		if (_renderStalactites)
		{
			_renderS1 = GD.Randf() > 0.5f;
			_renderS2 = GD.Randf() > 0.5f;
			_renderS3 = GD.Randf() > 0.5f;
			_renderS4 = GD.Randf() > 0.5f;
			_renderS5 = GD.Randf() > 0.5f;
		}
		else
		{
			_renderS1 = false;
			_renderS2 = false;
			_renderS3 = false;
			_renderS4 = false;
			_renderS5 = false;
		}
		UpdateVisibility();
	}

	private void UpdateVisibility()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0183: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0206: Unknown result type (might be due to invalid IL or missing references)
		//IL_0213: Unknown result type (might be due to invalid IL or missing references)
		//IL_0220: Unknown result type (might be due to invalid IL or missing references)
		//IL_022d: Unknown result type (might be due to invalid IL or missing references)
		//IL_023a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0247: Unknown result type (might be due to invalid IL or missing references)
		//IL_0254: Unknown result type (might be due to invalid IL or missing references)
		//IL_0261: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_027c: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0309: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		Color modulate = default(Color);
		((Color)(ref modulate))._002Ector((1f + _overlayColor.R) / 2f, (1f + _overlayColor.G) / 2f, (1f + _overlayColor.B) / 2f, 1f);
		Color modulate2 = default(Color);
		((Color)(ref modulate2))._002Ector(_overlayColor.R, _overlayColor.G, _overlayColor.B, _overlayGlowAlpha);
		((CanvasItem)_floor).Modulate = _overlayColor;
		((CanvasItem)_ceiling).Modulate = _overlayColor;
		((CanvasItem)_bg1).Modulate = _overlayColor;
		((CanvasItem)_bg2).Modulate = _overlayColor;
		((CanvasItem)_mg1).Modulate = _overlayColor;
		((CanvasItem)_mg2).Modulate = _overlayColor;
		((CanvasItem)_mg3).Modulate = _overlayColor;
		((CanvasItem)_mg4).Modulate = _overlayColor;
		((CanvasItem)_c1).Modulate = _overlayColor;
		((CanvasItem)_c2).Modulate = _overlayColor;
		((CanvasItem)_c3).Modulate = _overlayColor;
		((CanvasItem)_c4).Modulate = _overlayColor;
		((CanvasItem)_s1).Modulate = _overlayColor;
		((CanvasItem)_s2).Modulate = _overlayColor;
		((CanvasItem)_s3).Modulate = _overlayColor;
		((CanvasItem)_s4).Modulate = _overlayColor;
		((CanvasItem)_s5).Modulate = _overlayColor;
		((CanvasItem)_bg1Glow).Modulate = modulate2;
		((CanvasItem)_bg2Glow).Modulate = modulate2;
		((CanvasItem)_mg1Glow).Modulate = modulate2;
		((CanvasItem)_mg2Glow).Modulate = modulate2;
		((CanvasItem)_mg3Glow).Modulate = modulate2;
		((CanvasItem)_mg4Glow).Modulate = modulate2;
		((CanvasItem)_c1Glow).Modulate = modulate2;
		((CanvasItem)_c2Glow).Modulate = modulate2;
		((CanvasItem)_c3Glow).Modulate = modulate2;
		((CanvasItem)_c4Glow).Modulate = modulate2;
		((CanvasItem)_s1Glow).Modulate = modulate2;
		((CanvasItem)_s2Glow).Modulate = modulate2;
		((CanvasItem)_s3Glow).Modulate = modulate2;
		((CanvasItem)_s4Glow).Modulate = modulate2;
		((CanvasItem)_s5Glow).Modulate = modulate2;
		((CanvasItem)_i1).Modulate = _overlayColor;
		((CanvasItem)_i2).Modulate = _overlayColor;
		((CanvasItem)_i3).Modulate = _overlayColor;
		((CanvasItem)_i4).Modulate = _overlayColor;
		((CanvasItem)_i5).Modulate = _overlayColor;
		((CanvasItem)_f1).Modulate = modulate;
		((CanvasItem)_f2).Modulate = modulate;
		((CanvasItem)_f3).Modulate = modulate;
		((CanvasItem)_f4).Modulate = modulate;
		((CanvasItem)_f5).Modulate = modulate;
		((CanvasItem)_fg).Modulate = modulate;
		((CanvasItem)_bg2).Visible = _renderAltBg;
		((CanvasItem)_bg2Glow).Visible = _renderAltBg;
		((CanvasItem)_mg1).Visible = _renderM1;
		((CanvasItem)_mg1Glow).Visible = _renderM1;
		((CanvasItem)_mg2).Visible = _renderM2;
		((CanvasItem)_mg2Glow).Visible = _renderM2;
		((CanvasItem)_mg3).Visible = _renderM3;
		((CanvasItem)_mg3Glow).Visible = _renderM3;
		((CanvasItem)_mg4).Visible = _renderM4;
		((CanvasItem)_mg4Glow).Visible = _renderM4;
		bool visible = _columnConfig != ColumnConfig.Open;
		bool visible2 = _columnConfig == ColumnConfig.SmallPlusLeft;
		bool visible3 = _columnConfig == ColumnConfig.SmallPlusRight;
		bool visible4 = _columnConfig != ColumnConfig.Open;
		((CanvasItem)_c1).Visible = visible;
		((CanvasItem)_c1Glow).Visible = visible;
		((CanvasItem)_c2).Visible = visible2;
		((CanvasItem)_c2Glow).Visible = visible2;
		((CanvasItem)_c3).Visible = visible3;
		((CanvasItem)_c3Glow).Visible = visible3;
		((CanvasItem)_c4).Visible = visible4;
		((CanvasItem)_c4Glow).Visible = visible4;
		((CanvasItem)_s1).Visible = _renderS1;
		((CanvasItem)_s1Glow).Visible = _renderS1;
		((CanvasItem)_s2).Visible = _renderS2;
		((CanvasItem)_s2Glow).Visible = _renderS2;
		((CanvasItem)_s3).Visible = _renderS3;
		((CanvasItem)_s3Glow).Visible = _renderS3;
		((CanvasItem)_s4).Visible = _renderS4;
		((CanvasItem)_s4Glow).Visible = _renderS4;
		((CanvasItem)_s5).Visible = _renderS5;
		((CanvasItem)_s5Glow).Visible = _renderS5;
		((CanvasItem)_i1).Visible = _renderI1;
		((CanvasItem)_i2).Visible = _renderI2;
		((CanvasItem)_i3).Visible = _renderI3;
		((CanvasItem)_i4).Visible = _renderI4;
		((CanvasItem)_i5).Visible = _renderI5;
		((CanvasItem)_f1).Visible = _renderF1;
		((CanvasItem)_f2).Visible = _renderF2;
		((CanvasItem)_f3).Visible = _renderF3;
		((CanvasItem)_f4).Visible = _renderF4;
		((CanvasItem)_f5).Visible = _renderF5;
	}

	public void OnTreeEntered()
	{
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
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

	public void SetBossMode()
	{
	}
}
