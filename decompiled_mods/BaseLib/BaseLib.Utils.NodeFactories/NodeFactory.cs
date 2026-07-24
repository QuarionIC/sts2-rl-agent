using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace BaseLib.Utils.NodeFactories;

public abstract class NodeFactory
{
	protected interface INodeInfo
	{
		string Path { get; }

		bool UniqueName { get; }

		bool MakeNameUnique { get; }

		bool IsValidName(Node n);

		bool IsValidType(Node node);

		bool IsValidUnique(Node n);

		Type NodeType();
	}

	protected record NodeInfo<T>(string Path, bool MakeNameUnique = true) : INodeInfo
	{
		public bool UniqueName { get; init; }

		public StringName StringName { get; init; }

		public NodeInfo(string Path, bool MakeNameUnique = true)
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Expected O, but got Unknown
			UniqueName = Path.StartsWith('%');
			string text;
			if (!Path.StartsWith('%'))
			{
				text = Path;
			}
			else
			{
				text = Path.Substring(1, Path.Length - 1);
			}
			StringName = new StringName(text);
			base._002Ector();
		}

		public bool IsValidType(Node node)
		{
			return node is T;
		}

		public bool IsValidName(Node n)
		{
			return n.Name.Equals(StringName);
		}

		public bool IsValidUnique(Node n)
		{
			if (!UniqueName)
			{
				return false;
			}
			if (n is T)
			{
				return n.Name.Equals(StringName);
			}
			return false;
		}

