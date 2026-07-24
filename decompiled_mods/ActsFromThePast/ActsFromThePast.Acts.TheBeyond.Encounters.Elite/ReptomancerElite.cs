using System.Collections.Generic;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters.Elite;

public sealed class ReptomancerElite : CustomEncounterModel
{
	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[5] { "dagger3", "dagger1", "reptomancer", "dagger2", "dagger4" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Reptomancer>();
			yield return (MonsterModel)(object)ModelDb.Monster<SnakeDagger>();
		}
	}

	public ReptomancerElite()
		: base((RoomType)2, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheBeyondAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)>
		{
			(((MonsterModel)ModelDb.Monster<SnakeDagger>()).ToMutable(), "dagger3"),
			(((MonsterModel)ModelDb.Monster<SnakeDagger>()).ToMutable(), "dagger4"),
			(((MonsterModel)ModelDb.Monster<Reptomancer>()).ToMutable(), "reptomancer")
		};
	}
}
