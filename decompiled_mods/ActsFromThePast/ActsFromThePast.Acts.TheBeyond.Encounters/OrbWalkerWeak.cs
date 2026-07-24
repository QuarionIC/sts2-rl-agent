using System.Collections.Generic;
using ActsFromThePast.Acts.TheBeyond.Enemies;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class OrbWalkerWeak : CustomEncounterModel
{
	public override bool IsWeak => true;

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<OrbWalker>();
		}
	}

	public override IEnumerable<string> ExtraAssetPaths => new string[1] { "res://scenes/vfx/vfx_fire_burst.tscn" };

	public OrbWalkerWeak()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheBeyondAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((MonsterModel)ModelDb.Monster<OrbWalker>()).ToMutable(), null) };
	}
}
