using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Random;

namespace ActsFromThePast.Minigames;

public class PortalMapBuilderMinigame
{
	private const int NodeCount = 6;

	private const int Budget = 11;

	private static readonly MapPointType[] AvailableTypes;

	private static readonly MapPointType[] RandomizableTypes;

	private static readonly Dictionary<MapPointType, int> PointCosts;

	private readonly TaskCompletionSource _completionSource = new TaskCompletionSource();

	private readonly Player _owner;

	private int _selectedIndex = -1;

	public MapPointType[] Nodes { get; }

	public Rng Rng { get; }

	public int MaxBudget => 11;

	public int AvailableNodeCount { get; }

	public bool IsRandomized { get; private set; }

	public int SelectedIndex
	{
		get
		{
			return _selectedIndex;
		}
		set
		{
			if (_selectedIndex != value)
			{
				int selectedIndex = _selectedIndex;
				_selectedIndex = value;
				this.SelectionChanged?.Invoke();
			}
		}
	}

	public int TotalCost
	{
		get
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			int num = 0;
			MapPointType[] nodes = Nodes;
			foreach (MapPointType type in nodes)
			{
				num += GetCost(type);
			}
			return num;
		}
	}

	public bool IsOverBudget => TotalCost > 11;

	public bool IsValid => IsRandomized || !IsOverBudget;

	public int NodeCountTotal => 6;

	public event Action? SelectionChanged;

	public event Action? NodesChanged;

	public event Action? Randomized;

	public event Action? Finished;

	public PortalMapBuilderMinigame(Player owner, Rng rng, int availableNodeCount)
	{
		_owner = owner;
		Rng = rng;
		AvailableNodeCount = Math.Clamp(availableNodeCount, 0, 6);
		Nodes = (MapPointType[])(object)new MapPointType[6];
		for (int i = 0; i < 6; i++)
		{
			Nodes[i] = (MapPointType)((i < AvailableNodeCount) ? 5 : 0);
		}
	}

	public bool IsLocked(int index)
	{
		return index >= AvailableNodeCount || IsRandomized;
	}

	public static int GetCost(MapPointType type)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		int value;
		return (!PointCosts.TryGetValue(type, out value)) ? 1 : value;
	}

	public void CycleSelectedNode(int direction)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		if (_selectedIndex >= 0 && _selectedIndex < 6 && !IsLocked(_selectedIndex))
		{
			MapPointType value = Nodes[_selectedIndex];
			int num = Array.IndexOf(AvailableTypes, value);
			num = (num + direction + AvailableTypes.Length) % AvailableTypes.Length;
			Nodes[_selectedIndex] = AvailableTypes[num];
			this.NodesChanged?.Invoke();
		}
	}

	public void SelectNode(int index)
	{
		if (!IsLocked(index))
		{
			SelectedIndex = ((index == _selectedIndex) ? (-1) : index);
		}
	}

	public void Randomize()
	{
		if (!IsRandomized)
		{
			for (int i = 0; i < AvailableNodeCount; i++)
			{
				int num = Rng.NextInt(0, RandomizableTypes.Length);
				Nodes[i] = RandomizableTypes[num];
			}
			IsRandomized = true;
			_selectedIndex = -1;
			this.Randomized?.Invoke();
			this.NodesChanged?.Invoke();
		}
	}

	public void Confirm()
	{
		if (IsValid && !_completionSource.Task.IsCompleted)
		{
			_completionSource.SetResult();
			this.Finished?.Invoke();
		}
	}

	public void ForceEnd()
	{
		if (!_completionSource.Task.IsCompleted)
		{
			_completionSource.SetCanceled();
		}
	}

	public async Task PlayMinigame()
	{
		if (LocalContext.IsMe(_owner))
		{
			NPortalMapBuilderScreen.ShowScreen(this);
			await _completionSource.Task;
		}
	}

	public MapPointType GetNodeType(int index)
	{
		return Nodes[index];
	}

	static PortalMapBuilderMinigame()
	{
		MapPointType[] array = new MapPointType[7];
		RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		AvailableTypes = (MapPointType[])(object)array;
		MapPointType[] array2 = new MapPointType[6];
		RuntimeHelpers.InitializeArray(array2, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
		RandomizableTypes = (MapPointType[])(object)array2;
		PointCosts = new Dictionary<MapPointType, int>
		{
			{
				(MapPointType)5,
				1
			},
			{
				(MapPointType)6,
				2
			},
			{
				(MapPointType)4,
				3
			},
			{
				(MapPointType)2,
				2
			},
			{
				(MapPointType)3,
				3
			},
			{
				(MapPointType)1,
				1
			},
			{
				(MapPointType)0,
				0
			}
		};
	}
}
