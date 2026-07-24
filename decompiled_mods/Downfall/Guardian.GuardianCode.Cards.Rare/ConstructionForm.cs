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

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class ConstructionForm : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public ConstructionForm()
		: base(3, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<BufferPower>(2, 0);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)2);
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
		((ConstructedCardModel)(object)this).WithPower<ConstructionFormPower>(1, showTooltip: false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<BufferPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<ConstructionFormPower>(ctx, (CardModel)(object)this, false);
	}
}
