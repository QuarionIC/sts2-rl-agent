using System;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Status;
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
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Automaton.AutomatonCode.Cards.Rare;

[Pool(typeof(AutomatonCardPool))]
public class ProtoShield : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public ProtoShield()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(8, 2);
		((ConstructedCardModel)this).WithPower<PlatingPower>(2, 1);
		((ConstructedCardModel)(object)this).WithTip<Error>();
		((ConstructedCardModel)this).WithCards(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CommonActions.ApplySelf<PlatingPower>(ctx, (CardModel)(object)this, false);
		await DownfallCardCmd.GiveCards<Error>(((CardModel)this).Owner, (PileType)3, (decimal)((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, (CardPilePosition)3, upgraded: false, 0.6f, (CardPreviewStyle)1, skipAnimation: false, (Action<Error>?)null, (Player?)null);
	}
}
