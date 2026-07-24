using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Hermit.HermitCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class ShortFuse : HermitCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public ShortFuse()
		: base(3, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(13, 4);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Attack", ((CardModel)this).Owner.Character.AttackAnimDelay);
		await CommonActions.CardAttack((CardModel)(object)this, play, 1, (string)null, (string)null, (string)null).WithHermitShortFuseHitFx().Execute(ctx);
	}

	public override Task AfterCardEnteredCombat(CardModel card)
	{
		if ((object)card != this || ((CardModel)this).IsClone)
		{
			return Task.CompletedTask;
		}
		int amount = CombatManager.Instance.History.CardPlaysFinished.Count((CardPlayFinishedEntry e) => (int)e.CardPlay.Card.Rarity == 1 && e.CardPlay.Card.Owner == ((CardModel)this).Owner && ((CombatHistoryEntry)e).HappenedThisTurn(((CardModel)this).CombatState));
		ReduceCostBy(amount);
		return Task.CompletedTask;
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		if (cardPlay.Card.Owner != ((CardModel)this).Owner || (int)cardPlay.Card.Rarity != 1)
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
