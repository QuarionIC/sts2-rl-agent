using System.Collections.Generic;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public class DarklingsWeak : CustomEncounterModel
{
	public override bool IsWeak => true;

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Darkling>();
		}
	}

	public DarklingsWeak()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheBeyondAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		List<(MonsterModel, string)> list = new List<(MonsterModel, string)>();
		for (int i = 0; i < 3; i++)
		{
			Darkling darkling = (Darkling)(object)((MonsterModel)ModelDb.Monster<Darkling>()).ToMutable();
			darkling.SlotIndex = i;
			list.Add(((MonsterModel)(object)darkling, null));
		}
		return list;
	}
}
