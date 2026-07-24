using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ActsFromThePast.Powers;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast;

public sealed class BronzeOrb : CustomMonsterModel
{
	private const int BeamDamage = 8;

	private const int SupportBlockAmount = 12;

	private const string BEAM = "BEAM";

	private const string SUPPORT_BEAM = "SUPPORT_BEAM";

	private const string STASIS = "STASIS";

	private int _bobIndex;

	private bool _usedStasis;

	private bool _spawnAnimPending;

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 54, 52);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 60, 58);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/bronze_orb/bronze_orb.tscn";

	public int BobIndex
	{
		get
		{
			return _bobIndex;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_bobIndex = value;
		}
	}

	private bool UsedStasis
	{
		get
		{
			return _usedStasis;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_usedStasis = value;
		}
	}

	public bool SpawnAnimPending
	{
		get
		{
			return _spawnAnimPending;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_spawnAnimPending = value;
		}
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		_usedStasis = false;
		if (SpawnAnimPending)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				NCreature creatureNode = instance.GetCreatureNode(((MonsterModel)this).Creature);
				obj = ((creatureNode != null) ? creatureNode.Visuals : null);
			}
			NCreatureVisuals visuals = (NCreatureVisuals)obj;
			Sprite2D sprite = ((visuals != null) ? ((Node)visuals).GetNodeOrNull<Sprite2D>(NodePath.op_Implicit("Visuals")) : null);
			if (sprite != null)
			{
				((CanvasItem)sprite).Visible = false;
			}
		}
		StartBobAnimation();
	}

	private void StartBobAnimation()
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		NCombatRoom instance = NCombatRoom.Instance;
		NCreature val = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
		NCreatureVisuals val2 = ((val != null) ? val.Visuals : null);
		if (val2 != null)
		{
			Vector2 position = ((Node2D)val2).Position;
			float num = 6f;
			float num2 = 2.16f;
			Tween val3 = ((Node)val).CreateTween();
			val3.SetLoops(0);
			if (BobIndex % 2 == 0)
			{
				val3.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("position:y"), Variant.op_Implicit(position.Y - num), (double)(num2 / 2f)).SetTrans((TransitionType)1).SetEase((EaseType)2);
				val3.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("position:y"), Variant.op_Implicit(position.Y + num), (double)(num2 / 2f)).SetTrans((TransitionType)1).SetEase((EaseType)2);
			}
			else
			{
				val3.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("position:y"), Variant.op_Implicit(position.Y + num), (double)(num2 / 2f)).SetTrans((TransitionType)1).SetEase((EaseType)2);
				val3.TweenProperty((GodotObject)(object)val2, NodePath.op_Implicit("position:y"), Variant.op_Implicit(position.Y - num), (double)(num2 / 2f)).SetTrans((TransitionType)1).SetEase((EaseType)2);
			}
		}
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Expected O, but got Unknown
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Expected O, but got Unknown
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected O, but got Unknown
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("BEAM", (Func<IReadOnlyList<Creature>, Task>)BeamMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new SingleAttackIntent(8) });
		MoveState val2 = new MoveState("SUPPORT_BEAM", (Func<IReadOnlyList<Creature>, Task>)SupportBeamMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DefendIntent() });
		MoveState val3 = new MoveState("STASIS", (Func<IReadOnlyList<Creature>, Task>)StasisMove, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new CardDebuffIntent() });
		ConditionalBranchState conditionalBranchState = (ConditionalBranchState)(object)(val3.FollowUpState = (val2.FollowUpState = (val.FollowUpState = (MonsterState)(object)new ConditionalBranchState("MOVE_BRANCH", SelectNextMove))));
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		list.Add((MonsterState)(object)conditionalBranchState);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)conditionalBranchState);
	}

	private string SelectNextMove(Creature owner, Rng rng, MonsterMoveStateMachine stateMachine)
	{
		int num = rng.NextInt(100);
		if (!UsedStasis && num >= 25)
		{
			UsedStasis = true;
			return "STASIS";
		}
		if (num >= 70 && !LastTwoMoves(stateMachine, "SUPPORT_BEAM"))
		{
			return "SUPPORT_BEAM";
		}
		if (!LastTwoMoves(stateMachine, "BEAM"))
		{
			return "BEAM";
		}
		return "SUPPORT_BEAM";
	}

	private static bool LastTwoMoves(MonsterMoveStateMachine stateMachine, string moveId)
	{
		List<MonsterState> stateLog = stateMachine.StateLog;
		if (stateLog.Count < 2)
		{
			return false;
		}
		return stateLog[stateLog.Count - 1].Id == moveId && stateLog[stateLog.Count - 2].Id == moveId;
	}

	private async Task BeamMove(IReadOnlyList<Creature> targets)
	{
		BorderFlashEffect.PlaySky();
		AFTPModAudio.Play("general", "magic_beam_short", -6f);
		Creature target = ((IEnumerable<Creature>)targets).FirstOrDefault((Func<Creature, bool>)((Creature t) => t.IsAlive));
		Vector2 targetPos = ((target != null) ? Sts1VfxHelper.GetCreatureCenter(target) : Vector2.Zero);
		Vector2 orbPos = Sts1VfxHelper.GetCreatureCenter(((MonsterModel)this).Creature);
		SmallLaserEffect laser = SmallLaserEffect.Create(orbPos, targetPos);
		Sts1VfxHelper.Play(laser);
		await Cmd.Wait(0.3f, false);
		await DamageCmd.Attack(8m).FromMonster((MonsterModel)(object)this).Execute((PlayerChoiceContext)null);
	}

	private async Task SupportBeamMove(IReadOnlyList<Creature> targets)
	{
		Creature automaton = ((IEnumerable<Creature>)((MonsterModel)this).CombatState.GetTeammatesOf(((MonsterModel)this).Creature)).FirstOrDefault((Func<Creature, bool>)((Creature t) => t.Monster is BronzeAutomaton && t.IsAlive));
		if (automaton != null)
		{
			AFTPModAudio.Play("general", "magic_beam_short", -6f);
			Vector2 orbPos = Sts1VfxHelper.GetCreatureCenter(((MonsterModel)this).Creature);
			Vector2 automatonPos = Sts1VfxHelper.GetCreatureCenter(automaton);
			SmallLaserEffect laser = SmallLaserEffect.Create(orbPos, automatonPos);
			Sts1VfxHelper.Play(laser);
			await Cmd.Wait(0.3f, false);
			await CreatureCmd.GainBlock(automaton, 12m, (ValueProp)8, (CardPlay)null, false);
		}
	}

	private async Task StasisMove(IReadOnlyList<Creature> targets)
	{
		foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
		{
			Player player = target.Player ?? target.PetOwner;
			List<CardModel> drawCards = CardPile.GetCards(player, (PileType[])(object)new PileType[1] { (PileType)1 }).ToList();
			List<CardModel> discardCards = CardPile.GetCards(player, (PileType[])(object)new PileType[1] { (PileType)3 }).ToList();
			if (drawCards.Count == 0 && discardCards.Count == 0)
			{
				continue;
			}
			List<CardModel> pool = ((drawCards.Count > 0) ? drawCards : discardCards);
			ListExtensions.StableShuffle<CardModel>(pool, ((MonsterModel)this).RunRng.CombatCardGeneration);
			CardModel cardToSteal = ((IEnumerable<CardModel>)pool).FirstOrDefault((Func<CardModel, bool>)((CardModel c) => (int)c.Rarity == 4)) ?? ((IEnumerable<CardModel>)pool).FirstOrDefault((Func<CardModel, bool>)((CardModel c) => (int)c.Rarity == 3)) ?? ((IEnumerable<CardModel>)pool).FirstOrDefault((Func<CardModel, bool>)((CardModel c) => (int)c.Rarity == 2)) ?? pool.FirstOrDefault();
			if (cardToSteal == null)
			{
				continue;
			}
			await CardPileCmd.RemoveFromCombat(cardToSteal, false);
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((MonsterModel)this).Creature) : null);
			if (creatureNode != null && LocalContext.IsMine(cardToSteal))
			{
				Marker2D specialNode = creatureNode.GetSpecialNode<Marker2D>("%StolenCardPos");
				if (specialNode != null)
				{
					NCard ncard = NCard.Create(cardToSteal, (ModelVisibility)1);
					GodotTreeExtensions.AddChildSafely((Node)(object)specialNode, (Node)(object)ncard);
					((Control)ncard).Position = ((Control)ncard).Position + ((Control)ncard).Size * 0.5f;
					ncard.UpdateVisuals((PileType)6, (CardPreviewMode)1);
				}
			}
			StasisPower stasis = (StasisPower)(object)((PowerModel)ModelDb.Power<StasisPower>()).ToMutable(0);
			await stasis.Capture(cardToSteal, target);
			await PowerCmd.Apply((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), (PowerModel)(object)stasis, ((MonsterModel)this).Creature, 1m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
