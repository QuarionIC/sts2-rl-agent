using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class BeyondArmor : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public BeyondArmor()
		: base(1, (CardType)2, (CardRarity)2, (TargetType)1)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithBlock(5, 3);
		((ConstructedCardModel)this).WithCards(2, 0);
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)2
		});
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardBlock((CardModel)(object)this, cardPlay);
		await CardPileCmd.Add(IEnumerableExtensions.TakeRandom<CardModel>(((CardModel)this).Owner.GetDraw().Where(SneckoCmd.IsOffclass), ((DynamicVar)((CardModel)this).DynamicVars.Cards).IntValue, ((CardModel)this).Owner.RunState.Rng.CombatCardSelection), (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
	}
}
