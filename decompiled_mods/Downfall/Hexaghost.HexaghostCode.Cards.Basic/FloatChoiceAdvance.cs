using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using Hexaghost.HexaghostCode.Core;
using Hexaghost.HexaghostCode.CustomEnums;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Hexaghost.HexaghostCode.Cards.Basic;

[Pool(typeof(TokenCardPool))]
public class FloatChoiceAdvance : HexaghostCardModel
{
	public override string CustomPortraitPath => (StringExtensions.RemovePrefix(((AbstractModel)ModelDb.Card<Float>()).Id.Entry).ToLowerInvariant() + ".tres").CardImageAtlasPath<Hexaghost.HexaghostCode.Core.Hexaghost>();

	public override CardPoolModel VisualCardPool => (CardPoolModel)(object)ModelDb.CardPool<HexaghostCardPool>();

	public FloatChoiceAdvance()
		: base(0, (CardType)2, (CardRarity)1, (TargetType)1)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(HexaghostKeyword.Advance));
	}
}
