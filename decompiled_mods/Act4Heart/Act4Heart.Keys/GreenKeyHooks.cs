using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Act4Heart.Powers;
using Dolso;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace Act4Heart.Keys;

[Hook]
internal class GreenKeyHooks : ModelHook
{
	private class SuperEliteQuest : AbstractModel
	{
		public override bool ShouldReceiveCombatHooks => false;
	}

	internal static bool IsPointMarked(MapPoint? point)
	{
		if (point == null || point.Quests.Count == 0)
		{
			return false;
		}
		foreach (AbstractModel quest in point.Quests)
		{
			if (quest is SuperEliteQuest)
			{
				return true;
			}
		}
		return false;
	}

	[Hook]
	private static void Init()
	{
		ModelHook.Register<GreenKeyHooks>();
	}

	public override ActMap ModifyGeneratedMapLate(IRunState runState, ActMap map, int actIndex)
	{
		if (!ModMain.current_config.keys_enable)
		{
			return map;
		}
		try
		{
			MarkSuperElite(runState, map, actIndex);
		}
		catch (Exception data)
		{
			log.error(data);
		}
		return map;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void MarkSuperElite(IRunState run_state, ActMap map, int act_index)
	{
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Invalid comparison between Unknown and I4
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Invalid comparison between Unknown and I4
		if (act_index > 2 || KeyRelicModel.EveryoneHasKey<EmeraldKey>(run_state))
		{
			return;
		}
		List<MapPoint> list = new List<MapPoint>();
		MapPoint[,] grid = map.Grid;
		foreach (MapPoint val in grid)
		{
			if (val == null)
			{
				continue;
			}
			if ((int)val.PointType == 6)
			{
				list.Add(val);
			}
			if (val.Quests.Count <= 0)
			{
				continue;
			}
			foreach (AbstractModel quest in val.Quests)
			{
				if (quest is SuperEliteQuest)
				{
					if ((int)val.PointType == 6)
					{
						log.info("map already contained super elite point");
						return;
					}
					log.warning("non-elite map node had super elite quest");
					val.RemoveQuest(quest);
					break;
				}
			}
		}
		if (list.Count != 0)
		{
			int index = new Rng(run_state.Rng.Seed + (uint)act_index, "se_coord").NextInt(0, list.Count);
			list[index].AddQuest(((AbstractModel)ModelDb.Get<SuperEliteQuest>()).MutableClone());
		}
	}

	public override Task BeforeCombatStart()
	{
		if (!ModMain.current_config.keys_enable)
		{
			return Task.CompletedTask;
		}
		try
		{
			return DoSuperEliteBuff();
		}
		catch (Exception data)
		{
			log.error(data);
		}
		return Task.CompletedTask;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static async Task DoSuperEliteBuff()
	{
		RunState val = ModelHook.run_state;
		CombatState combat_state = ModelHook.combat_state;
		int num;
		if (val == null)
		{
			num = 1;
		}
		else
		{
			MapPoint currentMapPoint = val.CurrentMapPoint;
			num = (((int)((currentMapPoint != null) ? new MapPointType?(currentMapPoint.PointType) : ((MapPointType?)null)).GetValueOrDefault() != 6) ? 1 : 0);
		}
		if (num != 0 || combat_state == null || !IsPointMarked(val.CurrentMapPoint))
		{
			return;
		}
		int num2 = 1 + val.CurrentActIndex;
		Config current_config = ModMain.current_config;
		int num3 = new Rng(val.Rng.Seed + (uint)num2, "se_buff").NextInt(0, 4);
		PowerModel power;
		decimal amount;
		switch (num3)
		{
		case 0:
			power = (PowerModel)(object)ModelDb.Power<StrengthPower>();
			amount = current_config.super_elite_strength.GetAmount(num2);
			break;
		case 1:
			power = (PowerModel)(object)ModelDb.Power<MetallicizePowerA4h>();
			amount = current_config.super_elite_metallicize.GetAmount(num2);
			break;
		case 2:
			power = (PowerModel)(object)ModelDb.Power<RegeneratePowerA4h>();
			amount = current_config.super_elite_regenerate.GetAmount(num2);
			break;
		case 3:
			power = null;
			amount = current_config.super_elite_maxhp_percent;
			break;
		default:
			log.error($"se rng rolled {num3}, out of bounds");
			goto case 0;
		}
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(27, 2);
		defaultInterpolatedStringHandler.AppendLiteral("Applying super elite buff ");
		PowerModel obj = power;
		defaultInterpolatedStringHandler.AppendFormatted(((obj != null) ? ((AbstractModel)obj).Id.Entry : null) ?? "MAX_HP");
		defaultInterpolatedStringHandler.AppendLiteral(" ");
		defaultInterpolatedStringHandler.AppendFormatted(amount);
		log.info(defaultInterpolatedStringHandler.ToStringAndClear());
		for (int i = 0; i < combat_state.Enemies.Count; i++)
		{
			Creature val2 = combat_state.Enemies[i];
			if (power != null)
			{
				await PowerCmd.Apply((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), power.ToMutable(0), val2, amount, (Creature)null, (CardModel)null, false);
			}
			else
			{
				await CreatureCmd.GainMaxHp(val2, (decimal)val2.MaxHp * amount / 100m);
			}
		}
	}

	public override bool TryModifyRewards(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		if (!ModMain.current_config.keys_enable)
		{
			return false;
		}
		try
		{
			return AddGreenKeyReward(player, rewards, room);
		}
		catch (Exception data)
		{
			log.error(data);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool AddGreenKeyReward(Player player, List<Reward> rewards, AbstractRoom? room)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Invalid comparison between Unknown and I4
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		if (room != null && (int)room.RoomType == 2 && player.GetRelic<EmeraldKey>() == null)
		{
			RunState? obj = ModelHook.run_state;
			if (IsPointMarked((obj != null) ? obj.CurrentMapPoint : null))
			{
				rewards.Add((Reward)new RelicReward(((RelicModel)ModelDb.Relic<EmeraldKey>()).ToMutable(), player));
				return true;
			}
		}
		return false;
	}

	[HookBefore(typeof(NNormalMapPoint), "RefreshMarkedIconVisibility")]
	private static bool UseSeMapEffect_RefreshMarkedIconVisibility(NNormalMapPoint __instance)
	{
		try
		{
			return UseSeMapEffect(__instance);
		}
		catch (Exception data)
		{
			log.error(data);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static bool UseSeMapEffect(NNormalMapPoint self)
	{
		Node icon = (Node)(object)self._icon;
		if (IsPointMarked(((NMapPoint)self).Point))
		{
			if (icon.GetNodeOrNull(NodePath.op_Implicit("SuperEliteNode")) == null)
			{
				NSuperElitePoint nSuperElitePoint = new NSuperElitePoint(self);
				((Node)nSuperElitePoint).Name = StringName.op_Implicit("SuperEliteNode");
				icon.AddChild((Node)(object)nSuperElitePoint, false, (InternalMode)0);
			}
			if (((NMapPoint)self).Point.Quests.Count == 1)
			{
				((CanvasItem)self._questIcon).Visible = false;
				return false;
			}
		}
		return true;
	}
}
