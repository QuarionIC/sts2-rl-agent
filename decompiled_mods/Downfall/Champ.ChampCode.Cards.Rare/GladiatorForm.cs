using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class GladiatorForm : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public GladiatorForm()
		: base(3, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithPower<GladiatorFormPower>(1, showTooltip: false);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<GladiatorFormPower>(ctx, (CardModel)(object)this, false);
	}
}
