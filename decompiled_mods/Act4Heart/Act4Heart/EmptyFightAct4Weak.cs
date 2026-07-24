using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Act4Heart;

internal class EmptyFightAct4Weak : EncounterModel
{
	public override RoomType RoomType => (RoomType)1;

	public override IEnumerable<MonsterModel> AllPossibleMonsters => Array.Empty<MonsterModel>();

	public override bool IsWeak => true;

	public override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return Array.Empty<(MonsterModel, string)>();
	}
}
