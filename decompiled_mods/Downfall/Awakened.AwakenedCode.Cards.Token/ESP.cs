using System.Threading.Tasks;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Events;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Interfaces;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Awakened.AwakenedCode.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class ESP : AwakenedCardModel, ISpell, IOnAwaken, ICustomTypePlaque
{
	public LocString GetTypePlaqueName => new LocString("gameplay_ui", "AWAKENED-SPELL");

	public ESP()
		: base(0, (CardType)2, (CardRarity)7, (TargetType)1)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithCards(1, 1);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[2]
		{
			(CardKeyword)1,
			(CardKeyword)5
		});
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)AwakenedTag.Spell });
	}

	public Task OnAwaken(PlayerChoiceContext ctx, Player player)
	{
		if (player != ((CardModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		CardCmd.Upgrade((CardModel)(object)this, (CardPreviewStyle)0);
		return Task.CompletedTask;
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CardPileCmd.Draw(ctx, ((DynamicVar)((CardModel)this).DynamicVars.Cards).BaseValue, cardPlay.Card.Owner, false);
	}
}
