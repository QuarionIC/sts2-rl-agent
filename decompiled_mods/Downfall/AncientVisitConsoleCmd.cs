using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

public class AncientVisitConsoleCmd : AbstractConsoleCmd
{
	public override string CmdName => "downfall-ancient";

	public override string Args => "<id:string> <index:int>";

	public override string Description => "Opens an ancient event forcing a specific visit/win index";

	public override bool IsNetworked => true;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Expected O, but got Unknown
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Expected O, but got Unknown
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (args.Length < 2)
		{
			return new CmdResult(false, "Usage: downfall-ancient <id> <index>");
		}
		if (!int.TryParse(args[1], out var result) || result < 0)
		{
			return new CmdResult(false, "index must be a non-negative integer.");
		}
		EventModel byIdOrNull = ModelDb.GetByIdOrNull<EventModel>(new ModelId(ModelDb.GetCategory(typeof(EventModel)), args[0].ToUpperInvariant()));
		if (!(byIdOrNull is AncientEventModel) && !(byIdOrNull is TheArchitect))
		{
			return new CmdResult(false, "Invalid ancient ID.");
		}
		AncientDebug.ForcedVisitIndex = result;
		EventRoom val = new EventRoom(byIdOrNull);
		if (issuingPlayer != null)
		{
			issuingPlayer.RunState.AppendToMapPointHistory((MapPointType)8, (RoomType)6, ((AbstractModel)byIdOrNull).Id);
		}
		return new CmdResult(RunManager.Instance.EnterRoom((AbstractRoom)(object)val), true, $"Opened {args[0].ToUpperInvariant()} at index {result}");
	}

	public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
	{
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Expected O, but got Unknown
		if (args.Length <= 1)
		{
			return ((AbstractConsoleCmd)this).CompleteArgument((IEnumerable<string>)ModelDb.AllAncients.Select((AncientEventModel a) => ((AbstractModel)a).Id.Entry).ToList(), Array.Empty<string>(), args.FirstOrDefault() ?? "", (CompletionType)2, (Func<string, string, bool>)null);
		}
		return new CompletionResult
		{
			Type = (CompletionType)2,
			ArgumentContext = ((AbstractConsoleCmd)this).CmdName
		};
	}
}
