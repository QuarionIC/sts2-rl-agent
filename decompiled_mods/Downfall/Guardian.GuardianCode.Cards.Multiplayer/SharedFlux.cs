using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.CustomEnums;
using Guardian.GuardianCode.Core;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Multiplayer;

[Pool(typeof(GuardianCardPool))]
public class SharedFlux : GuardianCardModel
{
	public override CardMultiplayerConstraint MultiplayerConstraint => (CardMultiplayerConstraint)1;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<AlexMdle>();

	public SharedFlux()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)6)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		Creature target = cardPlay.Target;
		Player val = ((target != null) ? target.Player : null);
		if (val != null && GuardianCmd.CanPutIntoStasis(val, ((CardModel)this).Owner))
		{
			CardModel val2 = (await CardSelectCmd.FromCombatPile(ctx, PileTypeExtensions.GetPile((PileType)1, val), val, new CardSelectorPrefs(DownfallCardSelectorPrefs.StasisSelectionPrompt, 0, 1))).FirstOrDefault();
			if (val2 != null)
			{
				await GuardianCmd.PutIntoStasis(val2, ctx, (AbstractModel)(object)this);
			}
		}
	}
}
