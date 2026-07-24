using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LiceNormal : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => new _003C_003Ez__ReadOnlySingleElementList<EncounterTag>(CustomEncounterTags.Lice);

	public override bool IsWeak => false;

	private static MonsterModel[] Lice => (MonsterModel[])(object)new MonsterModel[2]
	{
		(MonsterModel)ModelDb.Monster<LouseRed>(),
		(MonsterModel)ModelDb.Monster<LouseGreen>()
	};

	public override IEnumerable<MonsterModel> AllPossibleMonsters => Lice;

	public LiceNormal()
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
			(((EncounterModel)this).Rng.NextItem<MonsterModel>((IEnumerable<MonsterModel>)Lice).ToMutable(), null),
			(((EncounterModel)this).Rng.NextItem<MonsterModel>((IEnumerable<MonsterModel>)Lice).ToMutable(), null),
			(((EncounterModel)this).Rng.NextItem<MonsterModel>((IEnumerable<MonsterModel>)Lice).ToMutable(), null)
		};
	}
}
