using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace ActsFromThePast.Acts.TheBeyond.Encounters;

public sealed class JawWormHordeNormal : CustomEncounterModel
{
	public override bool HasScene => true;

	public override IReadOnlyList<string> Slots => new string[3] { "first", "second", "third" };

	public override IEnumerable<MonsterModel> AllPossibleMonsters
	{
		get
		{
			yield return (MonsterModel)(object)ModelDb.Monster<JawWorm>();
		}
	}

	public JawWormHordeNormal()
		: base((RoomType)1, true)
	{
	}

	public override bool IsValidForAct(ActModel act)
	{
		return act is TheBeyondAct;
	}

	protected override IReadOnlyList<(MonsterModel, string?)> GenerateMonsters()
	{
		JawWorm jawWorm = (JawWorm)(object)((MonsterModel)ModelDb.Monster<JawWorm>()).ToMutable();
		JawWorm jawWorm2 = (JawWorm)(object)((MonsterModel)ModelDb.Monster<JawWorm>()).ToMutable();
		JawWorm jawWorm3 = (JawWorm)(object)((MonsterModel)ModelDb.Monster<JawWorm>()).ToMutable();
		jawWorm.HardMode = true;
		jawWorm2.HardMode = true;
		jawWorm3.HardMode = true;
		return new List<(MonsterModel, string)>
		{
			((MonsterModel)(object)jawWorm, "first"),
			((MonsterModel)(object)jawWorm2, "second"),
			((MonsterModel)(object)jawWorm3, "third")
		};
	}
}
