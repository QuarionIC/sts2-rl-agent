using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Stance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Relics;

[Pool(typeof(ChampRelicPool))]
public class DolphinsStyleGuide : ChampRelicModel
{
	public DolphinsStyleGuide()
		: base((RelicRarity)3)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		WithTip(ChampTip.Stance);
	}

	public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		Creature creature = ((RelicModel)this).Owner.Creature;
		if (side == creature.Side && ((RelicModel)this).Owner.IsInChampStance<ChampNoStance>())
		{
			await PowerCmd.Apply<DrawCardsNextTurnPower>(ctx, creature, 1m, creature, (CardModel)null, false);
		}
	}
}
