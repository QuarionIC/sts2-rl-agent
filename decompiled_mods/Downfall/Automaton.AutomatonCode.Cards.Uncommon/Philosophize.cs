using System.Threading.Tasks;
using Automaton.AutomatonCode.Core;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using BaseLib.Utils;
using Downfall.DownfallCode.Artists;
using Downfall.DownfallCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Automaton.AutomatonCode.Cards.Uncommon;

[Pool(typeof(AutomatonCardPool))]
public class Philosophize : AutomatonCardModel
{
	protected override Artist Artist => Downfall.DownfallCode.Artists.Artist.Get<Opal>();

	public Philosophize()
		: base(1, (CardType)2, (CardRarity)3, (TargetType)1)
	{
		((ConstructedCardModel)this).WithKeyword((CardKeyword)2, (UpgradeType)0);
		((ConstructedCardModel)(object)this).WithPower<PhilosophizePower>(3, 2, showTooltip: false);
		((ConstructedCardModel)(object)this).WithTip<StrengthPower>();
	}

	public override async Task AfterCardDrawn(PlayerChoiceContext choiceContext, CardModel card, bool fromHandDraw)
	{
		if (((CardModel)this).CombatState != null && (object)card == this)
		{
			Creature creature = ((CardModel)this).Owner.Creature;
			decimal baseValue = DynamicVarSetExtensions.Power<PhilosophizePower>(((CardModel)this).DynamicVars).BaseValue;
			await PowerCmd.Apply<PhilosophizePower>(choiceContext, creature, baseValue * (decimal)(await ((CardModel)this).GeneratePlayCount(((CardModel)this).CombatState, (Creature)null)), ((CardModel)this).Owner.Creature, (CardModel)(object)this, false);
		}
	}
}
