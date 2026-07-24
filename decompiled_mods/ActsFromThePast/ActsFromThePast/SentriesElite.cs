using System.Collections.Generic;
using ActsFromThePast.Acts;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class SentriesElite : CustomEncounterModel
{
	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Sentry>();
		}
	}

	public SentriesElite()
		: base((RoomType)2, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is ExordiumAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		Sentry sentry = (Sentry)(object)((MonsterModel)ModelDb.Monster<Sentry>()).ToMutable();
		Sentry sentry2 = (Sentry)(object)((MonsterModel)ModelDb.Monster<Sentry>()).ToMutable();
		Sentry sentry3 = (Sentry)(object)((MonsterModel)ModelDb.Monster<Sentry>()).ToMutable();
		sentry.BoltFirst = true;
		sentry2.BoltFirst = false;
		sentry3.BoltFirst = true;
		return new List<(MonsterModel, string)>
		{
			((MonsterModel)(object)sentry, null),
			((MonsterModel)(object)sentry2, null),
			((MonsterModel)(object)sentry3, null)
		};
	}
}
