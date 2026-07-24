using System.Collections.Generic;
using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class BronzeAutomatonBoss : CustomEncounterModel
{
	public override string BossNodePath => "res://ActsFromThePast/map_boss_icons/bronze_automaton";

	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[3] { "orb1", "automaton", "orb2" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<BronzeAutomaton>();
			yield return (MonsterModel)(object)ModelDb.Monster<BronzeOrb>();
		}
	}

	public BronzeAutomatonBoss()
		: base((RoomType)3, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheCityAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((MonsterModel)ModelDb.Monster<BronzeAutomaton>()).ToMutable(), "automaton") };
	}
}
