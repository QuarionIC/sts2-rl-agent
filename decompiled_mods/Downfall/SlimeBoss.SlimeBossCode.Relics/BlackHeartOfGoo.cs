using System;
using System.Threading.Tasks;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.Events;

namespace SlimeBoss.SlimeBossCode.Relics;

[Pool(typeof(SlimeBossRelicPool))]
public class BlackHeartOfGoo : SlimeBossRelicModel, IAfterConsumeEffect
{
	private DynamicVar UsesLeft => ((RelicModel)this).DynamicVars["UsesLeft"];

	private DynamicVar MaxUses => ((RelicModel)this).DynamicVars["MaxUses"];

	public override int DisplayAmount => UsesLeft.IntValue;

	public override bool ShowCounter => CombatManager.Instance.IsInProgress;

	public BlackHeartOfGoo()
		: base((RelicRarity)1)
	{
		WithHeal(3);
		WithVar("UsesLeft", 15);
		WithVar("MaxUses", 15);
	}

	public async Task AfterConsumeEffect(PlayerChoiceContext ctx, Creature creature, Creature attacker, decimal goopAmount)
	{
		if (attacker == ((RelicModel)this).Owner.Creature || !(UsesLeft.BaseValue > 0m))
		{
			decimal heal = Math.Min(((DynamicVar)((RelicModel)this).DynamicVars.Heal).BaseValue, UsesLeft.BaseValue);
			if (!(heal <= 0m))
			{
				await CreatureCmd.Heal(((RelicModel)this).Owner.Creature, heal, true);
				DynamicVar usesLeft = UsesLeft;
				usesLeft.BaseValue -= heal;
				((RelicModel)this).Flash();
				((RelicModel)this).InvokeDisplayAmountChanged();
			}
		}
	}

	public override Task BeforeCombatStart()
	{
		UsesLeft.BaseValue = MaxUses.BaseValue;
		((RelicModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public override Task AfterCombatEnd(CombatRoom _)
	{
		((RelicModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}
}
