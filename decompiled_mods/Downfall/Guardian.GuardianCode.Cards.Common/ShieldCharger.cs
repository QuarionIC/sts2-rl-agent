using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Extensions;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Common;

[Pool(typeof(GuardianCardPool))]
public class ShieldCharger : GuardianCardModel, ITickCard
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public ShieldCharger()
		: base(2, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(10, 2);
		((ConstructedCardModel)this).WithKeyword(GuardianKeyword.Volatile, (UpgradeType)0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Stasis));
		((ConstructedCardModel)(object)this).WithBrace(4, 2);
	}

	public async Task OnTick(PlayerChoiceContext ctx)
	{
		await GuardianCmd.Brace(ctx, (CardModel)(object)this);
		await CommonActions.CardBlock((CardModel)(object)this, (CardPlay)null);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await GuardianCmd.PutIntoStasis((CardModel)(object)this, ctx, (AbstractModel)(object)this);
	}
}
