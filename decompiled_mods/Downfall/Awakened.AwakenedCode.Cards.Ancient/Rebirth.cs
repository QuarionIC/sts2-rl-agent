using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Powers;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Awakened.AwakenedCode.Cards.Ancient;

[Pool(typeof(AwakenedCardPool))]
public class Rebirth : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Rebirth()
		: base(1, (CardType)3, (CardRarity)5, (TargetType)1)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithPower<AwakeningPower>(8, 3, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<VulnerablePower>();
		((ConstructedCardModel)(object)this).WithTip<WeakPower>();
		((ConstructedCardModel)(object)this).WithTip<FrailPower>();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AwakenedTip.Awaken));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<AwakeningPower>(ctx, (CardModel)(object)this, false);
	}
}
