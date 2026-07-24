using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace BaseLib.Abstracts;

public abstract class CustomPetModel(bool visibleHp) : CustomMonsterModel()
{
	public override bool IsHealthBarVisible => visibleHp;

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		MoveState val = new MoveState("NOTHING_MOVE", (Func<IReadOnlyList<Creature>, Task>)((IReadOnlyList<Creature> _) => Task.CompletedTask), Array.Empty<AbstractIntent>());
		val.FollowUpState = (MonsterState)(object)val;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new _003C_003Ez__ReadOnlySingleElementList<MonsterState>((MonsterState)(object)val), (MonsterState)(object)val);
	}
}
