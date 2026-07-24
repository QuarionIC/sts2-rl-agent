using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class SerpentineSleuth : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public SerpentineSleuth()
		: base(4, (CardType)3, (CardRarity)4, (TargetType)1)
	{
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)4,
			Type = (CardType)3
		});
		((ConstructedCardModel)(object)this).WithPower<SerpentineSleuthPower>(1, 1, showTooltip: false);
		((ConstructedCardModel)this).WithEnergy(1, 1);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.ApplySelf<SerpentineSleuthPower>(ctx, (CardModel)(object)this, false);
	}
}
