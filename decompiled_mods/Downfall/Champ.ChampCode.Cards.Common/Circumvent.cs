using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Common;

[Pool(typeof(ChampCardPool))]
public class Circumvent : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	protected override bool ShouldGlowRedInternal => !((CardModel)this).Owner.ShouldDefensiveComboTrigger();

	public Circumvent()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(6, 3);
		((ConstructedCardModel)this).WithCards(2, 0);
		((ConstructedCardModel)(object)this).WithDefensiveTip();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.Draw((CardModel)(object)this, ctx);
		if (!((CardModel)this).Owner.ShouldDefensiveComboTrigger())
		{
			CardSelectorPrefs val = default(CardSelectorPrefs);
			((CardSelectorPrefs)(ref val))._002Ector(CardSelectorPrefs.DiscardSelectionPrompt, ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue);
			await CardCmd.Discard(ctx, await CardSelectCmd.FromHandForDiscard(ctx, ((CardModel)this).Owner, val, (Func<CardModel, bool>)null, (AbstractModel)(object)this));
		}
	}
}
