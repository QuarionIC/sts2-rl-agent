using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Extensions.Cards;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class DarknessFalls : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public DarknessFalls()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Expected O, but got Unknown
		((ConstructedCardModel)this).WithTip(AwakenedTip.Drained.WithVars((DynamicVar)new EnergyVar(1)));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)5));
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
		((ConstructedCardModel)(object)this).WithPower<DarknessFallsPower>(4, showTooltip: false);
		((ConstructedCardModel)(object)this).WithPower<DarkblessedPower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)3, (UpgradeType)1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<DarknessFallsPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<DarkblessedPower>(ctx, (CardModel)(object)this, false);
	}
}
