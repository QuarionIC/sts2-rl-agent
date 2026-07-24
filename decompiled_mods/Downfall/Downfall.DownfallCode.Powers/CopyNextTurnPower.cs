using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Powers;

public class CopyNextTurnPower : DownfallPowerModel
{
	private class CardDynamicVar : DynamicVar
	{
		private CopyNextTurnPower? _power;

		public CardDynamicVar()
			: base("card", 0m)
		{
		}

		public override void SetOwner(AbstractModel model)
		{
			((DynamicVar)this).SetOwner(model);
			_power = model as CopyNextTurnPower;
		}

		public override string ToString()
		{
			if (_power?.Card != null)
			{
				return _power.Card.Title;
			}
			return "?";
		}
	}

	public CardModel? Card;

	public Action<CardModel>? OnAdd;

	public override PowerInstanceType InstanceType => (PowerInstanceType)1;

	public CopyNextTurnPower()
		: base((PowerType)1, (PowerStackType)2)
	{
		WithVars(new CardDynamicVar());
		WithTips(Tip);
	}

	private static IEnumerable<IHoverTip> Tip(PowerModel arg)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Expected O, but got Unknown
		if (!(arg is CopyNextTurnPower { Card: not null } copyNextTurnPower))
		{
			return Array.Empty<IHoverTip>();
		}
		return new _003C_003Ez__ReadOnlySingleElementList<IHoverTip>((IHoverTip)new CardHoverTip(copyNextTurnPower.Card));
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player.Creature == ((PowerModel)this).Owner && Card != null)
		{
			await CardPileCmd.Add(Card, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
			OnAdd?.Invoke(Card);
			await PowerCmd.Remove((PowerModel)(object)this);
		}
	}
}
