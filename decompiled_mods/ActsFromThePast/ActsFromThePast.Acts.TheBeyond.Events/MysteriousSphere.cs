using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ActsFromThePast.Acts.TheBeyond.Encounters;
using ActsFromThePast.Patches.RoomEvents;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Rewards;

namespace ActsFromThePast.Acts.TheBeyond.Events;

public sealed class MysteriousSphere : CustomEventModel
{
	public override bool IsShared => true;

	public override EventLayoutType LayoutType => (EventLayoutType)1;

	public override EncounterModel CanonicalEncounter => (EncounterModel)(object)ModelDb.Encounter<TwoOrbWalkersEvent>();

	public override ActModel[] Acts => (ActModel[])(object)new TheBeyondAct[1] { ModelDb.Act<TheBeyondAct>() };

	protected override IReadOnlyList<EventOption> GenerateInitialOptions()
	{
		if (ActsFromThePastConfig.RebalancedMode)
		{
			return (IReadOnlyList<EventOption>)(object)new EventOption[2]
			{
				((CustomEventModel)this).Option((Func<Task>)Open, "INITIAL", Array.Empty<IHoverTip>()),
				((CustomEventModel)this).Option((Func<Task>)Distract, "INITIAL_REBALANCED", Array.Empty<IHoverTip>())
			};
		}
		return (IReadOnlyList<EventOption>)(object)new EventOption[2]
		{
			((CustomEventModel)this).Option((Func<Task>)Open, "INITIAL", Array.Empty<IHoverTip>()),
			((CustomEventModel)this).Option((Func<Task>)Leave, "INITIAL", Array.Empty<IHoverTip>())
		};
	}

	private Task Open()
	{
		MysteriousSpherePatches.SwapToOpenSphere();
		((EventModel)this).SetEventState(((CustomEventModel)this).PageDescription("PRE_COMBAT"), (IEnumerable<EventOption>)(object)new EventOption[1] { ((CustomEventModel)this).Option((Func<Task>)Fight, "PRE_COMBAT", Array.Empty<IHoverTip>()) });
		return Task.CompletedTask;
	}

	private Task Fight()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		RelicModel val = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner, (RelicRarity)4).ToMutable();
		List<Reward> list = new List<Reward>
		{
			(Reward)new GoldReward(45, 55, ((EventModel)this).Owner, false),
			(Reward)new RelicReward(val, ((EventModel)this).Owner)
		};
		((EventModel)this).EnterCombatWithoutExitingEvent<TwoOrbWalkersEvent>((IReadOnlyList<Reward>)list, false);
		return Task.CompletedTask;
	}

	private Task Leave()
	{
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("LEAVE"));
		return Task.CompletedTask;
	}

	private async Task Distract()
	{
		IReadOnlyList<Creature> enemies = ((EventModel)this)._combatSynchronizer.CombatStateForLayout.Enemies;
		foreach (Creature enemy in enemies)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(enemy) : null);
			if (creatureNode != null)
			{
				Vector2 scale = ((Control)creatureNode).Scale;
				((Control)creatureNode).Scale = new Vector2(0f - scale.X, scale.Y);
				Vector2 endPos = ((Control)creatureNode).Position + new Vector2(1200f, 0f);
				Tween tween = ((Node)creatureNode).CreateTween();
				tween.TweenProperty((GodotObject)(object)creatureNode, NodePath.op_Implicit("position"), Variant.op_Implicit(endPos), 3.0).SetTrans((TransitionType)0);
			}
		}
		MysteriousSpherePatches.SwapToOpenSphere();
		RelicModel commonRelic = RelicFactory.PullNextRelicFromFront(((EventModel)this).Owner, (RelicRarity)2).ToMutable();
		await RewardsCmd.OfferCustom(((EventModel)this).Owner, new List<Reward>(1) { (Reward)new RelicReward(commonRelic, ((EventModel)this).Owner) });
		((EventModel)this).SetEventFinished(((CustomEventModel)this).PageDescription("DISTRACT"));
	}

	public MysteriousSphere()
		: base(true)
	{
	}
}
