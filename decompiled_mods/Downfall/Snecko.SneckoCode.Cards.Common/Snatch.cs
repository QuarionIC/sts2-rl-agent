using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
[Obsolete]
public class Snatch : SneckoCardModel, IHasOverflowEffect
{
	public Snatch()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1, showInCardLibrary: false)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		((ConstructedCardModel)(object)this).WithOverflow();
		((ConstructedCardModel)this).WithCards(1, 1);
		((ConstructedCardModel)this).WithVar((DynamicVar)new CardsVar("OverflowCards", 1));
	}

	public async Task OverflowEffect(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CardPileCmd.Draw(ctx, ((CardModel)this).DynamicVars["OverflowCards"].BaseValue, ((CardModel)this).Owner, false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Draw((CardModel)(object)this, ctx);
	}
}
