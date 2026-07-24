using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ThreeFungiBeastsEvent : CustomEncounterModel
{
	public override bool IsWeak => false;

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<FungiBeast>();
		}
	}

	public ThreeFungiBeastsEvent()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return false;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)>
		{
			(((MonsterModel)ModelDb.Monster<FungiBeast>()).ToMutable(), null),
			(((MonsterModel)ModelDb.Monster<FungiBeast>()).ToMutable(), null),
			(((MonsterModel)ModelDb.Monster<FungiBeast>()).ToMutable(), null)
		};
	}
}
