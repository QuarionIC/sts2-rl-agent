using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class DeadAdventurerSentries : CustomEncounterModel
{
	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Sentry>();
		}
	}

	public DeadAdventurerSentries()
		: base((RoomType)2, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return false;
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
