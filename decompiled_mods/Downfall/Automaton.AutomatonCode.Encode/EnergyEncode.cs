using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Encode;

public class EnergyEncode : Encodable
{
	public override TargetType Target => (TargetType)1;

	public override CardType Type => (CardType)2;

	public override DynamicVar FunctionDynamicVar => (DynamicVar)new EnergyVar(0);

	public override Task OnPlay(AbstractModel model, PlayerChoiceContext ctx, Creature? target, CardPlay? cardPlay)
	{
		Player player = model.GetCreature().Player;
		if (player != null)
		{
			return PlayerCmd.GainEnergy((decimal)((DynamicVar)model.GetDynamicVars().Energy).IntValue, player);
		}
		return Task.CompletedTask;
	}

	public override IEnumerable<IHoverTip> HoverTips(AbstractModel model)
	{
		return new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>(GetEnergyTip(model));
	}

	private IHoverTip GetEnergyTip(AbstractModel model)
	{
		PowerModel val = (PowerModel)(object)((model is PowerModel) ? model : null);
		if (val == null)
		{
			RelicModel val2 = (RelicModel)(object)((model is RelicModel) ? model : null);
			if (val2 == null)
			{
				CardModel val3 = (CardModel)(object)((model is CardModel) ? model : null);
				if (val3 == null)
				{
					PotionModel val4 = (PotionModel)(object)((model is PotionModel) ? model : null);
					if (val4 != null)
					{
						return HoverTipFactory.ForEnergy(val4);
					}
					throw new Exception("Unknown model");
				}
				return HoverTipFactory.ForEnergy(val3);
			}
			return HoverTipFactory.ForEnergy(val2);
		}
		return HoverTipFactory.ForEnergy(val);
	}

	public override DynamicVar DynamicVar(AbstractModel model)
	{
		return (DynamicVar)(object)model.GetDynamicVars().Energy;
	}
}
