using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.Events;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Powers;

public class DevilsDancePower : HexaghostPowerModel, IWheelMoved, IHasSecondAmount
{
	private int UsesThisTurn { get; set; }

	public string GetSecondAmount()
	{
		return $"{UsesThisTurn}";
	}

	public async Task AfterWheelAdvance(PlayerChoiceContext ctx, Player player, AbstractModel? source, GhostflameModel ghostflame, int ghostflameIndex, bool silent)
	{
		if (!silent)
		{
			if (UsesThisTurn <= ((PowerModel)this).Amount)
			{
				await CardPileCmd.Draw(ctx, player);
			}
			UsesThisTurn++;
			if (UsesThisTurn <= ((PowerModel)this).Amount)
			{
				((PowerModel)this).InvokeDisplayAmountChanged();
			}
		}
	}

	public async Task AfterWheelRetract(PlayerChoiceContext ctx, Player player, AbstractModel? source, GhostflameModel ghostflame, int ghostflameIndex, bool silent)
	{
		if (!silent)
		{
			if (UsesThisTurn <= ((PowerModel)this).Amount)
			{
				await CardPileCmd.Draw(ctx, player);
			}
			UsesThisTurn++;
			if (UsesThisTurn <= ((PowerModel)this).Amount)
			{
				((PowerModel)this).InvokeDisplayAmountChanged();
			}
		}
	}

	public override Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((PowerModel)this).Owner.Side)
		{
			return Task.CompletedTask;
		}
		UsesThisTurn = 0;
		((PowerModel)this).InvokeDisplayAmountChanged();
		return Task.CompletedTask;
	}

	public DevilsDancePower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
