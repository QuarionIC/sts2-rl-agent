using System.Collections.Generic;
using Awakened.AwakenedCode.Cards.Token;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.Events;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Relics;

[Pool(typeof(AwakenedRelicPool))]
public class ZenerDeck : AwakenedRelicModel, IModifyBaseSpells
{
	public ZenerDeck()
		: base((RelicRarity)4)
	{
		WithTip<ESP>();
	}

	public IReadOnlyList<CardModel> ModifyBaseSpells(Player owner, IReadOnlyList<CardModel> types)
	{
		int num = 0;
		CardModel[] array = (CardModel[])(object)new CardModel[1 + types.Count];
		foreach (CardModel type in types)
		{
			array[num] = type;
			num++;
		}
		array[num] = (CardModel)(object)ModelDb.Card<ESP>();
		return new global::_003C_003Ez__ReadOnlyArray<CardModel>(array);
	}
}