		public Type NodeType()
		{
			return typeof(T);
		}
	}

	protected static readonly SpireField<Node, bool> CreatedFromFactoryInternal = new SpireField<Node, bool>(() => false);

	private static readonly ConcurrentDictionary<Type, NodeFactory> _factories = new ConcurrentDictionary<Type, NodeFactory>();

	private static readonly ConcurrentDictionary<string, (Type, Action<Node>?)> _registeredScenes = new ConcurrentDictionary<string, (Type, Action<Node>)>();

	[ThreadStatic]
	private static HashSet<Node>? _convertingNodes;

	private static readonly ConcurrentDictionary<string, byte> _loggedConversions = new ConcurrentDictionary<string, byte>();

	protected readonly List<INodeInfo> NamedNodes;

	protected readonly bool FlexibleStructure;

	public static bool CreatedFromFactory(Node n)
	{
		return CreatedFromFactoryInternal.Get(n);
	}

	public static void Init()
	{
		new ControlFactory();
		new NCreatureVisualsFactory();
		new NRestSiteCharacterFactory();
		new NMerchantCharacterFactory();
		new NEnergyCounterFactory();
		new NCustomTreasureRoomChestFactory();
	}

	public static void RegisterSceneType<TNode>(string scenePath, Action<TNode>? postConversionAction = null) where TNode : Node
	{
		RegisterSceneType(scenePath, (typeof(TNode), postConversionAction));
	}

	public static void RegisterSceneType<TNode>(string scenePath, (Type, Action<TNode>?) nodeType) where TNode : Node
	{
		if (string.IsNullOrWhiteSpace(scenePath))
		{
			BaseLibMain.Logger.Warn("Ignoring RegisterSceneType(" + nodeType.Item1.Name + ") with null/empty path", 1);
			return;
		}
		if (!scenePath.StartsWith("res://") && !scenePath.StartsWith("user://") && !scenePath.StartsWith("uid://"))
		{
			BaseLibMain.Logger.Warn("Registering non-res or user path '" + scenePath + "'; assuming res path", 1);
			scenePath = "res://" + scenePath;
		}
		scenePath = StringExtensions.SimplifyPath(scenePath);
		if (_registeredScenes.TryGetValue(scenePath, out (Type, Action<Node>) value) && value.Item1 != nodeType.Item1)
		{
			BaseLibMain.Logger.Warn($"Overwriting scene registration for '{scenePath}': {value.Item1.Name} → {nodeType.Item1.Name}", 1);
		}
		_registeredScenes[scenePath] = (nodeType.Item1, nodeType.Item2 as Action<Node>);
		BaseLibMain.Logger.Info("Registered scene '" + scenePath + "' for auto-conversion to " + nodeType.Item1.Name, 1);
	}

	internal static void RegisterFactory(Type nodeType, NodeFactory factory)
	{
		_factories[nodeType] = factory;
	}

	internal static bool TryAutoConvert(PackedScene scene, ref Node? result)
	{
		if (result == null || (_convertingNodes != null && _convertingNodes.Contains(result)))
		{
			return false;
		}
		string text = StringExtensions.SimplifyPath(((Resource)scene).ResourcePath);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (!_registeredScenes.TryGetValue(text, out (Type, Action<Node>) value))
		{
			return false;
		}
		if (value.Item1.IsInstanceOfType(result))
		{
			return false;
		}
		if (!_factories.TryGetValue(value.Item1, out NodeFactory value2))
		{
			BaseLibMain.Logger.Warn($"Scene '{text}' registered for {value.Item1.Name} but no factory exists for that type", 1);
			return false;
		}
		if (_convertingNodes == null)
		{
			_convertingNodes = new HashSet<Node>();
		}
		Node item = result;
		_convertingNodes.Add(item);
		try
		{
			string name = ((object)result).GetType().Name;
			Node val = value2.CreateFromNode(result);
			if (_loggedConversions.TryAdd(text, 0))
			{
				BaseLibMain.Logger.Info($"Auto-converted '{text}' from {name} to {((object)val).GetType().Name}", 1);
			}
			value.Item2?.Invoke(val);
			result = val;
			return true;
		}
		catch (Exception value3)
		{
			BaseLibMain.Logger.Error($"Auto-conversion failed for '{text}': {value3}", 1);
			throw;
		}
		finally
		{
			_convertingNodes.Remove(item);
		}
	}

	protected abstract Node CreateFromNode(Node source);

	protected NodeFactory(IEnumerable<INodeInfo> namedNodes)
	{
		NamedNodes = namedNodes.ToList();
		FlexibleStructure = NamedNodes.All((INodeInfo info) => info.UniqueName);
	}

	protected static void CopyControlProperties(Control target, Control source)
	{
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		CopyCanvasItemProperties((CanvasItem)(object)target, (CanvasItem)(object)source);
		target.LayoutMode = source.LayoutMode;
		target.AnchorLeft = source.AnchorLeft;
		target.AnchorTop = source.AnchorTop;
		target.AnchorRight = source.AnchorRight;
		target.AnchorBottom = source.AnchorBottom;
		target.OffsetLeft = source.OffsetLeft;
		target.OffsetTop = source.OffsetTop;
		target.OffsetRight = source.OffsetRight;
		target.OffsetBottom = source.OffsetBottom;
		target.GrowHorizontal = source.GrowHorizontal;
		target.GrowVertical = source.GrowVertical;
		target.Size = source.Size;
		target.CustomMinimumSize = source.CustomMinimumSize;
		target.PivotOffset = source.PivotOffset;
		target.MouseFilter = source.MouseFilter;
		target.FocusMode = source.FocusMode;
		target.ClipContents = source.ClipContents;
	}

	protected static void CopyCanvasItemProperties(CanvasItem target, CanvasItem source)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		target.Visible = source.Visible;
		target.Modulate = source.Modulate;
		target.SelfModulate = source.SelfModulate;
		target.ShowBehindParent = source.ShowBehindParent;
		target.TopLevel = source.TopLevel;
		target.ZIndex = source.ZIndex;
		target.ZAsRelative = source.ZAsRelative;
		target.YSortEnabled = source.YSortEnabled;
		target.TextureFilter = source.TextureFilter;
		target.TextureRepeat = source.TextureRepeat;
		target.Material = source.Material;
		target.UseParentMaterial = source.UseParentMaterial;
		Node2D val = (Node2D)(object)((target is Node2D) ? target : null);
		if (val != null)
		{
			Node2D val2 = (Node2D)(object)((source is Node2D) ? source : null);
			if (val2 != null)
			{
				val.Position = val2.Position;
				val.Rotation = val2.Rotation;
				val.Scale = val2.Scale;
				val.Skew = val2.Skew;
			}
		}
	}

	protected static void SetChildrenOwner(Node target, Node child)
	{
		foreach (Node child2 in child.GetChildren(false))
		{
			child2.Owner = target;
			SetChildrenOwner(target, child2);
		}
	}
}
public abstract class NodeFactory<T> : NodeFactory where T : Node, new()
{
	private static NodeFactory<T>? _instance;

