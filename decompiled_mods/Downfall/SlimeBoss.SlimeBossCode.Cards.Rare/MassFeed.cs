using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using SlimeBoss.SlimeBossCode.Core;

namespace SlimeBoss.SlimeBossCode.Cards.Rare;

[Pool(typeof(SlimeBossCardPool))]
public class MassFeed : SlimeBossCardModel
{
	public override bool CanBeGeneratedInCombat => false;

	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Thelethargicweirdo>();

	public MassFeed()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)3)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Expected O, but got Unknown
		((ConstructedCardModel)this).WithDamage(10, 2);
		((ConstructedCardModel)this).WithVars((DynamicVar[])(object)new DynamicVar[1] { (DynamicVar)DynamicVarExtensions.WithUpgrade<MaxHpVar>(new MaxHpVar(2m), 1m) });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)6));
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (((CardModel)this).CombatState == null)
		{
			return;
		}
		HashSet<Creature> fatalEligible = ((CardModel)this).CombatState.HittableEnemies.Where((Creature e) => e.Powers.All((PowerModel p) => p.ShouldOwnerDeathTriggerFatal())).ToHashSet();
		if ((await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_bite", (string)null, "blunt_attack.mp3").Execute(ctx)).Results.SelectMany((List<DamageResult> r) => r).Any((DamageResult r) => r.WasTargetKilled && fatalEligible.Contains(r.Receiver)))
		{
			await CreatureCmd.GainMaxHp(((CardModel)this).Owner.Creature, (decimal)((DynamicVar)((CardModel)this).DynamicVars.MaxHp).IntValue);
		}
	}
}
