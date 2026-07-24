using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class StrikeOfGenius : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public StrikeOfGenius()
		: base(2, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithEnergyTip();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(DownfallKeyword.Echo));
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)1 });
	}

	private static IHoverTip HoverTip(CardModel card)
	{
		if (!card.IsUpgraded)
		{
			return HoverTipFactory.FromPower<StrikeOfGeniusPower>((int?)null);
		}
		return HoverTipFactory.FromPower<StrikeOfGeniusPlusPower>((int?)null);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).IsUpgraded)
		{
			await CommonActions.ApplySelf<StrikeOfGeniusPlusPower>(ctx, (CardModel)(object)this, 1m, false);
		}
		else
		{
			await CommonActions.ApplySelf<StrikeOfGeniusPower>(ctx, (CardModel)(object)this, 1m, false);
		}
	}
}
