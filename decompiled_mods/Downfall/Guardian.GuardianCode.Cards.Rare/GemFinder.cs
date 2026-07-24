using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Powers;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class GemFinder : GuardianCardModel
{
	protected override bool HasEnergyCostX => true;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public GemFinder()
		: base(0, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianKeyword.Gem));
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Brace));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		int num = ((CardModel)this).ResolveEnergyXValue();
		if (((CardModel)this).IsUpgraded)
		{
			num++;
		}
		await CommonActions.ApplySelf<GemFinderPower>(ctx, (CardModel)(object)this, (decimal)num, false);
	}
}
