using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using Snecko.SneckoCode.Core;
using Snecko.SneckoCode.CustomEnums;
using Snecko.SneckoCode.Extensions;
using Snecko.SneckoCode.Interfaces;

namespace Snecko.SneckoCode.Cards.Common;

[Pool(typeof(SneckoCardPool))]
public class OtherworldlySlash : SneckoCardModel, IHasGift
{
	protected override bool ShouldGlowGoldInternal => PlayedOffClassThisTurn;

	private bool PlayedOffClassThisTurn => CombatManager.Instance.History.CardPlaysFinished.Any((CardPlayFinishedEntry e) => ((CombatHistoryEntry)e).Actor == ((CardModel)this).Owner.Creature && ((CombatHistoryEntry)e).HappenedThisTurn(((CardModel)this).CombatState) && SneckoCmd.IsOffclass(e.CardPlay.Card));

	public Gift? Gift { get; set; }

	public OtherworldlySlash()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)(object)this).WithGift(new Gift
		{
			Rarity = (CardRarity)2
		});
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(SneckoTip.Offclass));
		((ConstructedCardModel)this).WithDamage(7, 2);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (PlayedOffClassThisTurn)
		{
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 2, (string)null, (string)null, (string)null).Execute(ctx);
		}
		else
		{
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		}
	}
}
