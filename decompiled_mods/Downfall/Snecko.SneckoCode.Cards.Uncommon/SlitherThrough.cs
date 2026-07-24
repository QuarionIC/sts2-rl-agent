using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Uncommon;

[Pool(typeof(SneckoCardPool))]
public class SlitherThrough : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public SlitherThrough()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)3
		});
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
		((ConstructedCardModel)this).WithDamage(14, 4);
		((ConstructedCardModel)this).WithEnergy(1, 0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		((CardModel)this).Owner.GetHand().Where(SneckoCmd.IsOffclass).ToList()
			.ForEach(delegate(CardModel e)
			{
				e.EnergyCost.AddThisTurn(-1, false);
			});
	}
}
