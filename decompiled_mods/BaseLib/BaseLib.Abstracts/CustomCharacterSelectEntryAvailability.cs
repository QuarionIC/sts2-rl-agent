using System.Linq;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Saves;

namespace BaseLib.Abstracts;

internal static class CustomCharacterSelectEntryAvailability
{
	public static bool IsUnlocked(CharacterModel character)
	{
		return SaveManager.Instance.GenerateUnlockStateFromProgress().Characters.Contains(character);
	}
}
