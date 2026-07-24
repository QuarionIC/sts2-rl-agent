using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using Downfall.DownfallCode.Powers;
using Hexaghost.HexaghostCode.Core;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hexaghost.HexaghostCode.Cards.Common;

[Pool(typeof(HexaghostCardPool))]
public class PhantomFireball : HexaghostCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Inmo>();

	public PhantomFireball()
		: base(0, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(4, 2);
		((ConstructedCardModel)(object)this).WithTip<SoulBurnPower>();
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		if (cardPlay.Target != null && !cardPlay.Target.IsDead && cardPlay.Target.HasPower<SoulBurnPower>())
		{
			SoulBurnPower power = cardPlay.Target.GetPower<SoulBurnPower>();
			if (power != null)
			{
				await power.Detonate(ctx, ((CardModel)this).Owner.Creature, ((CardModel)this).IsUpgraded);
			}
		}
	}
}
