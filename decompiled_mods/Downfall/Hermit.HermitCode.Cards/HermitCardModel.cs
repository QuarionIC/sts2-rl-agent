using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Abstract;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Cards;

[Pool(typeof(HermitCardPool))]
public abstract class HermitCardModel : DownfallCardModel<Hermit.HermitCode.Core.Hermit>
{
	protected override bool ShouldGlowGoldInternal
	{
		get
		{
			if (this is IHasDeadOnEffect hasDeadOnEffect)
			{
				return hasDeadOnEffect.IsDeadOnInHand;
			}
			return false;
		}
	}

	protected HermitCardModel(int cost, CardType type, CardRarity rarity, TargetType targetType, bool showInCardLibrary = true, bool autoAdd = true)
		: base(cost, type, rarity, targetType, showInCardLibrary, autoAdd)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel e) => (!(e is IHasDeadOnEffect)) ? ((IEnumerable<IHoverTip>)Array.Empty<IHoverTip>()) : ((IEnumerable<IHoverTip>)new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromKeyword(HermitKeywords.DeadOn)))));
	}
}
