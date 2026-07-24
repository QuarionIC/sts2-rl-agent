using System.Threading.Tasks;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.Events;
using Champ.ChampCode.Extensions;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Relics;

[Pool(typeof(ChampRelicPool))]
public class VictoriousCrown : ChampRelicModel, IOnFinisher
{
	private CardPlay? _triggeringCardPlay;

	private bool _usedThisTurn;

	public VictoriousCrown()
		: base((RelicRarity)1)
	{
	}

	public async Task OnFinisher(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		Player owner = cardPlay.Card.Owner;
		if (!_usedThisTurn && owner == ((RelicModel)this).Owner)
		{
			((RelicModel)this).Flash();
			await CardPileCmd.Draw(ctx, 2m, owner, false);
			_triggeringCardPlay = cardPlay;
			_usedThisTurn = true;
		}
	}

	public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		if (cardPlay.Card.Owner == ((RelicModel)this).Owner && _triggeringCardPlay == cardPlay)
		{
			await ChampCmd.EnterRandomStance(choiceContext, ((RelicModel)this).Owner);
			_triggeringCardPlay = null;
			((RelicModel)this).Status = (RelicStatus)0;
		}
	}

	public override async Task BeforeHandDraw(Player player, PlayerChoiceContext ctx, ICombatState combatState)
	{
		if (player == ((RelicModel)this).Owner)
		{
			_usedThisTurn = false;
			((RelicModel)this).Status = (RelicStatus)1;
			PlayerCombatState playerCombatState = ((RelicModel)this).Owner.PlayerCombatState;
			if (playerCombatState != null && playerCombatState.TurnNumber == 1)
			{
				((RelicModel)this).Flash();
				await ChampCmd.EnterDifferentStance(ctx, player);
				ChampStanceModel stance = ((RelicModel)this).Owner.ChampStance();
				await stance.SkillBonus(ctx);
				await stance.SkillBonus(ctx);
			}
		}
	}
}
