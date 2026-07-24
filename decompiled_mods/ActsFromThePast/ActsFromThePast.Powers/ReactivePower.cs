using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.ValueProps;

namespace ActsFromThePast.Powers;

public sealed class ReactivePower : CustomPowerModel
{
	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)0;

	public override async Task AfterDamageReceived(PlayerChoiceContext choiceContext, Creature target, DamageResult result, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		if (target != ((PowerModel)this).Owner || ((Enum)props).HasFlag((Enum)(object)(ValueProp)4) || !((Enum)props).HasFlag((Enum)(object)(ValueProp)8) || result.UnblockedDamage <= 0 || target.CurrentHp <= 0)
		{
			return;
		}
		((PowerModel)this).Flash();
		MonsterModel monster = ((PowerModel)this).Owner.Monster;
		if (monster == null)
		{
			return;
		}
		MoveState nextMove = monster.NextMove;
		string currentMoveId = ((nextMove != null) ? ((MonsterState)nextMove).Id : null);
		List<MonsterState> candidates = monster.MoveStateMachine.States.Values.Where((MonsterState s) => s.IsMove && s.Id != currentMoveId && s.Id != "MOVE_BRANCH").ToList();
		if (candidates.Count <= 0)
		{
			return;
		}
		if (monster is WrithingMass wm && wm.UsedMegaDebuff)
		{
			candidates.RemoveAll((MonsterState s) => s.Id == "MEGA_DEBUFF");
		}
		if (candidates.Count > 0)
		{
			MonsterState next = candidates[monster.RunRng.MonsterAi.NextInt(candidates.Count)];
			monster.SetMoveImmediate((MoveState)next, false);
		}
	}
}
