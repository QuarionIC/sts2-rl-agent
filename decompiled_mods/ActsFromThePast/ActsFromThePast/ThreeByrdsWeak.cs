using System;
using System.Collections.Generic;
using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ThreeByrdsWeak : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

	public override bool IsWeak => true;

	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[3] { "first", "second", "third" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters => (IEnumerable<MonsterModel>)(object)new MonsterModel[1] { (MonsterModel)ModelDb.Monster<Byrd>() };

	public ThreeByrdsWeak()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheCityAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)>
		{
			(((MonsterModel)ModelDb.Monster<Byrd>()).ToMutable(), "first"),
			(((MonsterModel)ModelDb.Monster<Byrd>()).ToMutable(), "second"),
			(((MonsterModel)ModelDb.Monster<Byrd>()).ToMutable(), "third")
		};
	}
}
