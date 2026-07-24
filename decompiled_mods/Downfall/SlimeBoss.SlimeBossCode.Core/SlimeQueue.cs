using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Core;

public static class SlimeQueue
{
	private static readonly SpireField<Player, int> SlimeSlots = new SpireField<Player, int>((Func<Player, int>)((Player _) => 1));

	private static List<Creature> GetSlimes(Player player)
	{
		PlayerCombatState playerCombatState = player.PlayerCombatState;
		return ((playerCombatState != null) ? playerCombatState.Pets.Where((Creature e) => e.Monster is SlimeModel).ToList() : null) ?? new List<Creature>();
	}

	public static void ResetAllSlots()
	{
		SlimeSlots._table.Clear();
	}

	public static void SetSlots(Player player, int amount)
	{
		SlimeSlots[player] = amount;
	}

	public static int GetCount(Player player)
	{
		PlayerCombatState playerCombatState = player.PlayerCombatState;
		if (playerCombatState == null)
		{
			return 0;
		}
		return playerCombatState.Pets.Count((Creature e) => e.Monster is SlimeModel);
	}

	public static Task IncreaseSlimeSlots(Player player, int amount)
	{
		SpireField<Player, int> slimeSlots = SlimeSlots;
		slimeSlots[player] += amount;
		return Task.CompletedTask;
	}

