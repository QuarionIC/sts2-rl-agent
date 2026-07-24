using System;
using System.Linq;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Interfaces;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Cards.Abstract;

public abstract class GemCard<T> : GuardianCardModel, IGemCard, IGemSocketCard, IModifyReplayCount, ICardOverlay, ICustomTypePlaque, IModfyCardDescription where T : GemModel
{
	public override bool CanBeGeneratedInCombat => false;

	public override CardRarity Rarity => GuardianModelDb.Gem<T>().Rarity;

	public override int MaxUpgradeLevel => 0;

	public LocString GetTypePlaqueName => new LocString("gameplay_ui", "GUARDIAN-GEM");

	public GemModel CanonicalGemModel => GuardianModelDb.Gem<T>();

	public GemModel GemModel => CardModifier.DirectModifiers((CardModel)(object)this).OfType<GemModel>().First();

	public int GemSlots => 0;

	protected GemCard()
		: base(0, (CardType)2, (CardRarity)0, (TargetType)1)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		((CardModel)this)._titleLocString = GuardianModelDb.Gem<T>().Title;
		((ConstructedCardModel)this).WithKeyword(GuardianKeyword.Gem, (UpgradeType)0);
		foreach (IHoverTip extraHoverTip in GuardianModelDb.Gem<T>().ExtraHoverTips)
		{
			((ConstructedCardModel)this).WithTip(new TooltipSource((Func<CardModel, IHoverTip>)((CardModel _) => extraHoverTip)));
		}
		CardModifier.AddModifier((CardModel)(object)this, (CardModifier)(object)GuardianModelDb.Gem<T>().ToMutable());
	}

	public LocString ModifyDescription(LocString oldLocString)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		return new LocString("cards", "GUARDIAN-GEM_CARD.description");
	}
}
