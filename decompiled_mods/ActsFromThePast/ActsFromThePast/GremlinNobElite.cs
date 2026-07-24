using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class GremlinNobElite : CustomEncounterModel
{
	public override RoomType RoomType => (RoomType)2;

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinNob>();
		}
	}

	public GremlinNobElite()
		: base((RoomType)2, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is ExordiumAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((MonsterModel)ModelDb.Monster<GremlinNob>()).ToMutable(), null) };
	}
}
