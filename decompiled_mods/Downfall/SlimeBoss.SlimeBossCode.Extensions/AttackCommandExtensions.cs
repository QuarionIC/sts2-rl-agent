using System;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Extensions;

public static class AttackCommandExtensions
{
	public static AttackCommand FromSlime(this AttackCommand command, SlimeModel slime)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		if (command.Attacker != null)
		{
			throw new InvalidOperationException("Attacker has already been set.");
		}
		command.Attacker = ((MonsterModel)slime).Creature;
		command._attackerAnimName = "Attack";
		command._sourceType = (SourceType)0;
		return command;
	}
}
