using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Nodes.Pooling;

namespace BaseLib.Utils;

public class GeneratedNodePool
{
	private static Dictionary<Type, INodePool>? _pools;

	internal static readonly Variant NameStr = Variant.CreateFrom("name");

	internal static readonly Variant CallableStr = Variant.CreateFrom("callable");

	internal static readonly Variant SignalStr = Variant.CreateFrom("signal");

	public static GeneratedNodePool<T> Init<T>(Func<T> constructor, int prewarmCount) where T : Node, IPoolable
	{
		Type typeFromHandle = typeof(T);
		if (_pools == null)
		{
			_pools = (Dictionary<Type, INodePool>)AccessTools.DeclaredField(typeof(NodePool), "_pools").GetValue(null);
		}
		if (_pools == null)
		{
			throw new Exception("Failed to access _pools from NodePool");
		}
		if (_pools.TryGetValue(typeFromHandle, out INodePool _))
		{
			throw new InvalidOperationException($"Tried to init GeneratedNodePool for type {typeof(T)} but it's already initialized!");
		}
		GeneratedNodePool<T> generatedNodePool = new GeneratedNodePool<T>(constructor, prewarmCount);
		_pools[typeFromHandle] = (INodePool)(object)generatedNodePool;
		return generatedNodePool;
	}
}
public class GeneratedNodePool<T> : INodePool where T : Node, IPoolable
{
	private readonly Func<T> _constructor;

	private readonly List<T> _freeObjects = new List<T>();

	private readonly HashSet<T> _usedObjects = new HashSet<T>();

	public IReadOnlyList<T> DebugFreeObjects => _freeObjects;

	public GeneratedNodePool(Func<T> constructor, int prewarmCount = 0)
	{
		_constructor = constructor;
		for (int i = 0; i < prewarmCount; i++)
		{
			_freeObjects.Add(Instantiate());
		}
	}

	IPoolable INodePool.Get()
	{
		return (IPoolable)(object)Get();
	}

	void INodePool.Free(IPoolable poolable)
	{
		Free((T)(object)poolable);
	}

	public T Get()
	{
		T val;
		if (_freeObjects.Count > 0)
		{
			List<T> freeObjects = _freeObjects;
			val = freeObjects[freeObjects.Count - 1];
			_freeObjects.RemoveAt(_freeObjects.Count - 1);
		}
		else
		{
			val = Instantiate();
		}
		_usedObjects.Add(val);
		((IPoolable)val).OnReturnedFromPool();
		return val;
	}

	public void Free(T obj)
	{
		if (!_usedObjects.Contains(obj))
		{
			if (_freeObjects.Contains(obj))
			{
				Log.Error($"Tried to free object {obj} ({((object)obj).GetType()}) back to pool {typeof(GeneratedNodePool<T>)} but it's already been freed!", 2);
			}
			else
			{
				Log.Error($"Tried to free object {obj} ({((object)obj).GetType()}) back to pool {typeof(GeneratedNodePool<T>)} but it's not part of the pool!", 2);
			}
			GodotTreeExtensions.QueueFreeSafelyNoPool((Node)(object)obj);
		}
		else
		{
			DisconnectIncomingAndOutgoingSignals((Node)(object)obj);
			_usedObjects.Remove(obj);
			_freeObjects.Add(obj);
			((IPoolable)obj).OnFreedToPool();
		}
	}

	private T Instantiate()
	{
		T val = _constructor();
		((IPoolable)val).OnInstantiated();
		return val;
	}

	private void DisconnectIncomingAndOutgoingSignals(Node obj)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		Variant val;
		foreach (Dictionary signal3 in ((GodotObject)obj).GetSignalList())
		{
			val = signal3[GeneratedNodePool.NameStr];
			StringName val2 = ((Variant)(ref val)).AsStringName();
			foreach (Dictionary signalConnection in ((GodotObject)obj).GetSignalConnectionList(val2))
			{
				val = signalConnection[GeneratedNodePool.CallableStr];
				Callable callable = ((Variant)(ref val)).AsCallable();
				val = signalConnection[GeneratedNodePool.SignalStr];
				Signal signal = ((Variant)(ref val)).AsSignal();
				DisconnectSignal(callable, signal);
			}
		}
		foreach (Dictionary incomingConnection in ((GodotObject)obj).GetIncomingConnections())
		{
			val = incomingConnection[GeneratedNodePool.CallableStr];
			Callable callable2 = ((Variant)(ref val)).AsCallable();
			val = incomingConnection[GeneratedNodePool.SignalStr];
			Signal signal2 = ((Variant)(ref val)).AsSignal();
			DisconnectSignal(callable2, signal2);
		}
		for (int i = 0; i < obj.GetChildCount(false); i++)
		{
			DisconnectIncomingAndOutgoingSignals(obj.GetChild(i, false));
		}
	}

	private void DisconnectSignal(Callable callable, Signal signal)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		GodotObject target = ((Callable)(ref callable)).Target;
		if (target == null && ((Callable)(ref callable)).Method == (StringName)null)
		{
			return;
		}
		StringName name = ((Signal)(ref signal)).Name;
		Node val = (Node)(object)((target is Node) ? target : null);
		if (val == null || val.IsInsideTree())
		{
			GodotObject owner = ((Signal)(ref signal)).Owner;
			Node val2 = (Node)(object)((owner is Node) ? owner : null);
			if (val != null && ((GodotObject)val).HasSignal(name) && ((GodotObject)val).IsConnected(name, callable))
			{
				((GodotObject)val).Disconnect(name, callable);
			}
			else if (val2 != null && ((GodotObject)val2).HasSignal(name) && ((GodotObject)val2).IsConnected(name, callable))
			{
				((GodotObject)val2).Disconnect(name, callable);
			}
		}
	}
}
