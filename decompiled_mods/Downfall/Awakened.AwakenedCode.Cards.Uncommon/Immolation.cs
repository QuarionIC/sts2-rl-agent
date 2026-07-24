using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Events;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class Immolation : AwakenedCardModel, IOnDrained
{
	protected override Artist? Artist => Downfall.DownfallCode.Artists.Artist.Get<Chimedragon>();

	public Immolation()
		: base(3, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		((ConstructedCardModel)this).WithBlock(13, 4);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
		((ConstructedCardModel)this).WithTip(AwakenedTip.Drained.WithVars((DynamicVar)new EnergyVar(1)));
	}

	public Task OnDrained(PlayerChoiceContext ctx, Player player, int amount)
	{
		if (player == ((CardModel)this).Owner)
		{
			((CardModel)this).EnergyCost.AddUntilPlayed(-amount, false);
		}
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
