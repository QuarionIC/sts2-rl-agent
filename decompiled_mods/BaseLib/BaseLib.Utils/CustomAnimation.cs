using System;
using System.Collections;
using System.Linq;
using Godot;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;

namespace BaseLib.Utils;

public static class CustomAnimation
{
	private static readonly SpireField<Node, (Node?, Func<string[], bool?>)> _animHandler = new SpireField<Node, (Node, Func<string[], bool?>)>(delegate(Node root)
	{
		AnimationTree? obj = FindNode<AnimationTree>(root, (string?)null);
		(Node, Func<string[], bool?>)? tuple = ((Node, Func<string[], bool?>)?)((obj != null) ? ((ValueType)obj.UseAnimationTree()) : ((ValueType)((Node, Func<string[], bool?>)?)null));
		if (!tuple.HasValue)
		{
			AnimationTree? obj2 = SearchRecursive<AnimationTree>(root);
			(Node, Func<string[], bool?>)? tuple2 = ((Node, Func<string[], bool?>)?)((obj2 != null) ? ((ValueType)obj2.UseAnimationTree()) : ((ValueType)((Node, Func<string[], bool?>)?)null));
			if (!tuple2.HasValue)
			{
				AnimationPlayer? obj3 = FindNode<AnimationPlayer>(root, (string?)null);
				if (obj3 == null)
				{
					AnimatedSprite2D? obj4 = FindNode<AnimatedSprite2D>(root, (string?)null);
					if (obj4 == null)
					{
						AnimationPlayer? obj5 = SearchRecursive<AnimationPlayer>(root);
						if (obj5 == null)
						{
							AnimatedSprite2D? obj6 = SearchRecursive<AnimatedSprite2D>(root);
							if (obj6 == null)
							{
								return ((Node?, Func<string[], bool?>))(null, NoAnimation);
							}
							return obj6.UseAnimatedSprite2D();
						}
						return obj5.UseAnimationPlayer();
					}
					return obj4.UseAnimatedSprite2D();
				}
				return obj3.UseAnimationPlayer();
			}
			return tuple2.GetValueOrDefault();
		}
		return tuple.GetValueOrDefault();
	});

	private static bool? NoAnimation(string[] _)
	{
		return null;
	}

	public static bool HasCustomAnimation(Node visualRoot)
	{
		return _animHandler[visualRoot].Item1 != null;
	}

	public static bool PlayCustomAnimation(Node n, params string[] tryAnimNames)
	{
		(Node, Func<string[], bool?>) tuple = _animHandler[n];
		if (tuple.Item1 != null && !NodeUtil.IsValid(tuple.Item1))
		{
			BaseLibMain.Logger.Debug("Rechecking for Godot animation player", 1);
			SpireField<Node, (Node?, Func<string[], bool?>)> animHandler = _animHandler;
			AnimationTree? obj = FindNode<AnimationTree>(n, (string?)null);
			(Node, Func<string[], bool?>)? tuple2 = ((Node, Func<string[], bool?>)?)((obj != null) ? ((ValueType)obj.UseAnimationTree()) : ((ValueType)((Node, Func<string[], bool?>)?)null));
			(Node, Func<string[], bool?>) value;
			if (!tuple2.HasValue)
			{
				AnimationTree? obj2 = SearchRecursive<AnimationTree>(n);
				(Node, Func<string[], bool?>)? tuple3 = ((Node, Func<string[], bool?>)?)((obj2 != null) ? ((ValueType)obj2.UseAnimationTree()) : ((ValueType)((Node, Func<string[], bool?>)?)null));
				if (!tuple3.HasValue)
				{
					AnimationPlayer? obj3 = FindNode<AnimationPlayer>(n, (string?)null);
					if (obj3 == null)
					{
						AnimatedSprite2D? obj4 = FindNode<AnimatedSprite2D>(n, (string?)null);
						if (obj4 == null)
						{
							AnimationPlayer? obj5 = SearchRecursive<AnimationPlayer>(n);
							if (obj5 == null)
							{
								AnimatedSprite2D? obj6 = SearchRecursive<AnimatedSprite2D>(n);
								value = ((obj6 != null) ? obj6.UseAnimatedSprite2D() : (null, NoAnimation));
							}
							else
							{
								value = obj5.UseAnimationPlayer();
							}
						}
						else
						{
							value = obj4.UseAnimatedSprite2D();
						}
					}
					else
					{
						value = obj3.UseAnimationPlayer();
					}
				}
				else
				{
					value = tuple3.GetValueOrDefault();
				}
			}
			else
			{
				value = tuple2.GetValueOrDefault();
			}
			animHandler[n] = value;
		}
		return _animHandler[n].Item2(tryAnimNames).HasValue;
	}

