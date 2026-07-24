using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class MindBloomSlimeBoss : CustomEncounterModel
{
	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[7] { "spike_med_1", "spike_large", "spike_med_2", "acid_med_1", "slime_boss", "acid_large", "acid_med_2" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters => new List<MonsterModel>
	{
		(MonsterModel)(object)ModelDb.Monster<SlimeBoss>(),
		(MonsterModel)(object)ModelDb.Monster<SpikeSlimeLarge>(),
		(MonsterModel)(object)ModelDb.Monster<SpikeSlimeMedium>(),
		(MonsterModel)(object)ModelDb.Monster<AcidSlimeLarge>(),
		(MonsterModel)(object)ModelDb.Monster<AcidSlimeMedium>()
	};

	public MindBloomSlimeBoss()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return false;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		return new List<(MonsterModel, string)> { (((MonsterModel)ModelDb.Monster<SlimeBoss>()).ToMutable(), "slime_boss") };
	}
}
