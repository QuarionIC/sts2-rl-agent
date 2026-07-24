using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class DeadAdventurerLagavulin : CustomEncounterModel
{
	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Lagavulin>();
		}
	}

	public DeadAdventurerLagavulin()
		: base((RoomType)2, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return false;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		Lagavulin lagavulin = (Lagavulin)(object)((MonsterModel)ModelDb.Monster<Lagavulin>()).ToMutable();
		lagavulin.StartsAwake = true;
		return new List<(MonsterModel, string)> { ((MonsterModel)(object)lagavulin, null) };
	}
}
