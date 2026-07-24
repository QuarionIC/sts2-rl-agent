using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class RedMaskBanditsEvent : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

	public override bool IsWeak => false;

	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[3] { "pointy", "romeo", "bear" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Pointy>();
			yield return (MonsterModel)(object)ModelDb.Monster<Romeo>();
			yield return (MonsterModel)(object)ModelDb.Monster<Bear>();
		}
	}

	public RedMaskBanditsEvent()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return false;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)>
		{
			(((MonsterModel)ModelDb.Monster<Pointy>()).ToMutable(), "pointy"),
			(((MonsterModel)ModelDb.Monster<Romeo>()).ToMutable(), "romeo"),
			(((MonsterModel)ModelDb.Monster<Bear>()).ToMutable(), "bear")
		};
	}
}
