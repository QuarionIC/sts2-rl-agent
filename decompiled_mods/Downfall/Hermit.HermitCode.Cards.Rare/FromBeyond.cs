using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Rare;

public sealed class FromBeyond : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public FromBeyond()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)4)
	{
		((ConstructedCardModel)(object)this).WithHpLoss(5, 2);
		((ConstructedCardModel)this).WithCalculatedVar("CalculatedHits", 0, (Func<CardModel, Creature, decimal>)CountCardsInExhaust, 0, 0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
	}

	private static decimal CountCardsInExhaust(CardModel card, Creature? _)
	{
		return card.Owner.GetExhaust().Count;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		decimal exhaustCount = ((CalculatedVar)((CardModel)this).DynamicVars["CalculatedHits"]).Calculate((Creature)null);
		for (int i = 0; (decimal)i < exhaustCount; i++)
		{
			Creature val = ((CardModel)this).Owner.RunState.Rng.CombatTargets.NextItem<Creature>((IEnumerable<Creature>)((CardModel)this).CombatState.HittableEnemies);
			if (val != null)
			{
				HermitCombatFx.GroundFireOnTarget(val);
				await MyCommonActions.LoseHpToTarget(ctx, (AbstractModel)(object)this, val);
				continue;
			}
			break;
		}
	}
}
