using Downfall.DownfallCode.Vfx;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Downfall.DownfallCode.Console;

public class StatusBarSetCmd : AbstractConsoleCmd
{
	public override string CmdName => "downfall-statusbar";

	public override string Args => "<current:int> <max:int>";

	public override string Description => "Set the status bar for the issuing player.";

	public override bool IsNetworked => false;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		if (issuingPlayer == null)
		{
			return new CmdResult(false, "No player context.");
		}
		if (args.Length < 2)
		{
			return new CmdResult(false, "Usage: statusbar <current> <max>");
		}
		if (!int.TryParse(args[0], out var result))
		{
			return new CmdResult(false, "Invalid current value '" + args[0] + "'.");
		}
		if (!int.TryParse(args[1], out var result2))
		{
			return new CmdResult(false, "Invalid max value '" + args[1] + "'.");
		}
		if (result2 < 0 || result2 > 5)
		{
			return new CmdResult(false, "Max must be between 0 and 5.");
		}
		if (result < 0 || result > result2)
		{
			return new CmdResult(false, $"Current must be between 0 and {result2}.");
		}
		StatusBarHelper.SetStatus(issuingPlayer, result, result2, null);
		return new CmdResult(true, $"Status bar set to {result}/{result2}.");
	}
}
