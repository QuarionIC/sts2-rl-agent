using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class SoulStrike : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Eudaimonia>();

	public SoulStrike()
		: base(3, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(4, 2);
		((ConstructedCardModel)(object)this).WithRepeat(3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitCount(((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue).Execute(ctx);
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if ((object)card != this || ((CardModel)this).IsClone)
		{
			return Task.CompletedTask;
		}
		ReduceCostBy(CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry e) => (int)e.CardPlay.Card.Type == 3 && e.CardPlay.Card.Owner == ((CardModel)this).Owner && ((CombatHistoryEntry)e).HappenedThisTurn(((CardModel)this).CombatState)));
		return Task.CompletedTask;
	}

	public override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (cardPlay.Card.Owner != ((CardModel)this).Owner || (int)cardPlay.Card.Type != 3)
		{
			return Task.CompletedTask;
		}
		ReduceCostBy(1);
		return Task.CompletedTask;
	}

	private void ReduceCostBy(int amount)
	{
		((CardModel)this).EnergyCost.AddThisTurn(-amount, false);
	}
}
