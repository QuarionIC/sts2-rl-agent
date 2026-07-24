using System;
using System.Collections.Generic;
using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class CenturionAndMysticNormal : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

	public override bool IsWeak => false;

	public override IEnumerable<MonsterModel> AllPossibleMonsters => (IEnumerable<MonsterModel>)(object)new MonsterModel[2]
	{
		(MonsterModel)ModelDb.Monster<Centurion>(),
		(MonsterModel)ModelDb.Monster<Mystic>()
	};

	public CenturionAndMysticNormal()
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
			(((MonsterModel)ModelDb.Monster<Centurion>()).ToMutable(), null),
			(((MonsterModel)ModelDb.Monster<Mystic>()).ToMutable(), null)
		};
	}
}
