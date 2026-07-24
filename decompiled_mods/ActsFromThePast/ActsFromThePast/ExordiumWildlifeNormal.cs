using System;
using System.Collections.Generic;
using System.Linq;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ExordiumWildlifeNormal : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

	public override bool IsWeak => false;

	private static MonsterModel[] StrongPool => (MonsterModel[])(object)new MonsterModel[2]
	{
		(MonsterModel)ModelDb.Monster<FungiBeast>(),
		(MonsterModel)ModelDb.Monster<JawWorm>()
	};

	private static MonsterModel[] WeakPool => (MonsterModel[])(object)new MonsterModel[4]
	{
		(MonsterModel)ModelDb.Monster<LouseRed>(),
		(MonsterModel)ModelDb.Monster<LouseGreen>(),
		(MonsterModel)ModelDb.Monster<SpikeSlimeMedium>(),
		(MonsterModel)ModelDb.Monster<AcidSlimeMedium>()
	};

	public override IEnumerable<MonsterModel> AllPossibleMonsters => StrongPool.Concat(WeakPool);

	public ExordiumWildlifeNormal()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is ExordiumAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)>
		{
			(((EncounterModel)this).Rng.NextItem<MonsterModel>((IEnumerable<MonsterModel>)StrongPool).ToMutable(), null),
			(((EncounterModel)this).Rng.NextItem<MonsterModel>((IEnumerable<MonsterModel>)WeakPool).ToMutable(), null)
		};
	}
}
