using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using Downfall.DownfallCode.Compatibility;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Powers;

public class UnyieldingPower : HermitPowerModel, IModifyDamageMultiplicative
{
	public UnyieldingPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithVar("DamageDecrease", 0.5m);
		((ConstructedPowerModel)this).WithTip<VulnerablePower>();
	}

	public decimal ModifyDamageMultiplicativeCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		if (target == ((PowerModel)this).Owner && ValuePropExtensions.IsPoweredAttack(props) && dealer != null && target.HasPower<VulnerablePower>())
		{
			return ((PowerModel)this).DynamicVars["DamageDecrease"].BaseValue;
		}
		return 1m;
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		if ((int)side == 2)
		{
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}
}
