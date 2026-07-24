using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Relics;

[Obsolete]
public sealed class BrassTacks : HermitRelicModel
{
	public BrassTacks()
		: base((RelicRarity)2, autoAdd: false)
	{
		WithBlock(2);
	}

	public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (participants.Contains(((RelicModel)this).Owner.Creature))
		{
			((RelicModel)this).Flash();
			await CreatureCmd.GainBlock(((RelicModel)this).Owner.Creature, ((RelicModel)this).DynamicVars.Block, (CardPlay)null, false);
		}
	}
}
