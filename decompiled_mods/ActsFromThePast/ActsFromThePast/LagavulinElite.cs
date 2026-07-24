using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LagavulinElite : CustomEncounterModel
{
	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Lagavulin>();
		}
	}

	public LagavulinElite()
		: base((RoomType)2, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is ExordiumAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((MonsterModel)ModelDb.Monster<Lagavulin>()).ToMutable(), null) };
	}
}
