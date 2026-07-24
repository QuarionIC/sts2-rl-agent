using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Powers;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Uncommon;

[Pool(typeof(ChampCardPool))]
public class GutsAndGlory : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<GoofballMcgee>();

	public GutsAndGlory()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithGlory(4, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.ApplySelf<GloryPower>(ctx, (CardModel)(object)this, false);
	}
}
