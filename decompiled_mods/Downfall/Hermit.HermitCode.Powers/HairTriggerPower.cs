using System;
using System.Threading.Tasks;
using Hermit.HermitCode.Cards.Basic;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.CustomEnums;
using Hermit.HermitCode.Events;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Powers;

public class HairTriggerPower : HermitPowerModel, IAfterDeadOnTrigger
{
	public HairTriggerPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		WithTip((CardKeyword)1);
		WithTip(HermitKeywords.DeadOn);
		WithCardTip<StrikeHermit>((Action<StrikeHermit, PowerModel>?)delegate(StrikeHermit e, PowerModel _)
		{
			((CardModel)e).AddKeyword((CardKeyword)1);
		});
	}

	public async Task AfterDeadOnTrigger(PlayerChoiceContext ctx, CardModel card, CardPlay cardPlay)
	{
		if (card.Owner.Creature == ((PowerModel)this).Owner)
		{
			StrikeHermit strikeHermit = ModelDb.Card<StrikeHermit>();
			CardModel strike = ((PowerModel)this).CombatState.CreateCard((CardModel)(object)strikeHermit, card.Owner);
			strike.AddKeyword((CardKeyword)1);
			for (int i = 0; i < ((PowerModel)this).Amount; i++)
			{
				CardModel val = strike.CreateClone();
				await CardCmd.AutoPlay(ctx, val, (Creature)null, (AutoPlayType)1, false, false);
			}
		}
	}
}
