using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class GuardianBoss : CustomEncounterModel
{
	public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/guardian";

	public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel> { (MonsterModel)(object)ModelDb.Monster<Guardian>() };

	public GuardianBoss()
		: base((RoomType)3, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is ExordiumAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((MonsterModel)ModelDb.Monster<Guardian>()).ToMutable(), null) };
	}
}
