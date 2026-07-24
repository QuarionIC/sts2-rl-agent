using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Token;
using Automaton.AutomatonCode.Powers;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Encode;

public class PowerEncode : Encodable
{
	public override TargetType Target => (TargetType)1;

	public override CardType Type => (CardType)3;

	public override DynamicVar FunctionDynamicVar => (DynamicVar)(object)new PowerVar<FullReleasePower>(0m);

	public override async Task OnPlay(AbstractModel model, PlayerChoiceContext ctx, Creature? target, CardPlay? cardPlay)
	{
		if (model is FunctionCard functionCard)
		{
			(await CommonActions.ApplySelf<FullReleasePower>(ctx, (CardModel)(object)functionCard, false))?.SetDynamicalVars(((CardModel)functionCard).DynamicVars);
		}
	}

	public override DynamicVar DynamicVar(AbstractModel model)
	{
		return DynamicVarSetExtensions.Power<FullReleasePower>(model.GetDynamicVars());
	}
}
