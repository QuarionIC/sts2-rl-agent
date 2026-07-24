using System;
using System.Collections.Generic;
using System.Linq;
using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class GremlinLeaderElite : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[4] { "gremlin1", "gremlin2", "gremlin3", "leader" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinLeader>();
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinMad>();
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinSneaky>();
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinFat>();
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinShield>();
			yield return (MonsterModel)(object)ModelDb.Monster<GremlinWizard>();
		}
	}

	public override IEnumerable<string> ExtraAssetPaths => new string[1] { "res://scenes/vfx/vfx_fire_burst.tscn" };

	public GremlinLeaderElite()
		: base((RoomType)2, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheCityAct;
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
		List<string> list3 = ((EncounterModel)this).Slots.Where((string s) => s != "leader").Reverse().ToList();
		for (int num = 0; num < 2; num++)
		{
			int index = ((EncounterModel)this).Rng.NextInt(list.Count);
			list2.Add((list[index](), list3[num]));
			list.RemoveAt(index);
		}
		list2.Add((((MonsterModel)ModelDb.Monster<GremlinLeader>()).ToMutable(), "leader"));
		return list2;
	}
}
