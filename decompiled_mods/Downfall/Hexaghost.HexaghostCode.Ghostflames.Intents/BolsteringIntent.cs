using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;

namespace Hexaghost.HexaghostCode.Ghostflames.Intents;

public class BolsteringIntent : CustomIntent
{
	public override IntentType IntentType => (IntentType)2;

	protected override string IntentSpritePath => ((PowerModel)ModelDb.Power<StrengthPower>()).PackedIconPath;
}
