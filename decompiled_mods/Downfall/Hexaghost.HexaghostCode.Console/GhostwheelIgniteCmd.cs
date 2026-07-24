using System;
using System.Collections.Generic;
using System.Linq;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Runs;

namespace Hexaghost.HexaghostCode.Console;

public class GhostwheelIgniteCmd : AbstractConsoleCmd
{
	public override string CmdName => "downfall-ignite";

	public override string Args => "[index:int]";

	public override string Description => "Ignite the current ghostflame, or a specific index if provided.";

	public override bool IsNetworked => true;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected O, but got Unknown
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0156: Unknown result type (might be due to invalid IL or missing references)
		if (!RunManager.Instance.IsInProgress)
		{
			return new CmdResult(false, "No run in progress.");
		}
		if (issuingPlayer == null)
		{
			return new CmdResult(false, "No player context.");
		}
		BlockingPlayerChoiceContext ctx = new BlockingPlayerChoiceContext();
		GhostflameModel[] wheel = HexaghostCmd.GetWheel(issuingPlayer);
		if (args.Length == 0)
		{
			int currentIndex = HexaghostCmd.GetCurrentIndex(issuingPlayer);
			TaskHelper.RunSafely(HexaghostCmd.Ignite((PlayerChoiceContext)(object)ctx, issuingPlayer));
			return new CmdResult(true, $"Ignited flame at index {currentIndex} ({((object)wheel[currentIndex]).GetType().Name}).");
		}
		if (!int.TryParse(args[0], out var result) || result < 0 || result >= wheel.Length)
		{
			return new CmdResult(false, $"Invalid index. Valid range: 0-{wheel.Length - 1}.");
		}
		TaskHelper.RunSafely(HexaghostCmd.IgniteAt((PlayerChoiceContext)(object)ctx, issuingPlayer, result));
		return new CmdResult(true, $"Ignited flame at index {result} ({((object)wheel[result]).GetType().Name}).");
	}

	public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		if (args.Length != 1 || player == null || !RunManager.Instance.IsInProgress)
		{
			return new CompletionResult
			{
				Type = (CompletionType)2,
				ArgumentContext = ((AbstractConsoleCmd)this).CmdName
			};
		}
		GhostflameModel[] wheel = HexaghostCmd.GetWheel(player);
		List<string> list = (from i in Enumerable.Range(0, wheel.Length)
			select $"{i} ({((object)wheel[i]).GetType().Name})").ToList();
		return ((AbstractConsoleCmd)this).CompleteArgument((IEnumerable<string>)list, Array.Empty<string>(), args[0], (CompletionType)2, (Func<string, string, bool>)null);
	}
}
