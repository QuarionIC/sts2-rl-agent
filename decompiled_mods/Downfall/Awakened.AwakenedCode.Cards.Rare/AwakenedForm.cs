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

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class AwakenedForm : AwakenedCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public AwakenedForm()
		: base(3, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithPower<CuriosityPower>(1, 0);
		((ConstructedCardModel)this).WithPower<RitualPower>(1, 1);
		((ConstructedCardModel)(object)this).WithTip(TooltipSource.op_Implicit(AwakenedTip.Awaken), (UpgradeType)1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).IsUpgraded)
		{
			await AwakenedCmd.Awaken(((CardModel)this).Owner, ctx);
		}
		await CommonActions.ApplySelf<CuriosityPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<RitualPower>(ctx, (CardModel)(object)this, false);
	}
}
