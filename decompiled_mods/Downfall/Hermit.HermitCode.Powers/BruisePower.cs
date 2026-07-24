using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Patches.Localization;
using Downfall.DownfallCode.Compatibility;
using Hermit.HermitCode.Core;
using Hermit.HermitCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Hermit.HermitCode.Powers;

public sealed class BruisePower : HermitPowerModel, IAddDumbVariablesToPowerDescription, IModifyDamageAdditive
{
	private bool HasBigBruiser
	{
		get
		{
			Creature applier = ((PowerModel)this).Applier;
			if (applier == null)
			{
				return false;
			}
			return applier.HasPower<BigBruiserPower>();
		}
	}

	public override PowerInstanceType InstanceType => (PowerInstanceType)2;

	public BruisePower()
		: base((PowerType)2, (PowerStackType)1)
	{
	}

	public void AddDumbVariablesToPowerDescription(LocString description)
	{
		description.Add("HasBigBruiser", HasBigBruiser);
	}

	public decimal ModifyDamageAdditiveCompability(Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource, CardPlay? cardPlay)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		return (target == ((PowerModel)this).Owner && (dealer == ((PowerModel)this).Applier || HasBigBruiser) && ValuePropExtensions.IsPoweredAttack(props)) ? ((PowerModel)this).Amount : 0;
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (HermitHook.ShouldPreventBruiseRemoval(((PowerModel)this).CombatState, this, out IEnumerable<IShouldPreventBruiseRemoval> preventers))
		{
			await HermitHook.AfterPreventedBruiseRemoval(((PowerModel)this).CombatState, this, preventers);
		}
		else
		{
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
