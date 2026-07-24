using System.Globalization;
using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.DynamicVars;

public class BerserkerFinisherVar : DynamicVar
{
	private ChampStanceModel Stance => (ChampStanceModel)(object)base._owner;

	public BerserkerFinisherVar(decimal baseAmount)
		: base("BerserkerFinisher", baseAmount)
	{
	}

	public decimal Calculate()
	{
		if (!CombatManager.Instance.IsInProgress || !((AbstractModel)Stance).IsMutable)
		{
			return base._baseValue;
		}
		int num = ChampHook.ModifyBerserkerFinisherBonus(Stance.CombatState, Stance, (int)base._baseValue);
		((DynamicVar)this).PreviewValue = num;
		return num;
	}

	public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target, bool runGlobalHooks)
	{
		((DynamicVar)this).PreviewValue = Calculate();
	}

	protected override decimal GetBaseValueForIConvertible()
	{
		return Calculate();
	}

	public override string ToString()
	{
		return Calculate().ToString(CultureInfo.InvariantCulture);
	}
}
