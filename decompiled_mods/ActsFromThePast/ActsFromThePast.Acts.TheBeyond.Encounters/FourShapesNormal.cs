using System;
using System.Collections.Generic;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class FourShapesNormal : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => new _003C_003Ez__ReadOnlySingleElementList<EncounterTag>(CustomEncounterTags.Shapes);

	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[4] { "first", "second", "third", "fourth" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Repulsor>();
			yield return (MonsterModel)(object)ModelDb.Monster<Exploder>();
			yield return (MonsterModel)(object)ModelDb.Monster<Spiker>();
		}
	}

	public FourShapesNormal()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheBeyondAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		List<Func<MonsterModel>> list = new List<Func<MonsterModel>>
		{
			() => ((MonsterModel)ModelDb.Monster<Repulsor>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<Repulsor>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<Exploder>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<Exploder>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<Spiker>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<Spiker>()).ToMutable()
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
