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
using SlimeBoss.SlimeBossCode.Extensions;

namespace SlimeBoss.SlimeBossCode.Cards.Uncommon;

[Pool(typeof(SlimeBossCardPool))]
public class HungryTackle : SlimeBossCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public HungryTackle()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003f: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithDamage(18, 4);
		((ConstructedCardModel)(object)this).WithSelfDamage(3);
		((ConstructedCardModel)(object)this).WithSlurp(1m);
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)SlimeBossTag.Tackle });
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await MyCommonActions.SelfDamage(ctx, (AbstractModel)(object)this);
		await SlimeBossCmd.Slurp((CardModel)(object)this);
	}
}
