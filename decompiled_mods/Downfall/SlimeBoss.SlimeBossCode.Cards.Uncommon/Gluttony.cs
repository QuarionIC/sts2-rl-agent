using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Cards.Token;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;
using SlimeBoss.SlimeBossCode.Powers;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class Gluttony : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public Gluttony()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithPower<GluttonyPower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)3, (UpgradeType)1);
		((ConstructedCardModel)(object)this).WithTip<Lick>();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SlimeBossTip.Consume));
	}

	protected override Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return CommonActions.ApplySelf<GluttonyPower>(ctx, (CardModel)(object)this, false);
	}
}
