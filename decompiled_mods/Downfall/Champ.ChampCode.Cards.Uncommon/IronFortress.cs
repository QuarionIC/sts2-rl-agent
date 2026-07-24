using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class IronFortress : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public IronFortress()
		: base(2, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<DexterityPower>(2, 0);
		((ConstructedCardModel)this).WithPower<MetallicizePower>(3, 2);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<DexterityPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<MetallicizePower>(ctx, (CardModel)(object)this, false);
	}
}
