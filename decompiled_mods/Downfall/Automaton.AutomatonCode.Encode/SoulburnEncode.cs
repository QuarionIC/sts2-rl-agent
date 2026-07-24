using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Extensions;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Powers;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Encode;

public class SoulburnEncode : Encodable
{
	public override TargetType Target => (TargetType)3;

	public override CardType Type => (CardType)2;

	public override DynamicVar FunctionDynamicVar => (DynamicVar)(object)new PowerVar<SoulBurnPower>(0m);

	public override Task OnPlay(AbstractModel model, PlayerChoiceContext ctx, Creature? target, CardPlay? cardPlay)
	{
		Creature creature = model.GetCreature();
		ICombatState combatState = creature.CombatState;
		if (combatState == null)
		{
			return Task.CompletedTask;
		}
		return PowerCmd.Apply<SoulBurnPower>(ctx, (IEnumerable<Creature>)combatState.HittableEnemies, DynamicVarSetExtensions.Power<SoulBurnPower>(model.GetDynamicVars()).BaseValue, creature, (CardModel)(object)((model is CardModel) ? model : null), false);
	}

	public override IEnumerable<IHoverTip> HoverTips(AbstractModel model)
	{
		return new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromPower<SoulBurnPower>((int?)null));
	}

	public override DynamicVar DynamicVar(AbstractModel model)
	{
		return DynamicVarSetExtensions.Power<SoulBurnPower>(model.GetDynamicVars());
	}
}
