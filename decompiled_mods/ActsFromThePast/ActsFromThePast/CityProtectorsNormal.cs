using System;
using System.Collections.Generic;
using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class CityProtectorsNormal : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Sentry>();
			yield return (MonsterModel)(object)ModelDb.Monster<SphericGuardian>();
		}
	}

	public CityProtectorsNormal()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheCityAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		Sentry sentry = (Sentry)(object)((MonsterModel)ModelDb.Monster<Sentry>()).ToMutable();
		sentry.BoltFirst = true;
		return new List<(MonsterModel, string)>
		{
			((MonsterModel)(object)sentry, null),
			(((MonsterModel)ModelDb.Monster<SphericGuardian>()).ToMutable(), null)
		};
	}
}
