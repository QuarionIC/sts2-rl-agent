using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace BaseLib.Utils;

public static class MonsterActions
{
	public static AttackCommand Attack(MonsterModel monster, int baseDmg, int hitCount = 1)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		AttackCommand val = new AttackCommand((decimal)baseDmg).FromMonster(monster);
		if (hitCount != 1)
		{
			val.WithHitCount(hitCount);
		}
		return val;
	}

	public static async Task<T?> ApplySelf<T>(MonsterModel monster, decimal amount, PlayerChoiceContext? context = null, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.Apply.InvokeGeneric<Task<T>, T>(null, new object[6]
		{
			((object)context) ?? ((object)new ThrowingPlayerChoiceContext()),
			monster.Creature,
			amount,
			monster.Creature,
			null,
			silent
		});
	}

	public static async Task<IReadOnlyList<T>> Apply<T>(MonsterModel monster, decimal amount, IEnumerable<Creature> targets, PlayerChoiceContext? context = null, bool silent = false) where T : PowerModel
	{
		return await BetaMainCompatibility.PowerCmd_.ApplyMulti.InvokeGeneric<Task<IReadOnlyList<T>>, T>(null, new object[6]
		{
			((object)context) ?? ((object)new ThrowingPlayerChoiceContext()),
			targets,
			amount,
			monster.Creature,
			null,
			silent
		});
	}
}
