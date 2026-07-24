using System.Collections.Generic;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters.Elite;

public sealed class GiantHeadElite : CustomEncounterModel
{
	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<GiantHead>();
		}
	}

	public GiantHeadElite()
		: base((RoomType)2, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheBeyondAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((MonsterModel)ModelDb.Monster<GiantHead>()).ToMutable(), null) };
	}
}
