using System.Globalization;
using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.DynamicVars;

public class BerserkerSkillVar : DynamicVar
{
	private ChampStanceModel Stance => (ChampStanceModel)(object)base._owner;

	public BerserkerSkillVar(decimal baseAmount)
		: base("BerserkerSkill", baseAmount)
	{
	}

	public decimal Calculate()
	{
		if (!CombatManager.Instance.IsInProgress || !((AbstractModel)Stance).IsMutable)
		{
			return base._baseValue;
		}
		int num = ChampHook.ModifySkillBonus<VigorPower>(Stance.CombatState, Stance, (int)base._baseValue);
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
