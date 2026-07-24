using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Guardian.GuardianCode.Cards.Uncommon;

[Pool(typeof(GuardianCardPool))]
public class Orbwalk : GuardianCardModel, ITickCard
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Bukie>();

	public Orbwalk()
		: base(2, (CardType)3, (CardRarity)3, (TargetType)1)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithPower<StrengthPower>(3, 0);
		((ConstructedCardModel)this).WithKeyword(GuardianKeyword.Volatile, (UpgradeType)2);
	}

	public async Task OnTick(PlayerChoiceContext ctx)
	{
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, 1m, false);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
	}
}
