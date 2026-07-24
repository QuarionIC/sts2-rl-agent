using System;
using System.Collections.Generic;
using Awakened.AwakenedCode.Core;
using Awakened.AwakenedCode.CustomEnums;
using Awakened.AwakenedCode.Interfaces;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Awakened.AwakenedCode.Cards;

public abstract class AwakenedCardModel : DownfallCardModel<Awakened.AwakenedCode.Core.Awakened>
{
	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			if (this is IChantable chantable)
			{
				if (!AwakenedCmd.WasLastCardPlayedPower((CardModel)(object)this))
				{
					return chantable.HasChanted;
				}
				return true;
			}
			return false;
		}
	}

	protected AwakenedCardModel(int cost, CardType type, CardRarity rarity, TargetType targetType, bool showInCardLibrary = true, bool autoAdd = true)
		: base(cost, type, rarity, targetType, showInCardLibrary, autoAdd)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)delegate(CardModel card)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			if (!(card is IChantable chantable))
			{
				return Array.Empty<IHoverTip>();
			}
			return (!chantable.HasChanted) ? new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(AwakenedTip.Chant, Array.Empty<DynamicVar>())) : new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.Static(AwakenedTip.Chanted, Array.Empty<DynamicVar>()));
		});
	}
}
