using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using Guardian.GuardianCode.Cards.Abstract;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.DynamicVars;
using Guardian.GuardianCode.Events;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Gems;

public class DiamondGem : GemModel
{
	private bool _usedThisCombat;

	public override Color GemColor => new Color(2546654207u);

	protected override IEnumerable<DynamicVar> CanonicalVars => new _003C_003Ez__ReadOnlySingleElementList<DynamicVar>((DynamicVar)(object)new GemVar(1m));

	public override CardRarity Rarity => (CardRarity)4;

	public override IEnumerable<IHoverTip> ExtraHoverTips => new global::_003C_003Ez__ReadOnlyArray<IHoverTip>((IHoverTip[])(object)new IHoverTip[3]
	{
		HoverTipFactory.Static((StaticHoverTip)15, Array.Empty<DynamicVar>()),
		HoverTipFactory.Static((StaticHoverTip)7, Array.Empty<DynamicVar>()),
		HoverTipFactory.Static(GuardianTip.Aggravate, Array.Empty<DynamicVar>())
	});

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

	protected override Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay? cardPlay)
	{
		return Task.CompletedTask;
	}

	public override int ModifyPlayCount(int originalPlayCount)
	{
		if (UsedThisCombat || base.Card == null)
		{
			return originalPlayCount;
		}
		if (!((AbstractModel)this).IsMutable || !((AbstractModel)base.Card).IsMutable)
		{
			return originalPlayCount;
		}
		CardModel? card = base.Card;
		Player val = ((card != null) ? card.Owner : null);
		if (val == null)
		{
			return originalPlayCount;
		}
		ICombatState combatState = val.Creature.CombatState;
		if (combatState == null)
		{
			return originalPlayCount;
		}
		return originalPlayCount + (int)GuardianHook.ModifyGemEffect(combatState, this, ((DynamicVar)((CardModifier)this).DynamicVars.Gem()).BaseValue, base.Card);
	}

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (UsedThisCombat || cardPlay.Card != base.Card)
		{
			return Task.CompletedTask;
		}
		UsedThisCombat = true;
		return Task.CompletedTask;
	}

	public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
	{
		modifiedCost = originalCost;
		if (base.Card is IGemCard || card != base.Card)
		{
			return false;
		}
		++modifiedCost;
		return true;
	}
}
