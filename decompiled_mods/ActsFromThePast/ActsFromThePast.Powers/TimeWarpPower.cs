using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace ActsFromThePast.Powers;

public sealed class TimeWarpPower : CustomPowerModel
{
	private const int StrengthAmount = 2;

	private const int CountdownPerPlayer = 12;

	private const string _cardCountKey = "CardCount";

	private const string _countdownKey = "Countdown";

	public override PowerType Type => (PowerType)1;

	public override PowerStackType StackType => (PowerStackType)1;

	public override bool ShouldScaleInMultiplayer => false;

	public override int DisplayAmount => ((PowerModel)this).DynamicVars["CardCount"].IntValue;

	private int CountdownAmount => ((PowerModel)this).DynamicVars["Countdown"].IntValue;

	protected override IEnumerable<DynamicVar> CanonicalVars => (IEnumerable<DynamicVar>)(object)new DynamicVar[2]
	{
		new DynamicVar("CardCount", 0m),
		new DynamicVar("Countdown", 12m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => (IEnumerable<IHoverTip>)(object)new IHoverTip[1] { HoverTipFactory.FromPower<StrengthPower>((int?)null) };

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		((PowerModel)this).DynamicVars["Countdown"].BaseValue = 12 * ((PowerModel)this).Owner.CombatState.Players.Count;
		return Task.CompletedTask;
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		DynamicVar obj = ((PowerModel)this).DynamicVars["CardCount"];
		decimal baseValue = obj.BaseValue;
		obj.BaseValue = baseValue + 1m;
		((PowerModel)this).InvokeDisplayAmountChanged();
		if (((PowerModel)this).DynamicVars["CardCount"].IntValue < CountdownAmount)
		{
			return;
		}
		((PowerModel)this).DynamicVars["CardCount"].BaseValue = 0m;
		((PowerModel)this).InvokeDisplayAmountChanged();
		((PowerModel)this).Flash();
		AFTPModAudio.Play("time_eater", "time_warp");
		BorderFlashEffect.PlayGold();
		TimeWarpTurnEndEffect effect = TimeWarpTurnEndEffect.Create();
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)effect);
		}
		foreach (Player player in ((PowerModel)this).Owner.CombatState.Players)
		{
			PlayerCmd.EndTurn(player, false, (Func<Task>)null);
		}
		foreach (Creature enemy in ((PowerModel)this).Owner.CombatState.Enemies.Where((Creature e) => e.IsAlive))
		{
			await PowerCmd.Apply<StrengthPower>((PlayerChoiceContext)new ThrowingPlayerChoiceContext(), enemy, 2m, ((PowerModel)this).Owner, (CardModel)null, false);
		}
	}
}
