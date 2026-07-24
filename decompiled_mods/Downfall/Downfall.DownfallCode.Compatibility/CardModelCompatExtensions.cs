using System;
using System.Reflection;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Downfall.DownfallCode.Compatibility;

public static class CardModelCompatExtensions
{
	private static readonly MethodInfo? DupeWithPlayer = typeof(CardModel).GetMethod("CreateDupe", new Type[1] { typeof(Player) });

	private static readonly MethodInfo? DupeNoArgs = typeof(CardModel).GetMethod("CreateDupe", Type.EmptyTypes);

	public static CardModel CreateDupeCompat(this CardModel card, Player? newOwner = null)
	{
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Expected O, but got Unknown
		if (DupeWithPlayer != null)
		{
			return (CardModel)DupeWithPlayer.Invoke(card, new object[1] { newOwner ?? card.Owner });
		}
		if (DupeNoArgs != null)
		{
			return (CardModel)DupeNoArgs.Invoke(card, null);
		}
		throw new MissingMethodException("CardModel", "CreateDupe");
	}
}