	protected NodeFactory(IEnumerable<INodeInfo> namedNodes)
		: base(namedNodes)
	{
		_instance = this;
		NodeFactory.RegisterFactory(typeof(T), this);
		BaseLibMain.Logger.Info("Created node factory for " + typeof(T).Name + ".", 1);
	}

	public static T CreateFromResource(object resource)
	{
		if (_instance == null)
		{
			throw new Exception("No node factory found for type '" + typeof(T).FullName + "'");
		}
		if (!BaseLibMain.IsMainThread)
		{
			BaseLibMain.Logger.Warn($"NodeFactory<{typeof(T)}>.CreateFromResource called while not on main thread", 1);
			throw new Exception($"NodeFactory<{typeof(T)}>.CreateFromResource called while not on main thread");
		}
		if (resource is string text && ResourceLoader.Exists(text, ""))
		{
			resource = ResourceLoader.Load(text, "", (CacheMode)1);
		}
		BaseLibMain.Logger.Info("Creating " + typeof(T).Name + " from resource " + resource.GetType().Name, 1);
		T val = _instance.CreateBareFromResource(resource);
		_instance.ConvertScene(val, null);
		NodeFactory.CreatedFromFactoryInternal[(Node)(object)val] = true;
		return val;
	}

	protected virtual T CreateBareFromResource(object resource)
	{
		throw new Exception("Node factory for " + typeof(T).Name + " does not support generation from resource type " + resource.GetType().Name);
	}

	public static T CreateFromScene(string scenePath)
	{
		if (!scenePath.EndsWith(".tscn"))
		{
			BaseLibMain.Logger.Warn($"Attempting to create {typeof(T).Name} from scene {scenePath} with unusual file extension", 1);
		}
		return CreateFromScene(PreloadManager.Cache.GetScene(scenePath));
	}

	public static T CreateFromScene(PackedScene scene)
	{
		if (_instance == null)
		{
			throw new Exception("No node factory found for type '" + typeof(T).FullName + "'");
		}
		if (!BaseLibMain.IsMainThread)
		{
			BaseLibMain.Logger.Error($"NodeFactory<{typeof(T)}>.CreateFromScene called while not on main thread", 1);
			throw new Exception($"NodeFactory<{typeof(T)}>.CreateFromScene called while not on main thread");
		}
		BaseLibMain.Logger.Info("Creating " + typeof(T).Name + " from scene " + ((Resource)scene).ResourcePath, 1);
		T val = _instance.CreateFromNode(scene.Instantiate((GenEditState)0));
		NodeFactory.CreatedFromFactoryInternal[(Node)(object)val] = true;
		return val;
	}

	protected override T CreateFromNode(Node n)
	{
		T val = (T)(object)((n is T) ? n : null);
		if (val != null)
		{
			return val;
		}
		T val2 = new T();
		ConvertScene(val2, n);
		return val2;
	}

