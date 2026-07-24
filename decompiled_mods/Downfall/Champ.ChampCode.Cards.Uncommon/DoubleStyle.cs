using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class DoubleStyle : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public DoubleStyle()
		: base(2, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<DefensiveStylePower>(1, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<BerserkerStylePower>(1, 1, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<VigorPower>();
		((ConstructedCardModel)(object)this).WithTip<CounterPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<DefensiveStylePower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<BerserkerStylePower>(ctx, (CardModel)(object)this, false);
	}
}
