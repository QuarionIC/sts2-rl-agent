using System.Collections.Generic;
using System.Threading.Tasks;
using Champ.ChampCode.Cards.Common;
using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Champ.ChampCode.Powers;

public class CounterPower : ChampPowerModel
{
	public CounterPower()
		: base((PowerType)1, (PowerStackType)1)
	{
		WithTip(new PowerTooltipSource(GetPowerTooltip));
	}

	private static CardHoverTip GetPowerTooltip(PowerModel arg)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected O, but got Unknown
		RiposteStrike riposteStrike = ModelDb.Card<RiposteStrike>();
		((DynamicVar)((CardModel)riposteStrike).DynamicVars.Damage).BaseValue = arg.Amount;
		return new CardHoverTip((CardModel)(object)riposteStrike);
	}

	public override async Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult damageResult, ValueProp props, Creature? dealer, CardModel? cardSource)
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		if (target == ((PowerModel)this).Owner && dealer != ((PowerModel)this).Owner && ((PowerModel)this).Owner.Player != null && ValuePropExtensions.IsCardOrMonsterMove(props))
		{
			Player player = ((PowerModel)this).Owner.Player;
			int num = ChampHook.ModifyCounterStrikeCount(((PowerModel)this).CombatState, player, 1);
			List<CardModel> list = new List<CardModel>();
			for (int i = 0; i < num; i++)
			{
				CardModel val = player.Creature.CombatState.CreateCard((CardModel)(object)ModelDb.Card<RiposteStrike>(), player);
				((DynamicVar)val.DynamicVars.Damage).BaseValue = ((PowerModel)this).Amount;
				list.Add(val);
			}
			await CardPileCmd.AddGeneratedCardsToCombat((IEnumerable<CardModel>)list, (PileType)2, ((PowerModel)this).Owner.Player, (CardPilePosition)1);
			await PowerCmd.ModifyAmount(ctx, (PowerModel)(object)this, (decimal)(-((PowerModel)this).Amount), ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}
}
