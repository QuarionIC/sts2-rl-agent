using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;

namespace Downfall.DownfallCode.Compatibility;

public record struct CardLocationCompatiblity(Player Player, PileType PileType, CardPilePosition Position)
{
	[CompilerGenerated]
	public readonly void Deconstruct(out Player Player, out PileType PileType, out CardPilePosition Position)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected I4, but got Unknown
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected I4, but got Unknown
		Player = this.Player;
		PileType = (PileType)(int)this.PileType;
		Position = (CardPilePosition)(int)this.Position;
	}
}
