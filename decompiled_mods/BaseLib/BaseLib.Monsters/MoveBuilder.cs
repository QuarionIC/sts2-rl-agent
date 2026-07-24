using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Audio;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace BaseLib.Monsters;

public class MoveBuilder
{
	public enum PowerIntent
	{
		None,
		Buff,
		Debuff,
		StrongDebuff
	}

	private class ActionExecutor(List<Func<IReadOnlyList<Creature>, Task>> actions)
	{
		private List<Func<IReadOnlyList<Creature>, Task>> Actions { get; } = actions;

		public static implicit operator Func<IReadOnlyList<Creature>, Task>(ActionExecutor executor)
		{
			return async delegate(IReadOnlyList<Creature> creatures)
			{
				foreach (Func<IReadOnlyList<Creature>, Task> action in executor.Actions)
				{
					await action(creatures);
				}
			};
		}
	}

	public readonly MonsterModel Monster;

	public readonly string Id;

	public readonly List<Func<IReadOnlyList<Creature>, Task>> Actions = new List<Func<IReadOnlyList<Creature>, Task>>();

	public readonly List<AbstractIntent> Intents = new List<AbstractIntent>();

	public string? FollowUpStateId { get; set; }

	private void AddNewIntent<T>() where T : AbstractIntent, new()
	{
		if (!Intents.Any((AbstractIntent intent) => intent is T))
		{
			Intents.Add((AbstractIntent)(object)new T());
		}
	}

	private void AddDebuffIntent(bool strong)
	{
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Expected O, but got Unknown
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Expected O, but got Unknown
		int num = Intents.FindIndex(delegate(AbstractIntent intent)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			IntentType intentType = intent.IntentType;
			return intentType - 2 <= 1;
		});
		if (num >= 0)
		{
			if ((int)Intents[num].IntentType != 3 && strong)
			{
				Intents[num] = (AbstractIntent)new DebuffIntent(strong);
			}
		}
		else
		{
			Intents.Add((AbstractIntent)new DebuffIntent(strong));
		}
	}

	public MoveBuilder(MonsterModel monster, string id)
	{
		Monster = monster;
		Id = id;
	}

	public MoveBuilder Attack(int damage, int hitCount = 1, (string, float, bool)? attackerAnim = null, string? attackerVfx = null, string? attackerSfx = null, string? attackerTmpSfx = null, string? hitVfx = null, string? hitSfx = null, string? hitTmpSfx = null)
	{
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Expected O, but got Unknown
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_008e: Expected O, but got Unknown
		Actions.Add(async delegate
		{
			AttackCommand val = MonsterActions.Attack(Monster, damage, hitCount).WithAttackerFx(attackerVfx, attackerSfx, attackerTmpSfx).WithHitFx(hitVfx, hitSfx, hitTmpSfx);
			if (attackerAnim.HasValue)
			{
				val.WithAttackerAnim(attackerAnim.Value.Item1, attackerAnim.Value.Item2, (Creature)null);
				if (!attackerAnim.Value.Item3)
				{
					val.OnlyPlayAnimOnce();
				}
			}
			await val.Execute((PlayerChoiceContext)null);
		});
		if (hitCount != 1)
		{
			Intents.Add((AbstractIntent)new MultiAttackIntent(damage, hitCount));
		}
		else
		{
			Intents.Add((AbstractIntent)new SingleAttackIntent(damage));
		}
		return this;
	}

	public MoveBuilder Block(int amount, ValueProp props = (ValueProp)8)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		Actions.Add(async delegate
		{
			await CreatureCmd.GainBlock(Monster.Creature, (decimal)amount, props, (CardPlay)null, false);
		});
		AddNewIntent<DefendIntent>();
		return this;
	}

	public MoveBuilder ApplyToPlayers<T>(int amount, bool isStrongDebuff, bool silent = false) where T : PowerModel
	{
		Actions.Add(async delegate(IReadOnlyList<Creature> creatures)
		{
			await MonsterActions.Apply<T>(Monster, amount, creatures, null, silent);
		});
		AddDebuffIntent(isStrongDebuff);
		return this;
	}

	public MoveBuilder ApplyToSelf<T>(int amount, bool silent = false) where T : PowerModel
	{
		Actions.Add(async delegate
		{
			await MonsterActions.ApplySelf<T>(Monster, amount, null, silent);
		});
		AddNewIntent<BuffIntent>();
		return this;
	}

	public MoveBuilder ApplyToSomeone<T>(int amount, Func<IEnumerable<Creature>> targets, PowerIntent intent = PowerIntent.None, bool silent = false) where T : PowerModel
	{
		Actions.Add(async delegate
		{
			await MonsterActions.Apply<T>(Monster, amount, targets(), null, silent);
		});
		switch (intent)
		{
		case PowerIntent.Buff:
			AddNewIntent<BuffIntent>();
			break;
		case PowerIntent.Debuff:
			AddDebuffIntent(strong: false);
			break;
		case PowerIntent.StrongDebuff:
			AddDebuffIntent(strong: true);
			break;
		}
		return this;
	}

	public MoveBuilder HealSelf(int amount, bool autoScaleWithPlayers = true)
	{
		return HealSelf(() => amount * ((!autoScaleWithPlayers) ? 1 : Monster.Creature.CombatState.Players.Count));
	}

	public MoveBuilder HealSelf(Func<int> amount)
	{
		Actions.Add(async delegate
		{
			await CreatureCmd.Heal(Monster.Creature, (decimal)amount(), true);
		});
		AddNewIntent<HealIntent>();
		return this;
	}

	public MoveBuilder PlaySfx(string key)
	{
		Actions.Add(delegate
		{
			SfxCmd.Play(key, 1f);
			return Task.CompletedTask;
		});
		return this;
	}

	public MoveBuilder PlaySfx(ModSound sound, float volumeAdd = 0f, float volumeMult = 1f, float pitchVariation = 0f, float basePitch = 1f)
	{
		Actions.Add(delegate
		{
			sound.Play(volumeAdd, volumeMult, pitchVariation, basePitch);
			return Task.CompletedTask;
		});
		return this;
	}

	public MoveBuilder PlayAnim(string animKey, float waitTime)
	{
		Actions.Add(async delegate
		{
			await CreatureCmd.TriggerAnim(Monster.Creature, animKey, waitTime);
		});
		return this;
	}

	public MoveBuilder CustomAction(Func<IReadOnlyList<Creature>, Task> action)
	{
		Actions.Add(action);
		return this;
	}

	public MoveBuilder AddIntent(AbstractIntent intent)
	{
		Intents.Add(intent);
		return this;
	}

	public MoveBuilder FollowingState(string stateId)
	{
		FollowUpStateId = stateId;
		return this;
	}

	public MoveState Build()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Expected O, but got Unknown
		MoveState val = new MoveState(Id, (Func<IReadOnlyList<Creature>, Task>)new ActionExecutor(Actions), Intents.ToArray());
		val.set_FollowUpStateId(FollowUpStateId);
		return val;
	}

	public static implicit operator MoveState(MoveBuilder builder)
	{
		return builder.Build();
	}
}
