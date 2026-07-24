using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class SharpHide : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public SharpHide()
		: base(1, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<DexterityPower>(1, 0);
		((ConstructedCardModel)(object)this).WithPower<SharpHidePower>(2, 2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<ThornsPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<DexterityPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<SharpHidePower>(ctx, (CardModel)(object)this, false);
	}
}
