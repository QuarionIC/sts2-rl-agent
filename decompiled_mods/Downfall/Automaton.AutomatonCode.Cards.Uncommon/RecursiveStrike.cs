using System;
using System.Threading.Tasks;
using Automaton.AutomatonCode.Cards.Basic;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class RecursiveStrike : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public RecursiveStrike()
		: base(2, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(6, 3);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Encode));
		((ConstructedCardModel)this).WithTags((CardTag[])(object)new CardTag[1] { (CardTag)1 });
		((ConstructedCardModel)this).WithUpgradingCardTip<StrikeAutomaton>((Action<StrikeAutomaton, CardModel>)null);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 2, (string)null, (string)null, (string)null).WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null).Execute(ctx);
		ICombatState combatState = ((CardModel)this).Owner.Creature.CombatState;
		if (combatState != null)
		{
			StrikeAutomaton strikeAutomaton = combatState.CreateCard<StrikeAutomaton>(((CardModel)this).Owner);
			StrikeAutomaton strike2 = combatState.CreateCard<StrikeAutomaton>(((CardModel)this).Owner);
			if (((CardModel)this).IsUpgraded)
			{
				((CardModel)strikeAutomaton).UpgradeInternal();
				((CardModel)strike2).UpgradeInternal();
			}
			await AutomatonCmd.EncodeCard((CardModel)(object)strikeAutomaton, ctx);
			await AutomatonCmd.EncodeCard((CardModel)(object)strike2, ctx);
		}
	}
}
