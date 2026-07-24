using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Audio;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast;

public sealed class LouseGreen : CustomMonsterModel
{
	private const int WeakAmount = 2;

	private bool _isOpen = true;

	private readonly Dictionary<Creature, int> _biteDamageByCreature = new Dictionary<Creature, int>();

	public override int MinInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 12, 11);

	public override int MaxInitialHp => AscensionHelper.GetValueIfAscension((AscensionLevel)8, 18, 17);

	protected override string VisualsPath => "res://ActsFromThePast/monsters/louse_green/louse_green.tscn";

	public override DamageSfxType TakeDamageSfxType => (DamageSfxType)4;

	public bool IsOpen
	{
		get
		{
			return _isOpen;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_isOpen = value;
		}
	}

	private int GetBiteDamage()
	{
		int value;
		return _biteDamageByCreature.TryGetValue(((MonsterModel)this).Creature, out value) ? value : 0;
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Expected O, but got Unknown
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Expected O, but got Unknown
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Expected O, but got Unknown
		List<MonsterState> list = new List<MonsterState>();
		MoveState val = new MoveState("BITE", (Func<IReadOnlyList<Creature>, Task>)Bite, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DynamicSingleAttackIntent(() => GetBiteDamage()) });
		MoveState val2 = new MoveState("SPIT_WEB", (Func<IReadOnlyList<Creature>, Task>)SpitWeb, (AbstractIntent[])(object)new AbstractIntent[1] { (AbstractIntent)new DebuffIntent(false) });
		RandomBranchState val3 = (RandomBranchState)(object)(val2.FollowUpState = (val.FollowUpState = (MonsterState)new RandomBranchState("RANDOM")));
		int num = (AscensionHelper.HasAscension((AscensionLevel)9) ? 1 : 2);
		val3.AddBranch((MonsterState)(object)val2, num, 25f);
		val3.AddBranch((MonsterState)(object)val, 2, 75f);
		list.Add((MonsterState)(object)val);
		list.Add((MonsterState)(object)val2);
		list.Add((MonsterState)(object)val3);
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)list, (MonsterState)(object)val3);
	}

	public override async Task AfterAddedToRoom()
	{
		await _003C_003En__0();
		int dmg = (AscensionHelper.HasAscension((AscensionLevel)9) ? ((MonsterModel)this).RunRng.MonsterAi.NextInt(6, 9) : ((MonsterModel)this).RunRng.MonsterAi.NextInt(5, 8));
		_biteDamageByCreature[((MonsterModel)this).Creature] = dmg;
		int curlUpAmount = (AscensionHelper.HasAscension((AscensionLevel)8) ? ((MonsterModel)this).RunRng.MonsterAi.NextInt(9, 13) : ((MonsterModel)this).RunRng.MonsterAi.NextInt(3, 8));
		await PowerCmd.Apply<CurlUpPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), ((MonsterModel)this).Creature, (decimal)curlUpAmount, ((MonsterModel)this).Creature, (CardModel)null, false);
	}

	private async Task Bite(IReadOnlyList<Creature> targets)
	{
		if (!_isOpen)
		{
			SfxCmd.Play("event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_uncurl", 1f);
			await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "transitiontoopened", 0f);
			await Cmd.Wait(0.5f, false);
			_isOpen = true;
		}
		await FastAttackAnimation.Play(((MonsterModel)this).Creature);
		await DamageCmd.Attack((decimal)GetBiteDamage()).FromMonster((MonsterModel)(object)this).WithAttackerFx((string)null, "event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_attack", (string)null)
			.WithHitFx("vfx/vfx_attack_blunt", (string)null, (string)null)
			.Execute((PlayerChoiceContext)null);
	}

	private async Task SpitWeb(IReadOnlyList<Creature> targets)
	{
		if (_isOpen)
		{
			await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "rear", 0f);
			await Cmd.Wait(0.5f, false);
		}
		else
		{
			SfxCmd.Play("event:/sfx/enemy/enemy_attacks/giant_louse/giant_louse_uncurl", 1f);
			await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "transitiontoopened", 0f);
			await Cmd.Wait(0.3f, false);
			await CreatureCmd.TriggerAnim(((MonsterModel)this).Creature, "rear", 0f);
			await Cmd.Wait(0.7f, false);
			_isOpen = true;
		}
		AFTPModAudio.Play("general", "attack_magic_fast_3", 0f, 0.02f, 1.9f);
		NCombatRoom combatRoom = NCombatRoom.Instance;
		NCreature louseNode = ((combatRoom != null) ? combatRoom.GetCreatureNode(((MonsterModel)this).Creature) : null);
		if (louseNode != null)
		{
			Vector2 sourcePos = louseNode.VfxSpawnPosition + new Vector2(-70f, -10f);
			foreach (Creature target in targets.Where((Creature t) => t.IsAlive))
			{
				NCreature targetNode = ((combatRoom != null) ? combatRoom.GetCreatureNode(target) : null);
				if (targetNode != null)
				{
					Vector2 targetPos = targetNode.VfxSpawnPosition;
					WebEffect effect = WebEffect.Create(sourcePos, targetPos);
					((Node)combatRoom.CombatVfxContainer).AddChild((Node)(object)effect, false, (InternalMode)0);
				}
			}
		}
		foreach (Creature target2 in targets.Where((Creature t) => t.IsAlive))
		{
			await PowerCmd.Apply<WeakPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), target2, 2m, ((MonsterModel)this).Creature, (CardModel)null, false);
		}
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Expected O, but got Unknown
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Expected O, but got Unknown
		AnimState val = new AnimState("idle", true);
		AnimState val2 = new AnimState("idle closed", true);
		AnimState val3 = new AnimState("rear", false);
		AnimState val4 = new AnimState("transitiontoopened", false);
		AnimState val5 = new AnimState("transitiontoclosed", false);
		val3.NextState = val;
		val4.NextState = val;
		val5.NextState = val2;
		val.AddBranch("rear", val3, (Func<bool>)null);
		val.AddBranch("Curl", val5, (Func<bool>)null);
		val.AddBranch("transitiontoclosed", val5, (Func<bool>)null);
		val2.AddBranch("transitiontoopened", val4, (Func<bool>)null);
		val2.AddBranch("rear", val3, (Func<bool>)null);
		val4.AddBranch("rear", val3, (Func<bool>)null);
		return new CreatureAnimator(val, controller);
	}

	[CompilerGenerated]
	[DebuggerHidden]
	private Task _003C_003En__0()
	{
		return ((MonsterModel)this).AfterAddedToRoom();
	}
}
