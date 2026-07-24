using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Commands;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Cards.Ancient;
using SlimeBoss.SlimeBossCode.Core;
using SlimeBoss.SlimeBossCode.CustomEnums;

namespace SlimeBoss.SlimeBossCode.Cards.Basic;

[Pool(typeof(SlimeBossCardPool))]
public class Tackle : SlimeBossCardModel, ITranscendenceCard
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<HalfGoblinHankins>();

	public Tackle()
		: base(1, (CardType)1, (CardRarity)1, (TargetType)2)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected I4, but got Unknown
		((ConstructedCardModel)this).WithDamage(13, 4);
		((ConstructedCardModel)(object)this).WithSelfDamage(3);
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)(int)SlimeBossTag.Tackle });
	}

	public CardModel GetTranscendenceTransformedCard()
	{
		return (CardModel)(object)ModelDb.Card<AncientDarv>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		await MyCommonActions.SelfDamage(ctx, (AbstractModel)(object)this);
	}
}
