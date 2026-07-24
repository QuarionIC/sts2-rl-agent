using System.Threading.Tasks;
using BaseLib.Extensions;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Champ.ChampCode.Relics;

[Pool(typeof(ChampRelicPool))]
public class AmuletOfUnyielding : ChampRelicModel
{
	private int _strengthGranted;

	private decimal _vigorSpentThisCombat;

	public override bool ShowCounter => CombatManager.Instance.IsInProgress;

	public override int DisplayAmount => (int)(_vigorSpentThisCombat % (decimal)VigorThreshold);

	private int VigorThreshold => DynamicVarSetExtensions.Power<VigorPower>(((RelicModel)this).DynamicVars).IntValue;

	private int StrengthMult => DynamicVarSetExtensions.Power<StrengthPower>(((RelicModel)this).DynamicVars).IntValue;

	public AmuletOfUnyielding()
		: base((RelicRarity)4)
	{
		WithPower<StrengthPower>(1, showTooltip: true);
		WithPower<VigorPower>(12, showTooltip: true);
	}

	public override async Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power.Owner == ((RelicModel)this).Owner.Creature && power is VigorPower && !(amount >= 0m))
		{
			_vigorSpentThisCombat -= amount;
			((RelicModel)this).InvokeDisplayAmountChanged();
			int num = (int)(_vigorSpentThisCombat / (decimal)VigorThreshold);
			int num2 = num - _strengthGranted;
			if (num2 > 0)
			{
				_strengthGranted = num;
				num2 *= StrengthMult;
				await PowerCmd.Apply<StrengthPower>(ctx, ((RelicModel)this).Owner.Creature, (decimal)num2, ((RelicModel)this).Owner.Creature, (CardModel)null, false);
				((RelicModel)this).Flash();
			}
		}
	}
}
