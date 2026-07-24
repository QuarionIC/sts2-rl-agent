using System;
using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LotsOfSlimesNormal : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => new _003C_003Ez__ReadOnlySingleElementList<EncounterTag>((EncounterTag)5);

	public override bool IsWeak => false;

	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[5] { "first", "second", "third", "fourth", "fifth" };

	private static MonsterModel[] SmallSlimes => (MonsterModel[])(object)new MonsterModel[2]
	{
		(MonsterModel)ModelDb.Monster<SpikeSlimeSmall>(),
		(MonsterModel)ModelDb.Monster<AcidSlimeSmall>()
	};

	public override IEnumerable<MonsterModel> AllPossibleMonsters => SmallSlimes;

	public LotsOfSlimesNormal()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is ExordiumAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		List<Func<MonsterModel>> list = new List<Func<MonsterModel>>
		{
			() => ((MonsterModel)ModelDb.Monster<SpikeSlimeSmall>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<SpikeSlimeSmall>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<SpikeSlimeSmall>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<AcidSlimeSmall>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<AcidSlimeSmall>()).ToMutable()
		};
		List<(MonsterModel, string)> list2 = new List<(MonsterModel, string)>();
		for (int num = 0; num < ((EncounterModel)this).Slots.Count; num++)
		{
			int index = ((EncounterModel)this).Rng.NextInt(list.Count);
			list2.Add((list[index](), ((EncounterModel)this).Slots[num]));
			list.RemoveAt(index);
		}
		return list2;
	}
}
