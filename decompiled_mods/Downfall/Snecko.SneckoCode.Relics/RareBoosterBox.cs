using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;
using Snecko.SneckoCode.Core;

namespace Snecko.SneckoCode.Relics;

[Pool(typeof(SneckoRelicPool))]
public class RareBoosterBox : SneckoRelicModel
{
	public override bool HasUponPickupEffect => true;

	public RareBoosterBox()
		: base((RelicRarity)5)
	{
	}

	public override async Task AfterObtained()
	{
		CardModel val = ((RelicModel)this).Owner.RunState.Rng.CombatCardSelection.NextItem<CardModel>(from c in SneckoModel.GetRewardSneckoCards(((RelicModel)this).Owner)
			where (int)c.Rarity == 4
			select c);
		if (val != null)
		{
			CardModel val2 = val.ToMutable();
			((ICardScope)((RelicModel)this).Owner.RunState).AddCard(val2, ((RelicModel)this).Owner);
			CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(val2, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 0.1f, (CardPreviewStyle)1);
		}
	}
}