	private static (Node, Func<string[], bool?>)? UseAnimationTree(this AnimationTree animationTree)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		if (!(animationTree.TreeRoot is AnimationNodeStateMachine))
		{
			BaseLibMain.Logger.Error("BaseLib only supports AnimationTree using AnimationNodeStateMachine as tree root", 1);
			return null;
		}
		AnimationNodeStateMachinePlayback stateMachine = (AnimationNodeStateMachinePlayback)(GodotObject)((GodotObject)animationTree).Get(StringName.op_Implicit("parameters/playback"));
		return ((Node)(object)animationTree, delegate(string[] animNames)
		{
			foreach (string text in animNames)
			{
				BaseLibMain.Logger.Debug("Checking for animation " + text, 1);
				if (((AnimationMixer)animationTree).HasAnimation(StringName.op_Implicit(text)))
				{
					stateMachine.Travel(StringName.op_Implicit(text), true);
					return true;
				}
			}
			BaseLibMain.Logger.Debug("Animations not found: " + GD.Stringify(animNames), 1);
			return false;
		});
	}

	private static (Node, Func<string[], bool?>) UseAnimationPlayer(this AnimationPlayer animPlayer)
	{
		return ((Node)(object)animPlayer, delegate(string[] animNames)
		{
			foreach (string text in animNames)
			{
				BaseLibMain.Logger.Debug("Checking for animation " + text, 1);
				if (((AnimationMixer)animPlayer).HasAnimation(StringName.op_Implicit(text)))
				{
					if (animPlayer.CurrentAnimation.Equals(text))
					{
						animPlayer.Stop(false);
					}
					animPlayer.Play(StringName.op_Implicit(text), -1.0, 1f, false);
					return true;
				}
			}
			BaseLibMain.Logger.Debug("Animations not found: " + GD.Stringify(animNames), 1);
			return false;
		});
	}

	private static (Node, Func<string[], bool?>) UseAnimatedSprite2D(this AnimatedSprite2D animSprite)
	{
		return ((Node)(object)animSprite, delegate(string[] animNames)
		{
			foreach (string text in animNames)
			{
				BaseLibMain.Logger.Debug("Checking for animation " + text, 1);
				if (animSprite.SpriteFrames.HasAnimation(StringName.op_Implicit(text)))
				{
					animSprite.Play(StringName.op_Implicit(text), 1f, false);
					return true;
				}
			}
			BaseLibMain.Logger.Debug("Animations not found: " + GD.Stringify(animNames), 1);
			return false;
		});
	}

	private static T? FindNode<T>(Node root, string? name = null) where T : Node
	{
		T val = ((IEnumerable)root.GetChildren(false)).OfType<T>().FirstOrDefault();
		if (val != null)
		{
			BaseLibMain.Logger.Debug("Found " + typeof(T).Name, 1);
			return val;
		}
		if (name == null)
		{
			name = "T";
		}
		Node obj = root.GetNodeOrNull(NodePath.op_Implicit(name)) ?? root.GetNodeOrNull(NodePath.op_Implicit("Visuals/" + name)) ?? root.GetNodeOrNull(NodePath.op_Implicit("Body/" + name));
		val = (T)(object)((obj is T) ? obj : null);
		if (val != null)
		{
			BaseLibMain.Logger.Debug("Found " + typeof(T).Name, 1);
		}
		return val;
	}

	private static T? SearchRecursive<T>(Node parent) where T : Node
	{
		foreach (Node child in parent.GetChildren(false))
		{
			T val = (T)(object)((child is T) ? child : null);
			if (val != null)
			{
				return val;
			}
			T val2 = SearchRecursive<T>(child);
			if (val2 != null)
			{
				BaseLibMain.Logger.Debug("Found " + typeof(T).Name + " with recursive search", 1);
				return val2;
			}
		}
		return default(T);
	}
}
