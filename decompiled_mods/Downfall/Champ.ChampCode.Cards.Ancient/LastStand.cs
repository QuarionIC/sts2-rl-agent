using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Cards.Ancient;

[Pool(typeof(ChampCardPool))]
public class LastStand : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public LastStand()
		: base(1, (CardType)3, (CardRarity)5, (TargetType)1)
	{
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
		((ConstructedCardModel)this).WithPower<StrengthPower>(6, 0);
		((ConstructedCardModel)(object)this).WithTip<WeakPower>();
		((ConstructedCardModel)(object)this).WithTip<VulnerablePower>();
		((ConstructedCardModel)(object)this).WithTip<FrailPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<StrengthPower>(ctx, (CardModel)(object)this, false);
		await PowerCmd.Remove<WeakPower>(((CardModel)this).Owner.Creature);
		await PowerCmd.Remove<VulnerablePower>(((CardModel)this).Owner.Creature);
		await PowerCmd.Remove<FrailPower>(((CardModel)this).Owner.Creature);
	}
}
