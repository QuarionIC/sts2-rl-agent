using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards.Uncommon;

public sealed class DeadMansHand : HermitCardModel
{
	private const int DrawCount = 3;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public DeadMansHand()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCards(3, 0);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	private static int RarityLevel(CardRarity rarity)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Expected I4, but got Unknown
		return (rarity - 3) switch
		{
			2 => 3, 
			1 => 2, 
			0 => 1, 
			_ => 0, 
		};
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay play)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		IReadOnlyList<CardModel> hand = ((CardModel)this).Owner.GetHand();
		if (hand.Count > 0)
		{
			await CardCmd.Discard(ctx, (IEnumerable<CardModel>)hand);
		}
		await CardPileCmd.Add((IEnumerable<CardModel>)(from c in ((CardModel)this).Owner.GetDraw()
			orderby RarityLevel(c.Rarity) descending
			select c).Take(3).ToList(), (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
	}
}
