using System.Runtime.InteropServices;
using MegaCrit.Sts2.Core.Localization;

namespace Downfall.DownfallCode.CustomEnums;

[StructLayout(LayoutKind.Sequential, Size = 1)]
public struct DownfallCardSelectorPrefs
{
	public static LocString ToTopSelectionPrompt => new LocString("card_selection", "DOWNFALL-TO_TOP");

	public static LocString ToHandSelectionPrompt => new LocString("card_selection", "DOWNFALL-TO_HAND");

	public static LocString ToDeckSelectionPrompt => new LocString("card_selection", "DOWNFALL-TO_DECK");

	public static LocString ToAllPlayerHandSelectionPrompt => new LocString("card_selection", "DOWNFALL-TO_OTHER_HANDS");

	public static LocString ApplySelectionPrompt => new LocString("card_selection", "DOWNFALL-TO_APPLY");

	public static LocString StasisSelectionPrompt => new LocString("card_selection", "DOWNFALL-TO_STASIS");

	public static LocString PlaySelectionPrompt => new LocString("card_selection", "DOWNFALL-TO_PLAY");

	public static LocString ConjureSelectionPrompt => new LocString("card_selection", "DOWNFALL-TO_CONJURE");

	public static LocString RetainSelectionPrompt => new LocString("card_selection", "DOWNFALL-TO_RETAIN");

	public static LocString AddEtherealSelectionPrompt => new LocString("card_selection", "DOWNFALL-ADD_ETHEREAL");
}
