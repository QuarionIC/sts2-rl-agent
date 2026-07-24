using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Extensions;
using SlimeBoss.SlimeBossCode.Slimes;

namespace SlimeBoss.SlimeBossCode.Cards.Common;

[Pool(typeof(SlimeBossCardPool))]
public class SplitBruiser : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public SplitBruiser()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithCommand(2m, 1m);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await SlimeBossCmd.Split<BruiserSlime>(ctx, ((CardModel)this).Owner);
		await SlimeBossCmd.Command(ctx, (CardModel)(object)this, (ValueProp)8);
	}
}
