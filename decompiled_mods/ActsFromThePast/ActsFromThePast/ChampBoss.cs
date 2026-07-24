using System.Collections.Generic;
using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ChampBoss : CustomEncounterModel
{
	public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/champ";

	public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> { (MonsterModel)(object)ModelDb.Monster<Champ>() };

	public ChampBoss()
		: base((RoomType)3, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheCityAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((MonsterModel)ModelDb.Monster<Champ>()).ToMutable(), null) };
	}
}
