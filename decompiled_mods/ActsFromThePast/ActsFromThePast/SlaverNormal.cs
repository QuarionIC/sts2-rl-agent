using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SlaverNormal : CustomEncounterModel
{
	private static MonsterModel[] Slavers => (MonsterModel[])(object)new MonsterModel[2]
	{
		(MonsterModel)ModelDb.Monster<SlaverRed>(),
		(MonsterModel)ModelDb.Monster<SlaverBlue>()
	};

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<SlaverRed>();
			yield return (MonsterModel)(object)ModelDb.Monster<SlaverBlue>();
		}
	}

	public SlaverNormal()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is ExordiumAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((EncounterModel)this).Rng.NextItem<MonsterModel>((IEnumerable<MonsterModel>)Slavers).ToMutable(), null) };
	}
}
