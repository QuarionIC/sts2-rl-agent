using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class ViciousTackle : SlimeBossCardModel, IDoubleGoopBonus
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public ViciousTackle()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)SlimeBossTag.Tackle });
		((ConstructedCardModel)this).WithDamage(17, 7);
		((ConstructedCardModel)(object)this).WithSelfDamage(3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await MyCommonActions.SelfDamage(ctx, (AbstractModel)(object)this);
	}
}
