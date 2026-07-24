using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ColosseumSecondEncounter : CustomEncounterModel
{
	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Taskmaster>();
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinNob>();
		}
	}

	public ColosseumSecondEncounter()
		: base((RoomType)2, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return false;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)>
		{
			(((MonsterModel)ModelDb.Monster<Taskmaster>()).ToMutable(), null),
			(((MonsterModel)ModelDb.Monster<GremlinNob>()).ToMutable(), null)
		};
	}
}
