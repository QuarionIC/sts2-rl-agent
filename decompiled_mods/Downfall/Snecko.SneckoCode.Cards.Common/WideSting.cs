using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class WideSting : SneckoCardModel, IHasGift
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Magerblutooth>();

	public Gift? Gift { get; set; }

	public WideSting()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)3)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)2
		});
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
		((ConstructedCardModel)this).WithDamage(7, 3);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		foreach (CardModel item in from e in ((CardModel)this).Owner.GetHand()
			where e.IsUpgradable && SneckoCmd.IsOffclass(e)
			select e)
		{
			CardCmd.Upgrade(item, (CardPreviewStyle)1);
		}
	}
}