	public static async Task<(int actual, int absorbed)> DecreaseSlimeSlots(Player player, int amount = 1)
	{
		if (SlimeSlots[player] <= 0)
		{
			return (actual: 0, absorbed: 0);
		}
		int actual = Math.Min(amount, SlimeSlots[player]);
		SpireField<Player, int> slimeSlots = SlimeSlots;
		Player val = player;
		slimeSlots[val] -= actual;
		int item = await EvictSlimesDownTo(player, GetSlimes(player), SlimeSlots[player]);
		Callable val2 = Callable.From((Action)delegate
		{
			RearrangeSlimeOrbRow(player);
		});
		((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
		return (actual: actual, absorbed: item);
	}

	public static async Task<(bool added, int absorbed)> AddSlime(Player player, SlimeModel slimeModel)
	{
		int num = SlimeSlots[player];
		if (num == 0)
		{
			return (added: false, absorbed: 0);
		}
		List<Creature> slimes = GetSlimes(player);
		int absorbed = await EvictSlimesDownTo(player, slimes, num - 1);
		ICombatState combatState = player.Creature.CombatState;
		Creature val = ((combatState != null) ? combatState.CreateCreature(((MonsterModel)slimeModel).ToMutable(), player.Creature.Side, (string)null) : null);
		if (val == null)
		{
			return (added: false, absorbed: absorbed);
		}
		await PlayerCmd.AddPet(val, player);
		Callable val2 = Callable.From((Action)delegate
		{
			RearrangeSlimeOrbRow(player);
		});
		((Callable)(ref val2)).CallDeferred(Array.Empty<Variant>());
		return (added: true, absorbed: absorbed);
	}

	private static async Task<int> EvictSlimesDownTo(Player player, List<Creature> slimes, int maxSlots)
	{
		int evicted = 0;
		while (slimes.Count > maxSlots)
		{
			Creature oldest = slimes[0];
			slimes.RemoveAt(0);
			if (oldest.IsAlive)
			{
				await CreatureCmd.Kill(oldest, false);
				player.PlayerCombatState?._pets.Remove(oldest);
				evicted++;
			}
		}
		return evicted;
	}

	public static Task<(bool added, int evicted)> AddSlime<T>(Player player) where T : SlimeModel
	{
		return AddSlime(player, ModelDb.Monster<T>());
	}

	public static async Task<bool> RemoveLeadingSlime(Player player)
	{
		List<Creature> slimes = GetSlimes(player);
		if (slimes.Count == 0)
		{
			return false;
		}
		Creature leading = slimes[slimes.Count - 1];
		if (!leading.IsAlive)
		{
			return false;
		}
		await CreatureCmd.Kill(leading, false);
		player.PlayerCombatState?._pets.Remove(leading);
		Callable val = Callable.From((Action)delegate
		{
			RearrangeSlimeOrbRow(player);
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		return true;
	}

	public static async Task<int> RemoveAll(Player player)
	{
		List<Creature> slimes = GetSlimes(player);
		if (slimes.Count == 0)
		{
			return 0;
		}
		int amount = 0;
		foreach (Creature slime in slimes.Where((Creature val2) => val2.IsAlive))
		{
			await CreatureCmd.Kill(slime, false);
			player.PlayerCombatState?._pets.Remove(slime);
			amount++;
		}
		Callable val = Callable.From((Action)delegate
		{
			RearrangeSlimeOrbRow(player);
		});
		((Callable)(ref val)).CallDeferred(Array.Empty<Variant>());
		return amount;
	}

	private static void RearrangeSlimeOrbRow(Player player)
	{
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0178: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Invalid comparison between Unknown and I4
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_0197: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_026e: Unknown result type (might be due to invalid IL or missing references)
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_028d: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(player.Creature) : null);
		if (val == null)
		{
			return;
		}
		List<Creature> list = player.Creature.Pets.Where((Creature e) => e.Monster is SlimeModel).ToList();
		int count = list.Count;
		if (count == 0)
		{
			return;
		}
		Vector2 p = default(Vector2);
		((Vector2)(ref p))._002Ector(300f, -50f);
		Vector2 val2 = default(Vector2);
		((Vector2)(ref val2))._002Ector(-150f, 200f);
		Vector2 val3 = default(Vector2);
		((Vector2)(ref val3))._002Ector(400f, 150f);
		float num = ((Vector2)(ref p)).DistanceTo(val3) + ((Vector2)(ref val3)).DistanceTo(val2);
		float num2 = 300f / num;
		float num3;
		if ((num3 = (float)(count - 1) * num2) > 1f && count > 1)
		{
			num3 = 1f;
		}
		for (int num4 = 0; num4 < count; num4++)
		{
			Creature val4 = list[num4];
			NCombatRoom instance2 = NCombatRoom.Instance;
			NCreature slimeNode = ((instance2 != null) ? instance2.GetCreatureNode(val4) : null);
			NCreature obj = slimeNode;
			if (obj != null)
			{
				obj.ToggleIsInteractable(true);
			}
			if (slimeNode != null)
			{
				HideHealthBar(slimeNode);
				int num5 = count - 1 - num4;
				float num6 = ((count == 1) ? 0f : Mathf.Lerp(0f, num3, (float)num5 / (float)(count - 1)));
				num6 = Mathf.Clamp(num6, 0f, 1f);
				Vector2 val5 = CalculateQuadraticBezier(p, val3, val2, num6);
				if ((int)player.Creature.Side == 2)
				{
					val5.X = 0f - val5.X;
				}
				Vector2 val6 = ((Control)val).GlobalPosition + val5;
				Node parent = ((Node)slimeNode).GetParent();
				Node2D val7 = (Node2D)(object)((parent is Node2D) ? parent : null);
				Vector2 position = ((val7 != null) ? val7.ToLocal(val6) : val6);
				_ = ((Control)slimeNode).Position;
				if (!((GodotObject)slimeNode).HasMeta(StringName.op_Implicit("layout_tween")))
				{
					((Control)slimeNode).Position = position;
					slimeNode.UpdateBounds((Node)(object)slimeNode.Visuals);
				}
				if (!((GodotObject)slimeNode).HasMeta(StringName.op_Implicit("layout_tween")))
				{
					((Control)slimeNode).GlobalPosition = val6;
					slimeNode.UpdateBounds((Node)(object)slimeNode.Visuals);
				}
				Tween val8 = ((Node)slimeNode).CreateTween();
				((GodotObject)slimeNode).SetMeta(StringName.op_Implicit("layout_tween"), Variant.op_Implicit((GodotObject)(object)val8));
				val8.TweenProperty((GodotObject)(object)slimeNode, NodePath.op_Implicit("global_position"), Variant.op_Implicit(val6), 0.3499999940395355).From(Variant.op_Implicit(((Control)slimeNode).GlobalPosition)).SetEase((EaseType)1)
					.SetTrans((TransitionType)7);
				val8.Parallel().TweenCallback(Callable.From((Action)delegate
				{
					slimeNode.UpdateBounds((Node)(object)slimeNode.Visuals);
				}));
			}
		}
	}

	private static void HideHealthBar(NCreature slimeNode)
	{
		((CanvasItem)slimeNode._stateDisplay._healthBar).Visible = false;
	}

	private static Vector2 CalculateQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		float num = 1f - t;
		float num2 = t * t;
		return num * num * p0 + 2f * num * t * p1 + num2 * p2;
	}
}
