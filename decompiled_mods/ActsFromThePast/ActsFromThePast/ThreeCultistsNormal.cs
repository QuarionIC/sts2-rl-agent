using System.Collections.Generic;
using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ThreeCultistsNormal : CustomEncounterModel
{
	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[3] { "first", "second", "third" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Cultist>();
		}
	}

	public ThreeCultistsNormal()
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
			(((MonsterModel)ModelDb.Monster<Cultist>()).ToMutable(), "first"),
			(((MonsterModel)ModelDb.Monster<Cultist>()).ToMutable(), "second"),
			(((MonsterModel)ModelDb.Monster<Cultist>()).ToMutable(), "third")
		};
	}
}
