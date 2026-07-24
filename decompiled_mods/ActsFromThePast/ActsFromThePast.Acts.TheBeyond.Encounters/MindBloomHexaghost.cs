using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class MindBloomHexaghost : CustomEncounterModel
{
	public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> { (MonsterModel)(object)ModelDb.Monster<Hexaghost>() };

	public MindBloomHexaghost()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return false;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((MonsterModel)ModelDb.Monster<Hexaghost>()).ToMutable(), null) };
	}
}
