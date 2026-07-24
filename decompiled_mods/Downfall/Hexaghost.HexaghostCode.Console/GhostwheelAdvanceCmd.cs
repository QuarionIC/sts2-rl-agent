using System.Threading.Tasks;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Runs;

namespace Hexaghost.HexaghostCode.Console;

public class GhostwheelAdvanceCmd : AbstractConsoleCmd
{
	public override string CmdName => "downfall-advance";

	public override string Args => "[steps:int]";

	public override string Description => "Advance the ghostwheel by 1 step, or N steps if provided.";

	public override bool IsNetworked => true;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Expected O, but got Unknown
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		if (!RunManager.Instance.IsInProgress)
		{
			return new CmdResult(false, "No run in progress.");
		}
		if (issuingPlayer == null)
		{
			return new CmdResult(false, "No player context.");
		}
		int result = 1;
		if (args.Length != 0 && !int.TryParse(args[0], out result))
		{
			return new CmdResult(false, "Invalid steps value '" + args[0] + "'.");
		}
		TaskHelper.RunSafely(AdvanceMultiple((PlayerChoiceContext)new BlockingPlayerChoiceContext(), issuingPlayer, result));
		int currentIndex = HexaghostCmd.GetCurrentIndex(issuingPlayer);
		GhostflameModel currentFlame = HexaghostCmd.GetCurrentFlame(issuingPlayer);
		return new CmdResult(true, $"Advanced {result} step(s). Now at index {currentIndex} ({((object)currentFlame).GetType().Name}).");
	}

	private static async Task AdvanceMultiple(PlayerChoiceContext ctx, Player player, int steps)
	{
		for (int i = 0; i < steps; i++)
		{
			await HexaghostCmd.Advance(ctx, player, null);
		}
	}
}