	protected virtual void ConvertScene(T target, Node? source)
	{
		CanvasItem val2;
		if (source != null)
		{
			((Node)target).Name = source.Name;
			object obj = target;
			Control val = (Control)((obj is Control) ? obj : null);
			if (val == null)
			{
				object obj2 = target;
				val2 = (CanvasItem)((obj2 is CanvasItem) ? obj2 : null);
				if (val2 != null)
				{
					goto IL_005b;
				}
			}
			else
			{
				Control val3 = (Control)(object)((source is Control) ? source : null);
				if (val3 == null)
				{
					object obj3 = target;
					val2 = (CanvasItem)((obj3 is CanvasItem) ? obj3 : null);
					goto IL_005b;
				}
				NodeFactory.CopyControlProperties(val, val3);
			}
		}
		goto IL_006c;
		IL_006c:
		TransferAndCreateNodes(target, source);
		return;
		IL_005b:
		CanvasItem val4 = (CanvasItem)(object)((source is CanvasItem) ? source : null);
		if (val4 != null)
		{
			NodeFactory.CopyCanvasItemProperties(val2, val4);
		}
		goto IL_006c;
	}

	protected virtual void TransferAndCreateNodes(T target, Node? source)
	{
		if (source != null)
		{
			if (FlexibleStructure)
			{
				BaseLibMain.Logger.Debug("All named nodes use unique names; adding source directly as child to generated node", 1);
				((Node)target).AddChild(source, false, (InternalMode)0);
				source.Owner = (Node)(object)target;
				NodeFactory.SetChildrenOwner((Node)(object)target, source);
			}
			else
			{
				BaseLibMain.Logger.Debug("Transferring all nodes from source to generated node and freeing source", 1);
				foreach (Node child in source.GetChildren(false))
				{
					source.RemoveChild(child);
					((Node)target).AddChild(child, false, (InternalMode)0);
					child.Owner = (Node)(object)target;
					NodeFactory.SetChildrenOwner((Node)(object)target, child);
				}
				source.QueueFree();
			}
		}
		List<INodeInfo> list = new List<INodeInfo>();
		foreach (INodeInfo namedNode in NamedNodes)
		{
			if (namedNode.UniqueName)
			{
				list.Add(namedNode);
				continue;
			}
			Node val = ((Node)target).GetNodeOrNull(NodePath.op_Implicit(namedNode.Path));
			if (val != null)
			{
				if (!namedNode.IsValidType(val))
				{
					val = ConvertNodeType(val, namedNode.NodeType());
				}
				if (namedNode.MakeNameUnique)
				{
					val.UniqueNameInOwner = true;
					val.Owner = (Node)(object)target;
				}
			}
			else
			{
				GenerateNode((Node)(object)target, namedNode);
			}
		}
		Dictionary<INodeInfo, Node> dictionary = new Dictionary<INodeInfo, Node>();
		foreach (Node item in NodeUtil.GetChildrenRecursive<Node>((Node)(object)target))
		{
			for (int i = 0; i < list.Count; i++)
			{
				INodeInfo nodeInfo = list[i];
				if (nodeInfo.IsValidName(item))
				{
					dictionary[nodeInfo] = item;
				}
				if (nodeInfo.IsValidUnique(item))
				{
					item.UniqueNameInOwner = true;
					item.Owner = (Node)(object)target;
					list.Remove(nodeInfo);
					break;
				}
			}
		}
		foreach (INodeInfo item2 in list)
		{
			if (dictionary.TryGetValue(item2, out var value))
			{
				if (!item2.IsValidType(value))
				{
					value = ConvertNodeType(value, item2.NodeType());
				}
				value.UniqueNameInOwner = true;
				value.Owner = (Node)(object)target;
			}
			else
			{
				GenerateNode((Node)(object)target, item2);
			}
		}
	}

	protected virtual Node ConvertNodeType(Node node, Type targetType)
	{
		throw new InvalidOperationException($"Node factory for {typeof(T).Name} does not support conversion of {((object)node).GetType().Name} '{node.Name}' to {targetType.Name}");
	}

	protected abstract void GenerateNode(Node target, INodeInfo required);
}
