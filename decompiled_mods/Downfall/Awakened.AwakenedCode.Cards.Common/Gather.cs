using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.CustomEnums;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards.Common;

[Pool(typeof(AwakenedCardPool))]
public class Gather : AwakenedCardModel, IChantable
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public bool HasChanted { get; set; }

	public Gather()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(3, 3);
	}

	public async Task PlayChantEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		CardModel val = await CommonActions.SelectSingleCard((CardModel)(object)this, DownfallCardSelectorPrefs.ToHandSelectionPrompt, ctx, (PileType)3);
		if (val != null)
		{
			await CardPileCmd.Add(val, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
	}
}
