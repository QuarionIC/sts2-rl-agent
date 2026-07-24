using System.Linq;
using System.Threading.Tasks;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Interfaces;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Core;

public static class ChampCardEffectHandler
{
	public static async Task DoAfterOnPlayInternal(CardModel card, PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		Player owner = card.Owner;
		ChampStanceModel champStanceModel = owner.ChampStance();
		if (card.Keywords.Contains(ChampKeyword.TriggerSkillBonus))
		{
			await champStanceModel.SkillBonus(ctx);
		}
		if (card.Tags.Contains(ChampTag.EnterBerserker))
		{
			await ChampCmd.EnterBerserkerStance(ctx, owner);
		}
		else if (card.Tags.Contains(ChampTag.EnterDefensive))
		{
			await ChampCmd.EnterDefensiveStance(ctx, owner);
		}
		if (card is IBerserkerComboCard berserkerComboCard && owner.ShouldBerserkerComboTrigger())
		{
			await berserkerComboCard.BerserkerComboEffect(ctx, cardPlay);
		}
		if (card is IDefensiveComboCard defensiveComboCard && owner.ShouldDefensiveComboTrigger())
		{
			await defensiveComboCard.DefensiveComboEffect(ctx, cardPlay);
		}
		if (card.Tags.Contains(ChampTag.Finisher) && card is IFinisherCard finisherCard)
		{
			await finisherCard.FinisherEffect(ctx, cardPlay);
		}
	}
}
