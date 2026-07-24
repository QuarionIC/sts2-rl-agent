using System.Collections.Generic;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class AwakenedOneBoss : CustomEncounterModel
{
	public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/awakened_one";

	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[3] { "cultist_left", "cultist_right", "awakened" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters => (IEnumerable<MonsterModel>)(object)new MonsterModel[2]
	{
		(MonsterModel)ModelDb.Monster<Cultist>(),
		(MonsterModel)ModelDb.Monster<AwakenedOne>()
	};

	public AwakenedOneBoss()
		: base((RoomType)3, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheBeyondAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)>
		{
			(((MonsterModel)ModelDb.Monster<Cultist>()).ToMutable(), "cultist_left"),
			(((MonsterModel)ModelDb.Monster<Cultist>()).ToMutable(), "cultist_right"),
			(((MonsterModel)ModelDb.Monster<AwakenedOne>()).ToMutable(), "awakened")
		};
	}
}
