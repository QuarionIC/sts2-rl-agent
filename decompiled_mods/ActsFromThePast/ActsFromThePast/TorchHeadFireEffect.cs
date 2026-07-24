using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ActsFromThePast;

public class TorchHeadFireEffect : NSts1Effect
{
	private static readonly string[] FireTextures = new string[2] { "res://ActsFromThePast/vfx/fire1.png", "res://ActsFromThePast/vfx/fire2.png" };

	private Sprite2D _sprite;

	private float _x;

	private float _y;

	private float _vX;

	private float _vY;

	private float _scale;

	private Color _color;

	private bool _flippedX;

	private static Texture2D[] _cachedTextures;

	private static Texture2D GetRandomTexture()
	{
		if (_cachedTextures == null)
		{
			_cachedTextures = ((IEnumerable<string>)FireTextures).Select((Func<string, Texture2D>)GD.Load<Texture2D>).ToArray();
		}
		return _cachedTextures[Random.Shared.Next(_cachedTextures.Length)];
	}

	public static TorchHeadFireEffect Create(Vector2 position)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		TorchHeadFireEffect torchHeadFireEffect = new TorchHeadFireEffect();
		torchHeadFireEffect._x = position.X + (float)GD.RandRange(-5.0, 5.0);
		torchHeadFireEffect._y = position.Y + (float)GD.RandRange(-5.0, 5.0);
		torchHeadFireEffect._vX = (float)GD.RandRange(-30.0, 30.0);
		torchHeadFireEffect._vY = (float)GD.RandRange(-100.0, -20.0);
		torchHeadFireEffect._flippedX = GD.Randf() < 0.5f;
		torchHeadFireEffect.Setup();
		return torchHeadFireEffect;
	}

	protected override void Initialize()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_006f: Expected O, but got Unknown
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		Duration = 0.7f;
		StartingDuration = 0.7f;
		Texture2D randomTexture = GetRandomTexture();
		if (randomTexture == null)
		{
			IsDone = true;
			return;
		}
		_sprite = new Sprite2D();
		_sprite.Texture = randomTexture;
		_sprite.Centered = true;
		_sprite.FlipH = _flippedX;
		CanvasItemMaterial val = new CanvasItemMaterial();
		val.BlendMode = (BlendModeEnum)1;
		((CanvasItem)_sprite).Material = (Material)(object)val;
		((Node)this).AddChild((Node)(object)_sprite, false, (InternalMode)0);
		_scale = (float)GD.RandRange(0.8, 0.9);
		_color = new Color(0.5f, 1f, 0f, 0f);
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	protected override void Update(float delta)
	{
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		_x += _vX * delta;
		_y += _vY * delta;
		Duration -= delta;
		if (Duration < 0f)
		{
			IsDone = true;
			return;
		}
		_color.A = Duration / 2f;
		((Node2D)this).Position = new Vector2(_x, _y);
		UpdateSprite();
	}

	private void UpdateSprite()
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		if (_sprite != null)
		{
			((Node2D)_sprite).Scale = new Vector2(_scale * 1.2f, _scale * 1.2f);
			((CanvasItem)_sprite).Modulate = _color;
		}
	}
}
