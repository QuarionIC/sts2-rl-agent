using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Saves;

namespace BaseLib.Abstracts;

public abstract class DynamicBadge : Badge
{
	private readonly CustomBadge _baseBadge;

	public override BadgeRarity Rarity => _baseBadge.Rarity(base._run, base._localPlayer);

	public DynamicBadge(CustomBadge baseBadge, SerializableRun run, bool won, ulong playerId)
		: base(run, won, playerId, baseBadge.Id, baseBadge.RequiresWin, baseBadge.MultiplayerOnly)
	{
		_baseBadge = baseBadge;
	}

	public override bool IsObtained()
	{
		return _baseBadge.IsObtained(base._run, base._localPlayer);
	}
}
