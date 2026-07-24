using System;
using System.Collections.Generic;
using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class TwoThievesWeak : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

	public override bool IsWeak => true;

	public override IEnumerable<MonsterModel> AllPossibleMonsters => (IEnumerable<MonsterModel>)(object)new MonsterModel[2]
	{
		(MonsterModel)ModelDb.Monster<Looter>(),
		(MonsterModel)ModelDb.Monster<Mugger>()
	};

	public TwoThievesWeak()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheCityAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)>
		{
			(((MonsterModel)ModelDb.Monster<Looter>()).ToMutable(), null),
			(((MonsterModel)ModelDb.Monster<Mugger>()).ToMutable(), null)
		};
	}
}
