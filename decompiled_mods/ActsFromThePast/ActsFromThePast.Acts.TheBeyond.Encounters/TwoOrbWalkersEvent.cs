using System.Collections.Generic;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public class TwoOrbWalkersEvent : CustomEncounterModel
{
	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[2] { "left", "right" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<OrbWalker>();
		}
	}

	public override IEnumerable<string> ExtraAssetPaths => new string[1] { "res://scenes/vfx/vfx_fire_burst.tscn" };

	public TwoOrbWalkersEvent()
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
			(((MonsterModel)ModelDb.Monster<OrbWalker>()).ToMutable(), "left"),
			(((MonsterModel)ModelDb.Monster<OrbWalker>()).ToMutable(), "right")
		};
	}
}
