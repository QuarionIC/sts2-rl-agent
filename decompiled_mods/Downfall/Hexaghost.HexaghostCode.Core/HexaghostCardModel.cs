using Downfall.DownfallCode.Abstract;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Hexaghost.HexaghostCode.Core;

public abstract class HexaghostCardModel : DownfallCardModel<Hexaghost>
{
	protected HexaghostCardModel(int cost, CardType type, CardRarity rarity, TargetType targetType, bool showInCardLibrary = true, bool autoAdd = true)
		: base(cost, type, rarity, targetType, showInCardLibrary, autoAdd)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)
	//IL_0003: Unknown result type (might be due to invalid IL or missing references)
	//IL_0004: Unknown result type (might be due to invalid IL or missing references)

}
