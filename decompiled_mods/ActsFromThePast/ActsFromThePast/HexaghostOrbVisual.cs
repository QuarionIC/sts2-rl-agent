using System;
using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public class HexaghostOrbVisual : IDisposable
{
	private readonly int _index;

	private readonly Vector2 _basePosition;

	private Node _parentNode;

	private Vector2 _currentPosition;

	private float _activateTimer;

	private float _bobTimer;

	private float _particleTimer;

	private bool _playedSfx;

	private const float BobSpeed = 2f;

	private const float BobAmount = 3f;

	private const float ParticleInterval = 0.06f;

	public bool IsActivated { get; private set; }

	public bool IsHidden { get; private set; } = true;

	public HexaghostOrbVisual(int index, Vector2 position)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		_index = index;
		_basePosition = position + new Vector2((float)GD.RandRange(-10.0, 10.0), (float)GD.RandRange(-10.0, 10.0));
		_currentPosition = _basePosition;
		_activateTimer = (float)index * 0.3f;
	}

	public void SetParentNode(Node parent)
	{
		_parentNode = parent;
	}

	public void Activate(bool immediate = false)
	{
		_playedSfx = false;
		IsActivated = true;
		IsHidden = false;
		_activateTimer = (immediate ? 0f : ((float)_index * 0.3f));
	}

	public void Deactivate()
	{
		IsActivated = false;
	}

	public void Hide()
	{
		IsHidden = true;
	}

	public Vector2 GetGlobalPosition(Vector2 parentGlobalPosition)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		return parentGlobalPosition + _currentPosition;
	}

	public void Update(float delta, Vector2 parentGlobalPosition)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		if (IsHidden)
		{
			return;
		}
		_bobTimer += 2f * delta;
		float num = Mathf.Sin(_bobTimer) * 3f;
		_currentPosition = _basePosition + new Vector2(num * 2f, num * 2f);
		Vector2 globalPosition = GetGlobalPosition(parentGlobalPosition);
		if (IsActivated)
		{
			_activateTimer -= delta;
			if (_activateTimer < 0f)
			{
				if (!_playedSfx)
				{
					_playedSfx = true;
					SpawnIgniteEffect(globalPosition);
					PlayIgniteSound();
				}
				_particleTimer -= delta;
				if (_particleTimer < 0f)
				{
					SpawnFireEffect(globalPosition);
					_particleTimer = 0.06f;
				}
			}
		}
		else
		{
			_particleTimer -= delta;
			if (_particleTimer < 0f)
			{
				SpawnWeakFireEffect(globalPosition);
				_particleTimer = 0.06f;
			}
		}
	}

	private void SpawnIgniteEffect(Vector2 pos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		GhostIgniteEffect ghostIgniteEffect = GhostIgniteEffect.Create(pos.X, pos.Y);
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			((Node)instance.CombatVfxContainer).AddChild((Node)(object)ghostIgniteEffect, false, (InternalMode)0);
		}
	}

	private void SpawnFireEffect(Vector2 pos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		GhostlyFireEffect ghostlyFireEffect = GhostlyFireEffect.Create(pos.X, pos.Y);
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			((Node)instance.CombatVfxContainer).AddChild((Node)(object)ghostlyFireEffect, false, (InternalMode)0);
		}
	}

	private void SpawnWeakFireEffect(Vector2 pos)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		GhostlyWeakFireEffect ghostlyWeakFireEffect = GhostlyWeakFireEffect.Create(pos.X, pos.Y);
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			((Node)instance.CombatVfxContainer).AddChild((Node)(object)ghostlyWeakFireEffect, false, (InternalMode)0);
		}
	}

	private void PlayIgniteSound()
	{
		if (GD.Randf() < 0.5f)
		{
			AFTPModAudio.Play("hexaghost", "ghost_orb_ignite_1");
		}
		else
		{
			AFTPModAudio.Play("hexaghost", "ghost_orb_ignite_2");
		}
	}

	public void Dispose()
	{
	}
}
