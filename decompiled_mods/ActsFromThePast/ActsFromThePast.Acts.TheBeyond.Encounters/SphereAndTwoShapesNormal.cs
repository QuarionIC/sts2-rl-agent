using System.Collections.Generic;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class SphereAndTwoShapesNormal : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => new _003C_003Ez__ReadOnlySingleElementList<EncounterTag>(CustomEncounterTags.Shapes);

	public override bool IsWeak => false;

	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[3] { "first", "second", "sphere" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Spiker>();
			yield return (MonsterModel)(object)ModelDb.Monster<Repulsor>();
			yield return (MonsterModel)(object)ModelDb.Monster<Exploder>();
			yield return (MonsterModel)(object)ModelDb.Monster<SphericGuardian>();
		}
	}

	public SphereAndTwoShapesNormal()
		: base((RoomType)1, true)
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
			(RandomShape(), "first"),
			(RandomShape(), "second"),
			(((MonsterModel)ModelDb.Monster<SphericGuardian>()).ToMutable(), "sphere")
		};
	}

	private MonsterModel RandomShape()
	{
		int num = ((EncounterModel)this).Rng.NextInt(3);
		if (1 == 0)
		{
		}
		MonsterModel result = (MonsterModel)(num switch
		{
			0 => ((MonsterModel)ModelDb.Monster<Spiker>()).ToMutable(), 
			1 => ((MonsterModel)ModelDb.Monster<Repulsor>()).ToMutable(), 
			_ => ((MonsterModel)ModelDb.Monster<Exploder>()).ToMutable(), 
		});
		if (1 == 0)
		{
		}
		return result;
	}
}
