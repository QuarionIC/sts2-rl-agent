using Guardian.GuardianCode.Core;

namespace Guardian.GuardianCode.Cards.Abstract;

public interface IGemCard
{
	GemModel GemModel { get; }

	GemModel CanonicalGemModel { get; }
}
