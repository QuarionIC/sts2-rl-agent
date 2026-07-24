using System.Threading.Tasks;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Hexaghost.HexaghostCode.Relics;

[Pool(typeof(HexaghostRelicPool))]
public class Recycler : HexaghostRelicModel
{
	private bool _usedThisCombat;

	private bool UsedThisCombat
	{
		get
		{
			return _usedThisCombat;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_usedThisCombat = value;
		}
	}

	public Recycler()
		: base((RelicRarity)3)
	{
		WithTip((CardKeyword)2);
		WithTip((CardKeyword)1);
	}

	public override Task BeforeCombatStart()
	{
		((RelicModel)this).Status = (RelicStatus)1;
		UsedThisCombat = false;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom _)
	{
		((RelicModel)this).Status = (RelicStatus)0;
		UsedThisCombat = false;
		return Task.CompletedTask;
	}

	public override async Task AfterCardExhausted(PlayerChoiceContext choiceContext, CardModel card, bool causedByEthereal)
	{
		bool flag = UsedThisCombat || card.Owner != ((RelicModel)this).Owner || !card.Keywords.Contains((CardKeyword)2);
		if (!flag)
		{
			CardType type = card.Type;
			bool flag2 = type - 4 <= 1;
			flag = flag2;
		}
		if (!flag)
		{
			await CardPileCmd.Add(card.CreateClone(), (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
			UsedThisCombat = true;
			((RelicModel)this).Flash();
			((RelicModel)this).Status = (RelicStatus)0;
		}
	}
}
