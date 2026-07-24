using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;

namespace Awakened.AwakenedCode.Cards.Common;

[Pool(typeof(AwakenedCardPool))]
public class Gloomguard : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public override bool ShouldReceiveCombatHooks => true;

	protected override bool ShouldGlowGoldInternal => HasVoidInHand();

	public Gloomguard()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(8, 3);
		((ConstructedCardModel)this).WithEnergyTip();
		((ConstructedCardModel)(object)this).WithTip<Void>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}

	private bool HasVoidInHand()
	{
		return ((CardModel)this).Owner.GetHand().Any((CardModel e) => ((AbstractModel)e).Id == ((AbstractModel)ModelDb.Card<Void>()).Id);
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		if ((object)card == this && HasVoidInHand())
		{
			modifiedCost = default(decimal);
			return true;
		}
		modifiedCost = originalCost;
		return false;
	}

	public override Task AfterCardChangedPiles(CardModel card, PileType oldPileType, AbstractModel? source)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Invalid comparison between Unknown and I4
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		if (card.Owner == ((CardModel)this).Owner)
		{
			if ((int)oldPileType != 2)
			{
				CardPile pile = card.Pile;
				if (pile == null || (int)pile.Type != 2)
				{
					goto IL_002f;
				}
			}
			((CardModel)this).InvokeEnergyCostChanged();
		}
		goto IL_002f;
		IL_002f:
		return Task.CompletedTask;
	}
}
