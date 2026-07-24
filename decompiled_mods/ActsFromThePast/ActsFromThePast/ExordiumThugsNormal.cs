using System;
using System.Collections.Generic;
using System.Linq;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ExordiumThugsNormal : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

	public override bool IsWeak => false;

	private static MonsterModel[] FrontPool => (MonsterModel[])(object)new MonsterModel[4]
	{
		(MonsterModel)ModelDb.Monster<LouseRed>(),
		(MonsterModel)ModelDb.Monster<LouseGreen>(),
		(MonsterModel)ModelDb.Monster<SpikeSlimeMedium>(),
		(MonsterModel)ModelDb.Monster<AcidSlimeMedium>()
	};

	private static MonsterModel[] BackPool => (MonsterModel[])(object)new MonsterModel[4]
	{
		(MonsterModel)ModelDb.Monster<Cultist>(),
		(MonsterModel)ModelDb.Monster<SlaverBlue>(),
		(MonsterModel)ModelDb.Monster<SlaverRed>(),
		(MonsterModel)ModelDb.Monster<Looter>()
	};

	public override IEnumerable<MonsterModel> AllPossibleMonsters => FrontPool.Concat(BackPool);

	public ExordiumThugsNormal()
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
			(((EncounterModel)this).Rng.NextItem<MonsterModel>((IEnumerable<MonsterModel>)FrontPool).ToMutable(), null),
			(((EncounterModel)this).Rng.NextItem<MonsterModel>((IEnumerable<MonsterModel>)BackPool).ToMutable(), null)
		};
	}
}
