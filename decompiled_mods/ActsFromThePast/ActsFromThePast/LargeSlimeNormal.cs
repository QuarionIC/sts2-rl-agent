using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class LargeSlimeNormal : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => new _003C_003Ez__ReadOnlySingleElementList<EncounterTag>((EncounterTag)5);

	public override bool IsWeak => false;

	private static MonsterModel[] LargeSlimes => (MonsterModel[])(object)new MonsterModel[2]
	{
		(MonsterModel)ModelDb.Monster<AcidSlimeLarge>(),
		(MonsterModel)ModelDb.Monster<SpikeSlimeLarge>()
	};

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<AcidSlimeLarge>();
			yield return (MonsterModel)(object)ModelDb.Monster<SpikeSlimeLarge>();
			yield return (MonsterModel)(object)ModelDb.Monster<AcidSlimeMedium>();
			yield return (MonsterModel)(object)ModelDb.Monster<SpikeSlimeMedium>();
		}
	}

	public LargeSlimeNormal()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is ExordiumAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((EncounterModel)this).Rng.NextItem<MonsterModel>((IEnumerable<MonsterModel>)LargeSlimes).ToMutable(), null) };
	}
}
