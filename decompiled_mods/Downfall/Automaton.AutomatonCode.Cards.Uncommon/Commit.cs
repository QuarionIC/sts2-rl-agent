using System;
using System.Linq;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Commit : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	protected override bool ShouldGlowGoldInternal => WasLastCardPlayedFunction;

	private bool WasLastCardPlayedFunction
	{
		get
		{
			CardPlayStartedEntry? obj = CombatManager.Instance.History.CardPlaysStarted.LastOrDefault((Func<CardPlayStartedEntry, bool>)((CardPlayStartedEntry e) => e.CardPlay.Card.Owner == ((CardModel)this).Owner && (object)e.CardPlay.Card != this));
			return ((obj != null) ? obj.CardPlay.Card : null) is FunctionCard;
		}
	}

	public Commit()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		((ConstructedCardModel)this).WithBlock(6, 2);
		((ConstructedCardModel)this).WithDamage(6, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		if ((object)card == this && WasLastCardPlayedFunction)
		{
			modifiedCost = default(decimal);
			return true;
		}
		modifiedCost = originalCost;
		return false;
	}
}
