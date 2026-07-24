using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Automaton.AutomatonCode.Encode;

public class StrengthEncode : Encodable
{
	public override TargetType Target => (TargetType)1;

	public override CardType Type => (CardType)2;

	public override DynamicVar FunctionDynamicVar => (DynamicVar)(object)new PowerVar<StrengthPower>(0m);

	public override Task OnPlay(AbstractModel model, PlayerChoiceContext ctx, Creature? target, CardPlay? cardPlay)
	{
		return PowerCmd.Apply<StrengthPower>(ctx, model.GetCreature(), ((DynamicVar)model.GetDynamicVars().Strength).BaseValue, model.GetCreature(), (CardModel)(object)((model is CardModel) ? model : null), false);
	}

	public override IEnumerable<IHoverTip> HoverTips(AbstractModel model)
	{
		return new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(HoverTipFactory.FromPower<StrengthPower>((int?)null));
	}

	public override DynamicVar DynamicVar(AbstractModel model)
	{
		return (DynamicVar)(object)model.GetDynamicVars().Strength;
	}
}
