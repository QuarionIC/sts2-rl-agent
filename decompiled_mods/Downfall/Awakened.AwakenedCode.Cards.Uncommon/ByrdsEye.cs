using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Extensions;
using Awakened.AwakenedCode.Piles;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class ByrdsEye : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public ByrdsEye()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithConjure();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		AwakenedPile orInitSpellbook = AwakenedModel.GetOrInitSpellbook(((CardModel)this).Owner);
		if (((CardModel)this).IsUpgraded)
		{
			orInitSpellbook.Refresh(((CardModel)this).Owner);
		}
		IReadOnlyList<CardModel> cards = ((CardPile)orInitSpellbook).Cards;
		CardModel val = (await DownfallCardCmd.SelectFromCards(ctx, cards, DownfallCardSelectorPrefs.ConjureSelectionPrompt, (CardModel)(object)this)).FirstOrDefault();
		if (val != null)
		{
			await AwakenedCmd.ConjureSelected(((CardModel)this).Owner, (CardModel)(object)this, val);
		}
	}
}
