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

namespace Guardian.GuardianCode.Cards.Ancient;

[Pool(typeof(GuardianCardPool))]
public class AncientConstruct : GuardianCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public AncientConstruct()
		: base(3, (CardType)3, (CardRarity)5, (TargetType)1)
	{
		((ConstructedCardModel)this).WithPower<ArtifactPower>(1, 0);
		((ConstructedCardModel)(object)this).WithPower<AncientConstructPower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<ArtifactPower>(ctx, (CardModel)(object)this, false);
		await CommonActions.ApplySelf<AncientConstructPower>(ctx, (CardModel)(object)this, false);
	}
}
