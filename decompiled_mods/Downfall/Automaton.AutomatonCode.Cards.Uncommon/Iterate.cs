using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using Automaton.AutomatonCode.CustomEnums;
using Automaton.AutomatonCode.Piles;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Iterate : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Iterate()
		: base(1, (CardType)1, (CardRarity)3, (TargetType)2)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(4, 0);
		((ConstructedCardModel)(object)this).WithRepeat(2, 1);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(AutomatonTip.Stash));
	}

	public override async Task AfterAutoPostPlayPhaseEntered(PlayerChoiceContext choiceContext, Player player)
	{
		if (((CardModel)this).Pile != null && ((CardModel)this).Pile.Type == StashPile.Stash && player == ((CardModel)this).Owner)
		{
			await CardCmd.AutoPlay(choiceContext, (CardModel)(object)this, (Creature)null, (AutoPlayType)1, false, false);
		}
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		if (cardPlay.Target != null)
		{
			await BetaMainCompatibility.FromCardCompatibility(DamageCmd.Attack(((DynamicVar)((CardModel)this).DynamicVars.Damage).BaseValue), (CardModel)(object)this, cardPlay).Targeting(cardPlay.Target).WithHitCount(((DynamicVar)((CardModel)this).DynamicVars.Repeat).IntValue)
				.WithHitFx("vfx/vfx_attack_slash", (string)null, (string)null)
				.Execute(ctx);
		}
	}
}
