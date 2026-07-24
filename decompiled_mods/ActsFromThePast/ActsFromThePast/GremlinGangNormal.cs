using System;
using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class GremlinGangNormal : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

	public override bool IsWeak => false;

	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[4] { "first", "second", "third", "fourth" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinMad>();
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinSneaky>();
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinFat>();
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinShield>();
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinWizard>();
		}
	}

	public override IEnumerable<string> ExtraAssetPaths => new string[1] { "res://scenes/vfx/vfx_fire_burst.tscn" };

	public GremlinGangNormal()
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
			() => ((MonsterModel)ModelDb.Monster<GremlinMad>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<GremlinMad>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<GremlinSneaky>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<GremlinSneaky>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<GremlinFat>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<GremlinFat>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<GremlinShield>()).ToMutable(),
			() => ((MonsterModel)ModelDb.Monster<GremlinWizard>()).ToMutable()
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
