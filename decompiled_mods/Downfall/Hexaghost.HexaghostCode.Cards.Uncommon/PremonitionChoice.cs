using BaseLib.Abstracts;
using BaseLib.Utils;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.CardPools;

namespace Hexaghost.HexaghostCode.Cards.Uncommon;

[Pool(typeof(TokenCardPool))]
public class PremonitionChoice : HexaghostCardModel
{
	public override CardPoolModel VisualCardPool => (CardPoolModel)(object)ModelDb.CardPool<HexaghostCardPool>();

	public override CardType Type => MyType;

	private CardType MyType { get; set; } = (CardType)2;

	public override string CustomPortraitPath => ((CustomCardModel)ModelDb.Card<Premonition>()).CustomPortraitPath;

	public PremonitionChoice()
		: base(-1, (CardType)2, (CardRarity)7, (TargetType)1)
	{
	}//IL_0002: Unknown result type (might be due to invalid IL or missing references)


	public static PremonitionChoice Create(CardType cardType, Player owner)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		PremonitionChoice premonitionChoice = owner.Creature.CombatState.CreateCard<PremonitionChoice>(owner);
		premonitionChoice.MyType = cardType;
		return premonitionChoice;
	}

	protected override void AddExtraArgsToDescription(LocString description)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		description.Add("Type", CardTypeExtensions.ToLocString(MyType));
	}
}
