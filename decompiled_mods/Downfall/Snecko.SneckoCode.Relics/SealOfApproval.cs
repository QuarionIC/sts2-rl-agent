using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Relics;

[Pool(typeof(SneckoRelicPool))]
public class SealOfApproval : SneckoRelicModel
{
	public override bool HasUponPickupEffect => true;

	public SealOfApproval()
		: base((RelicRarity)2)
	{
	}

	public override async Task AfterObtained()
	{
		List<CardModel> list = IEnumerableExtensions.TakeRandom<CardModel>(from c in SneckoModel.GetRewardSneckoCards(((RelicModel)this).Owner)
			where c != null && (int)c.Rarity == 3 && (int)c.Type == 3
			select c, 5, ((RelicModel)this).Owner.RunState.Rng.CombatCardSelection).ToList();
		CardModel val = await CardSelectCmd.FromChooseACardScreen((PlayerChoiceContext)new BlockingPlayerChoiceContext(), (IReadOnlyList<CardModel>)list, ((RelicModel)this).Owner, false);
		if (val != null)
		{
			CardModel val2 = val.ToMutable();
			((ICardScope)((RelicModel)this).Owner.RunState).AddCard(val2, ((RelicModel)this).Owner);
			CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(val2, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 0.1f, (CardPreviewStyle)1);
		}
	}
}
