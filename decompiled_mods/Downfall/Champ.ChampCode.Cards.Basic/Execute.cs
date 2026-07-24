using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Cards.Ancient;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Basic;

[Pool(typeof(ChampCardPool))]
public class Execute : ChampCardModel, ITranscendenceCard
{
	public Execute()
		: base(2, (CardType)1, (CardRarity)1, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)(object)this).WithFinisher();
	}

	public CardModel GetTranscendenceTransformedCard()
	{
		return (CardModel)(object)ModelDb.Card<Execution>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitCount(2).Execute(ctx);
	}
}
