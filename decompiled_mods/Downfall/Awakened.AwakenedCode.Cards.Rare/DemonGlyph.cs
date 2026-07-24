using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class DemonGlyph : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public DemonGlyph()
		: base(1, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithPower<StrengthPower>(1, 0);
		((ConstructedCardModel)this).WithPower<DexterityPower>(1, 0);
		((ConstructedCardModel)(object)this).WithPower<DemonGlyphPower>(2, 1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AwakenedTip.Awaken));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<DexterityPower>(ctx, (CardModel)(object)this, false);
		if (AwakenedModel.IsAwakened(((CardModel)this).Owner))
		{
			decimal count = DynamicVarSetExtensions.Power<DemonGlyphPower>(((CardModel)this).DynamicVars).BaseValue;
			await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, count, false);
			await CommonActions.ApplySelf<DexterityPower>(ctx, (CardModel)(object)this, count, false);
		}
		else
		{
			await CommonActions.ApplySelf<DemonGlyphPower>(ctx, (CardModel)(object)this, false);
		}
	}
}
