using System;
using System.Threading.Tasks;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace ActsFromThePast;

public class HexaghostVisuals : IDisposable
{
	private readonly Creature _creature;

	private readonly NCreature _creatureNode;

	private Sprite2D _plasma1;

	private Sprite2D _plasma2;

	private Sprite2D _plasma3;

	private Sprite2D _shadow;

	private float _rotationSpeed = 1f;

	private float _targetRotationSpeed = 30f;

	private float _plasma1Angle;

	private float _plasma2Angle;

	private float _plasma3Angle;

	private float _bobTimer;

	private float _bobOffset;

	private const float BobSpeed = 0.75f;

	private const float BobAmount = 5f;

	private readonly HexaghostOrbVisual[] _orbs = new HexaghostOrbVisual[6];

	private static readonly Vector2[] OrbPositions = (Vector2[])(object)new Vector2[6]
	{
		new Vector2(-90f, -370f),
		new Vector2(90f, -370f),
		new Vector2(160f, -240f),
		new Vector2(90f, -110f),
		new Vector2(-90f, -110f),
		new Vector2(-160f, -240f)
	};

	private const float BodyOffsetY = -225f;

	public HexaghostVisuals(Creature creature, NCreature creatureNode)
	{
		_creature = creature;
		_creatureNode = creatureNode;
		CreatePlasmaLayers();
		CreateOrbs();
		TaskHelper.RunSafely(UpdateLoop());
	}

	private void CreatePlasmaLayers()
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Expected O, but got Unknown
		NCreatureVisuals visuals = _creatureNode.Visuals;
		_plasma3 = new Sprite2D();
		_plasma3.Texture = PreloadManager.Cache.GetTexture2D("res://ActsFromThePast/monsters/hexaghost/plasma3.png");
		((CanvasItem)_plasma3).ZIndex = -3;
		((Node)visuals).AddChild((Node)(object)_plasma3, false, (InternalMode)0);
		_plasma2 = new Sprite2D();
		_plasma2.Texture = PreloadManager.Cache.GetTexture2D("res://ActsFromThePast/monsters/hexaghost/plasma2.png");
		((CanvasItem)_plasma2).ZIndex = -2;
		((Node)visuals).AddChild((Node)(object)_plasma2, false, (InternalMode)0);
		_plasma1 = new Sprite2D();
		_plasma1.Texture = PreloadManager.Cache.GetTexture2D("res://ActsFromThePast/monsters/hexaghost/plasma1.png");
		((CanvasItem)_plasma1).ZIndex = -1;
		((Node)visuals).AddChild((Node)(object)_plasma1, false, (InternalMode)0);
		_shadow = new Sprite2D();
		_shadow.Texture = PreloadManager.Cache.GetTexture2D("res://ActsFromThePast/monsters/hexaghost/shadow.png");
		((CanvasItem)_shadow).ZIndex = -4;
		((Node)visuals).AddChild((Node)(object)_shadow, false, (InternalMode)0);
	}

	private void CreateOrbs()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		for (int i = 0; i < 6; i++)
		{
			HexaghostOrbVisual hexaghostOrbVisual = new HexaghostOrbVisual(i, OrbPositions[i]);
			hexaghostOrbVisual.SetParentNode((Node)(object)_creatureNode.Visuals);
			_orbs[i] = hexaghostOrbVisual;
		}
	}

	private async Task UpdateLoop()
	{
		while (GodotObject.IsInstanceValid((GodotObject)(object)_creatureNode) && _creature.IsAlive)
		{
			float delta = (float)((Node)_creatureNode).GetProcessDeltaTime();
			Update(delta);
			await ((GodotObject)_creatureNode).ToSignal((GodotObject)(object)((Node)_creatureNode).GetTree(), SignalName.ProcessFrame);
		}
	}

	private void Update(float delta)
	{
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		_rotationSpeed = Mathf.Lerp(_rotationSpeed, _targetRotationSpeed, delta * 5f);
		_plasma1Angle -= _rotationSpeed * delta;
		_plasma2Angle -= _rotationSpeed / 2f * delta;
		_plasma3Angle -= _rotationSpeed / 3f * delta;
		_bobTimer += 0.75f * delta;
		_bobOffset = Mathf.Sin(_bobTimer) * 5f;
		((Node2D)_plasma1).Rotation = Mathf.DegToRad(_plasma1Angle);
		((Node2D)_plasma1).Position = new Vector2(0f, (0f - _bobOffset) * 0.5f + -225f);
		((Node2D)_plasma2).Rotation = Mathf.DegToRad(_plasma2Angle);
		((Node2D)_plasma2).Position = new Vector2(6f, 0f - _bobOffset + -225f);
		((Node2D)_plasma3).Rotation = Mathf.DegToRad(_plasma3Angle);
		((Node2D)_plasma3).Scale = Vector2.One * 0.95f;
		((Node2D)_plasma3).Position = new Vector2(12f, (0f - _bobOffset) * 2f + -225f);
		((Node2D)_shadow).Position = new Vector2(12f, (0f - _bobOffset) / 4f - 15f + -225f);
		Vector2 globalPosition = ((Node2D)_creatureNode.Visuals).GlobalPosition;
		HexaghostOrbVisual[] orbs = _orbs;
		foreach (HexaghostOrbVisual hexaghostOrbVisual in orbs)
		{
			hexaghostOrbVisual.Update(delta, globalPosition);
		}
	}

	public void SetTargetRotationSpeed(float speed)
	{
		_targetRotationSpeed = speed;
	}

	public void ActivateAllOrbs()
	{
		for (int i = 0; i < 6; i++)
		{
			_orbs[i].Activate();
		}
	}

	public void ActivateNextOrb()
	{
		for (int i = 0; i < 6; i++)
		{
			if (!_orbs[i].IsActivated)
			{
				_orbs[i].Activate(immediate: true);
				break;
			}
		}
	}

	public void DeactivateAllOrbs()
	{
		HexaghostOrbVisual[] orbs = _orbs;
		foreach (HexaghostOrbVisual hexaghostOrbVisual in orbs)
		{
			hexaghostOrbVisual.Deactivate();
		}
	}

	public void HideAllOrbs()
	{
		HexaghostOrbVisual[] orbs = _orbs;
		foreach (HexaghostOrbVisual hexaghostOrbVisual in orbs)
		{
			hexaghostOrbVisual.Hide();
		}
	}

	public void Dispose()
	{
		SafeFree((Node)(object)_plasma1);
		SafeFree((Node)(object)_plasma2);
		SafeFree((Node)(object)_plasma3);
		SafeFree((Node)(object)_shadow);
		HexaghostOrbVisual[] orbs = _orbs;
		for (int i = 0; i < orbs.Length; i++)
		{
			orbs[i]?.Dispose();
		}
	}

	private static void SafeFree(Node node)
	{
		if (node != null && GodotObject.IsInstanceValid((GodotObject)(object)node))
		{
			node.QueueFree();
		}
	}
}
