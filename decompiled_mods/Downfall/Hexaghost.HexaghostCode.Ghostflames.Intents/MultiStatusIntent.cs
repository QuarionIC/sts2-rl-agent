using System;
using System.Collections.Generic;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Hexaghost.HexaghostCode.Ghostflames.Intents;

public class MultiStatusIntent<T>(Func<int> amount, int repeat) : CustomIntent() where T : PowerModel
{
	public override IntentType IntentType => (IntentType)2;

	protected override string IntentSpritePath => ((PowerModel)ModelDb.Power<T>()).PackedIconPath;

	public override LocString GetIntentLabel(IEnumerable<Creature> targets, Creature owner)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		LocString val = new LocString("intents", "FORMAT_DAMAGE_MULTI");
		val.Add("Damage", (decimal)_003Camount_003EP());
		val.Add("Repeat", (decimal)_003Crepeat_003EP);
		return val;
	}
}
