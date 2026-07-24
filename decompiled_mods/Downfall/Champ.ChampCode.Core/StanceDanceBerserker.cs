using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Cards;
using Champ.ChampCode.Cards.Basic;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Champ.ChampCode.Core;

[Pool(typeof(TokenCardPool))]
public class StanceDanceBerserker : ChampCardModel
{
	public override string CustomPortraitPath => ((CustomCardModel)ModelDb.Card<BerserkersShout>()).CustomPortraitPath;

	public StanceDanceBerserker()
		: base(-1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
	}
}
