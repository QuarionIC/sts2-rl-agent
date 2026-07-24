using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Cards.Rare;

[Pool(typeof(ChampCardPool))]
public class CheapShot : ChampCardModel
{
	public CheapShot()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		((ConstructedCardModel)this).WithDamage(5, 0);
		((ConstructedCardModel)this).WithKeywords((CardKeyword[])(object)new CardKeyword[1] { (CardKeyword)1 });
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit((StaticHoverTip)8));
		((ConstructedCardModel)this).WithCostUpgradeBy(-1);
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		ICombatState combatState = ((CardModel)this).CombatState;
		if (((combatState != null) ? combatState.Encounter : null) != null && cardPlay.Target != null)
		{
			if ((int)((CardModel)this).CombatState.Encounter.RoomType == 3)
			{
				await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 3, (string)null, (string)null, (string)null).Execute(ctx);
				return;
			}
			await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
			await CreatureCmd.Stun(cardPlay.Target, (string)null);
		}
	}
}
