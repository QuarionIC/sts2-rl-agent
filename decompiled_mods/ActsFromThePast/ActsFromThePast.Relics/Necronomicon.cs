using System.Collections.Generic;
using System.Threading.Tasks;
using ActsFromThePast.Cards;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.RelicPools;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace ActsFromThePast.Relics;

[Pool(typeof(EventRelicPool))]
public sealed class Necronomicon : CustomRelicModel
{
	private bool _activated = true;

	public override RelicRarity Rarity => (RelicRarity)6;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromCard<Necronomicurse>(false) };

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[1] { (DynamicVar)new EnergyVar(2) };

	public override async Task AfterObtained()
	{
		AFTPModAudio.Play("relics", "necronomicon");
		CardModel curse = ((ICardScope)((RelicModel)this).Owner.RunState).CreateCard((CardModel)(object)ModelDb.Card<Necronomicurse>(), ((RelicModel)this).Owner);
		CardCmd.PreviewCardPileAdd(await CardPileCmd.Add(curse, (PileType)6, (CardPilePosition)1, (AbstractModel)null, false), 2f, (CardPreviewStyle)1);
	}

	public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player != ((RelicModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		_activated = true;
		((RelicModel)this).Status = (RelicStatus)0;
		return Task.CompletedTask;
	}

	public override int ModifyCardPlayCount(CardModel card, Creature? target, int playCount)
	{
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		if (card.Owner != ((RelicModel)this).Owner)
		{
			return playCount;
		}
		if (!_activated)
		{
			return playCount;
		}
		if ((int)card.Type != 1)
		{
			return playCount;
		}
		if (card.EnergyCost.GetResolved() < ((DynamicVar)((RelicModel)this).DynamicVars.Energy).IntValue)
		{
			return playCount;
		}
		return playCount + 1;
	}

	public override Task AfterModifyingCardPlayCount(CardModel card)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Invalid comparison between Unknown and I4
		if (card.Owner != ((RelicModel)this).Owner)
		{
			return Task.CompletedTask;
		}
		if ((int)card.Type != 1)
		{
			return Task.CompletedTask;
		}
		if (card.EnergyCost.GetResolved() < ((DynamicVar)((RelicModel)this).DynamicVars.Energy).IntValue)
		{
			return Task.CompletedTask;
		}
		if (!_activated)
		{
			return Task.CompletedTask;
		}
		_activated = false;
		((RelicModel)this).Flash();
		((RelicModel)this).Status = (RelicStatus)2;
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom _)
	{
		_activated = true;
		((RelicModel)this).Status = (RelicStatus)0;
		return Task.CompletedTask;
	}

	public Necronomicon()
		: base(true)
	{
	}
}
