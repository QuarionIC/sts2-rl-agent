using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class Shapeshift : SneckoCardModel
{
	public Shapeshift()
		: base(2, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		Rng rng = ((CardModel)this).Owner.RunState.Rng.CombatCardGeneration;
		List<CardModel> source = SneckoModel.GetSneckoCards(((CardModel)this).Owner).ToList();
		Dictionary<CardRarity, List<CardModel>> byRarity = (from c in source
			group c by c.Rarity).ToDictionary((IGrouping<CardRarity, CardModel> g) => g.Key, (IGrouping<CardRarity, CardModel> g) => g.ToList());
		List<CardModel> list = ((CardModel)this).Owner.GetHand().ToList();
		foreach (CardModel card in list)
		{
			if (!byRarity.TryGetValue(card.Rarity, out var value) || value.Count == 0)
			{
				continue;
			}
			List<CardModel> list2 = value.Where((CardModel c) => ((AbstractModel)c).Id != ((AbstractModel)card).Id).ToList();
			if (list2.Count == 0)
			{
				continue;
			}
			CardModel val = rng.NextItem<CardModel>((IEnumerable<CardModel>)list2);
			if (val == null)
			{
				continue;
			}
			ICombatState combatState = ((CardModel)this).CombatState;
			CardModel replacement = ((combatState != null) ? combatState.CreateCard(val, ((CardModel)this).Owner) : null);
			if (replacement != null)
			{
				await CardCmd.Transform(card, replacement, (CardPreviewStyle)1);
				if (((CardModel)this).IsUpgraded)
				{
					CardCmd.Upgrade(replacement, (CardPreviewStyle)1);
				}
			}
		}
	}
}
