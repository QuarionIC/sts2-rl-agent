using System;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class Undervolt : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Undervolt()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)3)
	{
		((ConstructedCardModel)this).WithPower<StrengthPower>(-2, -1);
		((ConstructedCardModel)this).WithVar("StrengthLoss", 2, 1);
		((ConstructedCardModel)(object)this).WithTip<Burn>();
		((ConstructedCardModel)this).WithCards(2, 0);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.Apply<StrengthPower>(ctx, (CardModel)(object)this, cardPlay, false);
		await DownfallCardCmd.GiveCards<Burn>(((CardModel)this).Owner, (PileType)2, (decimal)((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, (CardPilePosition)1, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Burn>?)null, (Player?)null);
	}
}
