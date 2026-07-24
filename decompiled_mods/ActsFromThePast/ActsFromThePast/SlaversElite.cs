using System.Collections.Generic;
using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SlaversElite : CustomEncounterModel
{
	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[3] { "blue", "taskmaster", "red" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<SlaverBlue>();
			yield return (MonsterModel)(object)ModelDb.Monster<Taskmaster>();
			yield return (MonsterModel)(object)ModelDb.Monster<SlaverRed>();
		}
	}

	public SlaversElite()
		: base((RoomType)2, true)
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
			(((MonsterModel)ModelDb.Monster<SlaverBlue>()).ToMutable(), "blue"),
			(((MonsterModel)ModelDb.Monster<Taskmaster>()).ToMutable(), "taskmaster"),
			(((MonsterModel)ModelDb.Monster<SlaverRed>()).ToMutable(), "red")
		};
	}
}
