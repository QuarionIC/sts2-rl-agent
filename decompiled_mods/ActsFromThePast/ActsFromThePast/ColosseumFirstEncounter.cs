using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ColosseumFirstEncounter : CustomEncounterModel
{
	public override bool ShouldGiveRewards => false;

	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[2] { "blue", "red" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<SlaverBlue>();
			yield return (MonsterModel)(object)ModelDb.Monster<SlaverRed>();
		}
	}

	public ColosseumFirstEncounter()
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
			(((MonsterModel)ModelDb.Monster<SlaverBlue>()).ToMutable(), "blue"),
			(((MonsterModel)ModelDb.Monster<SlaverRed>()).ToMutable(), "red")
		};
	}
}
