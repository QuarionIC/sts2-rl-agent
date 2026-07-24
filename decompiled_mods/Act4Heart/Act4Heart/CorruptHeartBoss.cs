using System.Collections.Generic;
using Dolso;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Act4Heart;

internal class CorruptHeartBoss : EncounterModel
{
	public override RoomType RoomType => (RoomType)3;

	public override IEnumerable<MonsterModel> AllPossibleMonsters => new _003C_003Ez__ReadOnlySingleElementList<MonsterModel>((MonsterModel)(object)ModelDb.Monster<CorruptHeart>());

	public override string BossNodePath => "res://images/map/placeholder/" + ((AbstractModel)this).Id.Entry.ToLowerInvariant() + "_icon";

	public override MegaSkeletonDataResource? BossNodeSpineResource => null;

	public override string CustomBgm => "event:/music/STS_Boss4_v6";

	public override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		ConfigSynchronizer.instance?.UpdateConfigServer();
		return new _003C_003Ez__ReadOnlySingleElementList<(MonsterModel, string)>((((MonsterModel)ModelDb.Monster<CorruptHeart>()).ToMutable(), null));
	}
}
