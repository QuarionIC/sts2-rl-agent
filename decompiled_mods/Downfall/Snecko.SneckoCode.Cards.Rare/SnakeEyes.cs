using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;
using Snecko.SneckoCode.Powers;

namespace Snecko.SneckoCode.Cards.Rare;

[Pool(typeof(SneckoCardPool))]
public class SnakeEyes : SneckoCardModel, IHasGift
{
	public Gift? Gift { get; set; }

	public SnakeEyes()
		: base(1, (CardType)2, (CardRarity)4, (TargetType)1)
	{
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)4,
			Type = (CardType)2
		});
		((ConstructedCardModel)(object)this).WithPower<SnakeEyesPower>(1, 1, showTooltip: false);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CreatureCmd.TriggerAnim(((CardModel)this).Owner.Creature, "Cast", ((CardModel)this).Owner.Character.CastAnimDelay);
		await CommonActions.ApplySelf<SnakeEyesPower>(ctx, (CardModel)(object)this, false);
	}
}
