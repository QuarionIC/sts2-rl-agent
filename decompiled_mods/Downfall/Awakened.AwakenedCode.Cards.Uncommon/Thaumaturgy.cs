using System.Threading.Tasks;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Cards.Uncommon;

[Pool(typeof(AwakenedCardPool))]
public class Thaumaturgy : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Thaumaturgy()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<DexterityPower>(1, 1);
		((ConstructedCardModel)(object)this).WithPower<ThaumaturgyPower>(2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<Ceremony>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<DexterityPower>(ctx, (CardModel)(object)this, ((DynamicVar)((CardModel)this).DynamicVars.Dexterity).BaseValue, false);
		await CommonActions.ApplySelf<ThaumaturgyPower>(ctx, (CardModel)(object)this, false);
	}
}
