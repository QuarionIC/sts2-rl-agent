using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Hexaghost.HexaghostCode.Cards.Rare;

[Pool(typeof(TokenCardPool))]
public abstract class BurningQuestionChoiceBase : HexaghostCardModel
{
	public override CardPoolModel VisualCardPool => (CardPoolModel)(object)ModelDb.CardPool<HexaghostCardPool>();

	public override string CustomPortraitPath => ((CustomCardModel)ModelDb.Card<BurningQuestion>()).CustomPortraitPath;

	protected BurningQuestionChoiceBase()
		: base(-1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
	}

	public abstract Task OnSelect(PlayerChoiceContext ctx, CardPlay cardPlay);
}
