using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using Automaton.AutomatonCode.DynamicVars;
using Automaton.AutomatonCode.Interfaces;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards;

public abstract class AutomatonCardModel : DownfallCardModel<Automaton.AutomatonCode.Core.Automaton>
{
	protected AutomatonCardModel(int cost, CardType type, CardRarity rarity, TargetType targetType, bool showInCardLibrary = true, bool autoAdd = true)
		: base(cost, type, rarity, targetType, showInCardLibrary, autoAdd)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0004: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		if (AutomatonCmd.IsEncodable((CardModel)(object)this))
		{
			((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Encode));
		}
		if (this is ICompilable)
		{
			((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Compile));
		}
	}

	protected void WithStash(int baseValue, int upgradeValue = 0)
	{
		((ConstructedCardModel)this).WithVars((DynamicVar[])(object)new DynamicVar[1] { DynamicVarExtensions.WithUpgrade<StashVar>(new StashVar(baseValue), (decimal)upgradeValue) });
	}
}
