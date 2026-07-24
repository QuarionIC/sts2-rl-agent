using System.Threading.Tasks;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Automaton.AutomatonCode.Encode;

public class BlockEncode : Encodable
{
	public override TargetType Target => (TargetType)1;

	public override CardType Type => (CardType)2;

	public override DynamicVar FunctionDynamicVar => (DynamicVar)new BlockVar(0m, (ValueProp)8);

	public override Task OnPlay(AbstractModel model, PlayerChoiceContext ctx, Creature? target, CardPlay? cardPlay)
	{
		return CreatureCmd.GainBlock(model.GetCreature(), ((DynamicVar)model.GetDynamicVars().Block).BaseValue, (ValueProp)((model is CardModel) ? 8 : 4), cardPlay, false);
	}

	public override DynamicVar DynamicVar(AbstractModel model)
	{
		return (DynamicVar)(object)model.GetDynamicVars().Block;
	}
}
