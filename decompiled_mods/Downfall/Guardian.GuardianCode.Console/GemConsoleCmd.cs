using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Downfall.DownfallCode.Extensions;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.Interfaces;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Runs;

namespace Guardian.GuardianCode.Console;

public class GemConsoleCmd : AbstractConsoleCmd
{
	public override string CmdName => "gem";

	public override string Args => "<gem-id:string> <hand-index:int>";

	public override string Description => "Add a gem to a card in hand by index (0 is leftmost). Example: gem RUBY 0";

	public override bool IsNetworked => true;

	public override CmdResult Process(Player? issuingPlayer, string[] args)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_0359: Unknown result type (might be due to invalid IL or missing references)
		if (!RunManager.Instance.IsInProgress)
		{
			return new CmdResult(false, "A run is currently not in progress!");
		}
		if (issuingPlayer == null)
		{
			return new CmdResult(false, "No player context available!");
		}
		if (args.Length < 2)
		{
			return new CmdResult(false, "Usage: gem <gem-id> <hand-index>");
		}
		string gemName = args[0].ToUpperInvariant();
		GemModel gemModel = GuardianModelDb.AllGems.FirstOrDefault((GemModel g) => ((AbstractModel)g).Id.Entry == gemName)?.ToMutable();
		if (gemModel == null)
		{
			return new CmdResult(false, "Gem '" + gemName + "' not found.");
		}
		if (!int.TryParse(args[1], out var result))
		{
			return new CmdResult(false, "Arg 2 must be hand index (int), got '" + args[1] + "'.");
		}
		CardPile pile = PileTypeExtensions.GetPile((PileType)2, issuingPlayer);
		int count = pile.Cards.Count;
		if (result < 0 || result >= count)
		{
			return new CmdResult(false, $"Invalid hand index {result}. Valid range: 0-{count - 1}.");
		}
		CardModel val = pile.Cards[result];
		if (!(val is IGemSocketCard gemSocketCard))
		{
			return new CmdResult(false, $"Card at index {result} is not a Guardian card!");
		}
		if (gemSocketCard.GemCount >= gemSocketCard.GemSlots)
		{
			return new CmdResult(false, $"Card {((AbstractModel)val).Id.Entry} already has maximum gems ({gemSocketCard.GemSlots})!");
		}
		gemSocketCard.AddGem(gemModel);
		GuardianMainFile.Logger.Info($"Added gem to card: {val.Title} ({gemSocketCard.GetHashCode()})", 1);
		Logger logger = GuardianMainFile.Logger;
		DefaultInterpolatedStringHandler defaultInterpolatedStringHandler = new DefaultInterpolatedStringHandler(25, 2);
		defaultInterpolatedStringHandler.AppendLiteral("Gem's card reference: ");
		CardModel? card = gemModel.Card;
		defaultInterpolatedStringHandler.AppendFormatted((card != null) ? card.Title : null);
		defaultInterpolatedStringHandler.AppendLiteral(" (");
		defaultInterpolatedStringHandler.AppendFormatted(((object)gemModel.Card)?.GetHashCode());
		defaultInterpolatedStringHandler.AppendLiteral(")");
		logger.Info(defaultInterpolatedStringHandler.ToStringAndClear(), 1);
		NCard obj = NCard.FindOnTable(val, (PileType?)null);
		if (obj != null)
		{
			obj.UpdateVisuals((PileType)2, (CardPreviewMode)1);
		}
		if (obj != null)
		{
			obj.ReloadOverlay();
		}
		return new CmdResult(true, $"Added gem '{((AbstractModel)gemModel).Id.Entry}' to '{val.Title}' at index {result}.");
	}

	public override CompletionResult GetArgumentCompletions(Player? player, string[] args)
	{
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Expected O, but got Unknown
		int num = args.Length;
		if (num > 1)
		{
			if (num == 2 && RunManager.Instance.IsInProgress && player != null)
			{
				int count = player.GetHand().Count;
				if (count > 0)
				{
					return ((AbstractConsoleCmd)this).CompleteArgument((IEnumerable<string>)(from i in Enumerable.Range(0, count)
						select i.ToString()).ToList(), new string[1] { args[0] }, args[1], (CompletionType)2, (Func<string, string, bool>)null);
				}
			}
			return new CompletionResult
			{
				Type = (CompletionType)2,
				ArgumentContext = ((AbstractConsoleCmd)this).CmdName
			};
		}
		List<string> list = GuardianModelDb.AllGems.Select((GemModel g) => ((AbstractModel)g).Id.Entry).ToList();
		return ((AbstractConsoleCmd)this).CompleteArgument((IEnumerable<string>)list, Array.Empty<string>(), args.FirstOrDefault() ?? "", (CompletionType)2, (Func<string, string, bool>)null);
	}
}
