using System.Threading.Tasks;
using BaseLib.Utils;
using Downfall.DownfallCode.Compatibility;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Automaton.AutomatonCode.Encode;

public class DamageEncode : Encodable
{
	public override TargetType Target => (TargetType)2;

	public override CardType Type => (CardType)1;

	public override DynamicVar FunctionDynamicVar => (DynamicVar)new DamageVar(0m, (ValueProp)8);

	public override Task OnPlay(AbstractModel model, PlayerChoiceContext ctx, Creature? target, CardPlay? cardPlay)
	{
		if (target == null)
		{
			return Task.CompletedTask;
		}
		CardModel val = (CardModel)(object)((model is CardModel) ? model : null);
		if (val != null)
		{
			return BetaMainCompatibility.FromCardCompatibility(DamageCmd.Attack(((DynamicVar)val.DynamicVars.Damage).BaseValue), val, cardPlay).Targeting(target).Execute(ctx);
		}
		return DownfallCreatureCmd.Damage(ctx, target, ((DynamicVar)model.GetDynamicVars().Damage).BaseValue, (ValueProp)4, model.GetCreature(), null, null);
	}

	public override DynamicVar DynamicVar(AbstractModel model)
	{
		return (DynamicVar)(object)model.GetDynamicVars().Damage;
	}
}
