using System;
using System.Collections.Generic;
using ActsFromThePast.Acts.TheCity;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast;

public sealed class ChosenWeak : CustomEncounterModel
{
	public override IEnumerable<EncounterTag> Tags => Array.Empty<EncounterTag>();

	public override bool IsWeak => true;

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<Chosen>();
		}
	}

	public override IEnumerable<string> ExtraAssetPaths => new string[1] { "res://scenes/vfx/vfx_fire_burst.tscn" };

	public ChosenWeak()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheCityAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((MonsterModel)ModelDb.Monster<Chosen>()).ToMutable(), null) };
	}
}
