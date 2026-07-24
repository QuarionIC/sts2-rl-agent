using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace Hexaghost.HexaghostCode.Cards.Common;

[Pool(typeof(HexaghostCardPool))]
public class StokeTheFire : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<CartesianCanvas>();

	public StokeTheFire()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		((ConstructedCardModel)this).WithBlock(7, 3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		int ignitedCount = HexaghostCmd.GetIgnitedCount(((CardModel)this).Owner);
		if (ignitedCount == 0 || ((CardModel)this).CombatState == null)
		{
			return;
		}
		foreach (CardModel item in IEnumerableExtensions.TakeRandom<CardModel>(from e in ((CardModel)this).Owner.GetHand()
			where e.IsUpgradable
			select e, ignitedCount, ((CardModel)this).CombatState.RunState.Rng.CombatCardSelection))
		{
			CardCmd.Upgrade(item, (CardPreviewStyle)1);
		}
	}
}
