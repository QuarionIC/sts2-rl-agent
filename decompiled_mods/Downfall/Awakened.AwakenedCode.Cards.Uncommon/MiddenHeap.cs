using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.CustomEnums;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class MiddenHeap : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Occultpyromancer>();

	public MiddenHeap()
		: base(0, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(3, 1);
		((ConstructedCardModel)this).WithCards(1, 1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		List<CardModel> cards = ((CardModel)this).Owner.GetDiscard().Concat(((CardModel)this).Owner.GetDraw()).Where(delegate(CardModel c)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			CardType type = c.Type;
			return type - 4 <= 1;
		})
			.ToList();
		foreach (CardModel item in await DownfallCardCmd.SelectFromCards(ctx, cards, DownfallCardSelectorPrefs.ToHandSelectionPrompt, (CardModel)(object)this))
		{
			await CardPileCmd.Add(item, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}
}
