using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class WarpedTongs : CustomRelicModel
{
	public override RelicRarity Rarity => (RelicRarity)6;

	public override async Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		if ((int)side == 1)
		{
			CardPile pile = PileTypeExtensions.GetPile((PileType)2, ((RelicModel)this).Owner);
			List<CardModel> upgradableCards = pile.Cards.Where((CardModel c) => c.IsUpgradable).ToList();
			if (upgradableCards.Count != 0)
			{
				((RelicModel)this).Flash();
				CardModel card = ((RelicModel)this).Owner.RunState.Rng.CombatCardSelection.NextItem<CardModel>((IEnumerable<CardModel>)upgradableCards);
				CardCmd.Upgrade(card, (CardPreviewStyle)1);
			}
		}
	}

	public WarpedTongs()
		: base(true)
	{
	}
}
