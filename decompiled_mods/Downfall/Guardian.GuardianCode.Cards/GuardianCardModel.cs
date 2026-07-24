using System;
using System.Collections.Generic;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Abstract;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards;

public abstract class GuardianCardModel : DownfallCardModel<Guardian.GuardianCode.Core.Guardian>
{
	protected GuardianCardModel(int cost, CardType type, CardRarity rarity, TargetType targetType, bool showInCardLibrary = true, bool autoAdd = true)
		: base(cost, type, rarity, targetType, showInCardLibrary, autoAdd)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTips((Func<CardModel, IEnumerable<IHoverTip>>)((CardModel card) => (!(card is IGemSocketCard gemSocketCard)) ? Array.Empty<IHoverTip>() : gemSocketCard.Gems.SelectMany((GemModel gem) => gem.HoverTips)));
		if (this is ITickCard)
		{
			((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianTip.Tick));
		}
	}
}
