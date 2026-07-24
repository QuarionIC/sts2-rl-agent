using System;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Common;

[Pool(typeof(ChampCardPool))]
public class RisingStrike : ChampCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	private bool WasLastCardPlayedFinisher
	{
		get
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			CardPlayStartedEntry? obj = CombatManager.Instance.History.CardPlaysStarted.LastOrDefault((Func<CardPlayStartedEntry, bool>)((CardPlayStartedEntry e) => e.CardPlay.Card.Owner == ((CardModel)this).Owner && (object)e.CardPlay.Card != this));
			if (obj == null)
			{
				return false;
			}
			return obj.CardPlay.Card.Tags.Contains(ChampTag.Finisher);
		}
	}

	protected override bool ShouldGlowGoldInternal => WasLastCardPlayedFinisher;

	public RisingStrike()
		: base(1, (CardType)1, (CardRarity)2, (TargetType)2)
	{
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)5 });
		((ConstructedCardModel)this).WithDamage(8, 3);
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)1 });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(ChampTip.Finisher));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitCount((!WasLastCardPlayedFinisher) ? 1 : 2).Execute(ctx);
	}
}
