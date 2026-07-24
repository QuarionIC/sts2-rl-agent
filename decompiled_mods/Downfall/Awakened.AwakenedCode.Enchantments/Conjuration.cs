using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Enchantments;

public class Conjuration : DownfallEnchantmentModel<Awakened.AwakenedCode.Core.Awakened>
{
	protected override IEnumerable<IHoverTip> ExtraHoverTips => new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(AwakenedTip.Conjure, Array.Empty<DynamicVar>()));

	public override bool HasExtraCardText => true;

	public override bool CanEnchant(CardModel card)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		if (((EnchantmentModel)this).CanEnchant(card))
		{
			return !card.Tags.Contains(AwakenedTag.Conjure);
		}
		return false;
	}

	public override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay? cardPlay)
	{
		if (((cardPlay != null) ? cardPlay.Card.CombatState : null) == null)
		{
			return Task.CompletedTask;
		}
		return AwakenedCmd.Conjure(cardPlay.Card.Owner);
	}
}
