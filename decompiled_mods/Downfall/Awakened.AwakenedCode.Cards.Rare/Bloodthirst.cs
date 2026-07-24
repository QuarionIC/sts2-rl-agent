using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Potions;

namespace Awakened.AwakenedCode.Cards.Rare;

[Pool(typeof(AwakenedCardPool))]
public class Bloodthirst : AwakenedCardModel
{
	public override bool CanBeGeneratedInCombat => false;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Bloodthirst()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(20, 5);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((CardKeyword)1));
		((ConstructedCardModel)(object)this).WithTip<PowerPotion>();
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)6));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Target != null)
		{
			bool shouldTriggerFatal = cardPlay.Target.Powers.All((PowerModel p) => p.ShouldOwnerDeathTriggerFatal());
			AttackCommand val = await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
			if (shouldTriggerFatal && val.Results.Any((List<DamageResult> r) => r.Any((DamageResult g) => g.WasTargetKilled)))
			{
				await PotionCmd.TryToProcure(((PotionModel)ModelDb.Potion<PowerPotion>()).ToMutable(), ((CardModel)this).Owner, -1);
				await CardCmd.Exhaust(ctx, (CardModel)(object)this, false, false);
			}
		}
	}
}
