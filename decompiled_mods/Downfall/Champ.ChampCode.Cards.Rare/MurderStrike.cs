using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class MurderStrike : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public MurderStrike()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)this).WithVar("Increase", 2, 1);
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)1 });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
	}

	public override Task AfterCardPlayed(PlayerChoiceContext context, CardPlay cardPlay)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Invalid comparison between Unknown and I4
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		if (cardPlay.Card.Owner == ((CardModel)this).Owner && (int)cardPlay.Card.Type == 2)
		{
			CardPile pile = ((CardModel)this).Pile;
			if (pile != null && (int)pile.Type == 2)
			{
				((DynamicVar)((CardModel)this).DynamicVars.Damage).UpgradeValueBy((decimal)((CardModel)this).DynamicVars["Increase"].IntValue);
				return Task.CompletedTask;
			}
		}
		return Task.CompletedTask;
	}
}
