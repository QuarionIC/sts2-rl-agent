using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SmallSlimesWeak : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => new _003C_003Ez__ReadOnlySingleElementList<EncounterTag>((EncounterTag)5);

	public override bool IsWeak => true;

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<SpikeSlimeSmall>();
			yield return (MonsterModel)(object)ModelDb.Monster<AcidSlimeSmall>();
			yield return (MonsterModel)(object)ModelDb.Monster<SpikeSlimeMedium>();
			yield return (MonsterModel)(object)ModelDb.Monster<AcidSlimeMedium>();
		}
	}

	public SmallSlimesWeak()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is ExordiumAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		if (((EncounterModel)this).Rng.NextInt(2) == 0)
		{
			return new List<(MonsterModel, string)>
			{
				(((MonsterModel)ModelDb.Monster<SpikeSlimeSmall>()).ToMutable(), null),
				(((MonsterModel)ModelDb.Monster<AcidSlimeMedium>()).ToMutable(), null)
			};
		}
		return new List<(MonsterModel, string)>
		{
			(((MonsterModel)ModelDb.Monster<AcidSlimeSmall>()).ToMutable(), null),
			(((MonsterModel)ModelDb.Monster<SpikeSlimeMedium>()).ToMutable(), null)
		};
	}
}
