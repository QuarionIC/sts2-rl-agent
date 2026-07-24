using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Act4Heart;

internal class SpireShieldAndSpireSpearElite : EncounterModel
{
	public override RoomType RoomType => (RoomType)2;

	public override bool FullyCenterPlayers => true;

	public override bool HasScene => true;

	public override IEnumerable<MonsterModel> AllPossibleMonsters => new _003C_003Ez__ReadOnlyArray<MonsterModel>((MonsterModel[])(object)new MonsterModel[2]
	{
		ModelDb.Monster<SpireShield>(),
		ModelDb.Monster<SpireSpear>()
	});

	public override IReadOnlyList<string> Slots => new _003C_003Ez__ReadOnlyArray<string>(new string[2] { "shield", "spear" });

	public override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new _003C_003Ez__ReadOnlyArray<(MonsterModel, string)>(new(MonsterModel, string)[2]
		{
			(((MonsterModel)ModelDb.Monster<SpireShield>()).ToMutable(), "shield"),
			(((MonsterModel)ModelDb.Monster<SpireSpear>()).ToMutable(), "spear")
		});
	}

	public override float GetCameraScaling()
	{
		return 0.9f;
	}
}
