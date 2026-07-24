using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast.Powers;

public sealed class LifeLinkPower : CustomPowerModel
{
	private class Data
	{
		public bool isReviving;
	}

	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)2;

	private bool IsReviving => ((PowerModel)this).GetInternalData<Data>().isReviving;

	protected override object InitInternalData()
	{
		return new Data();
	}

	public async Task DoReattach()
	{
		if (!AreAllOtherDarklingsDead())
		{
			((PowerModel)this).GetInternalData<Data>().isReviving = false;
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				instance.SetCreatureIsInteractable(((PowerModel)this).Owner, true);
			}
			await CreatureCmd.TriggerAnim(((PowerModel)this).Owner, "Revive", 0f);
			await CreatureCmd.Heal(((PowerModel)this).Owner, (decimal)((PowerModel)this).Amount, true);
		}
	}

	public override async Task AfterDeath(PlayerChoiceContext choiceContext, Creature creature, bool wasRemovalPrevented, float deathAnimLength)
	{
		if (wasRemovalPrevented || ((PowerModel)this).Owner != creature)
		{
			return;
		}
		if (!AreAllOtherDarklingsDead() || !((PowerModel)this).Owner.IsDead)
		{
			((PowerModel)this).GetInternalData<Data>().isReviving = true;
			MonsterModel monster = creature.Monster;
			if (monster is Darkling darkling)
			{
				((PowerModel)this).Owner.Monster.SetMoveImmediate(darkling.DeadState, false);
			}
			await CreatureCmd.TriggerAnim(((PowerModel)this).Owner, "Dead", 0f);
			NCombatRoom instance = NCombatRoom.Instance;
			if (instance != null)
			{
				instance.SetCreatureIsInteractable(((PowerModel)this).Owner, false);
			}
		}
		else
		{
			await Cmd.Wait(0.25f, true);
			DoFadeOutOnAllDarklings();
		}
	}

	private void DoFadeOutOnAllDarklings()
	{
		List<NCreature> list = new List<NCreature>();
		foreach (Creature enemy in ((PowerModel)this).CombatState.Enemies)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(enemy) : null);
			if (val != null)
			{
				val.AnimHideIntent(0.0);
				list.Add(val);
			}
		}
		NMonsterDeathVfx val2 = NMonsterDeathVfx.Create(list);
		if (val2 == null || list.Count <= 0)
		{
			return;
		}
		Node parent = ((Node)list[0]).GetParent();
		GodotTreeExtensions.AddChildSafely(parent, (Node)(object)val2);
		GodotTreeExtensions.MoveChildSafely(parent, (Node)(object)val2, ((Node)list[0]).GetIndex(false));
		Task deathAnimationTask = TaskHelper.RunSafely(PlayVfxAndRemoveNodes(val2, list));
		foreach (NCreature item in list)
		{
			item.DeathAnimationTask = deathAnimationTask;
			NCombatRoom instance2 = NCombatRoom.Instance;
			if (instance2 != null)
			{
				instance2.RemoveCreatureNode(item);
			}
		}
	}

	private async Task PlayVfxAndRemoveNodes(NMonsterDeathVfx vfx, List<NCreature> nodes)
	{
		await Cmd.Wait(0.25f, true);
		await vfx.PlayVfx();
		foreach (NCreature node in nodes)
		{
			GodotTreeExtensions.QueueFreeSafely((Node)(object)node);
		}
	}

	public override bool ShouldAllowHitting(Creature creature)
	{
		return creature != ((PowerModel)this).Owner || !IsReviving;
	}

	public override bool ShouldCreatureBeRemovedFromCombatAfterDeath(Creature creature)
	{
		return creature != ((PowerModel)this).Owner;
	}

	public override bool ShouldPowerBeRemovedAfterOwnerDeath()
	{
		return false;
	}

	public override bool ShouldOwnerDeathTriggerFatal()
	{
		return AreAllOtherDarklingsDead();
	}

	private IEnumerable<Creature> GetOtherDarklings()
	{
		return from c in ((PowerModel)this).Owner.CombatState.GetTeammatesOf(((PowerModel)this).Owner)
			where c != ((PowerModel)this).Owner && c.Monster is Darkling
			select c;
	}

	private bool AreAllOtherDarklingsDead()
	{
		return GetOtherDarklings().All((Creature s) => s.IsDead);
	}
}
