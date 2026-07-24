using MegaCrit.Sts2.Core.Entities.Players;

namespace ActsFromThePast.Cards;

public static class TheBoxTracker
{
	public static Player? FreeNextPurchasePlayer { get; set; }

	public static bool SkipNextCompletion { get; set; }

	public static bool PlayerHasBox { get; set; }

	public static bool ShowRemovalDialogue { get; set; }

	public static bool CardRemovalUsed { get; set; }
}
