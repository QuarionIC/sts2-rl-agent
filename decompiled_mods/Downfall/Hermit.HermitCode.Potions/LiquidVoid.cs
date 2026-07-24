using System.Linq;
using System.Threading.Tasks;
using Downfall.DownfallCode.CustomEnums;
using Hermit.HermitCode.Core;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace Hermit.HermitCode.Potions;

public class LiquidVoid : HermitPotionModel
{
	public override bool CanBeGeneratedInCombat => false;

	public LiquidVoid()
		: base((PotionRarity)3, (PotionUsage)1, (TargetType)1)
	{
	}

	protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		CardSelectorPrefs val = default(CardSelectorPrefs);
		((CardSelectorPrefs)(ref val))._002Ector(DownfallCardSelectorPrefs.ToHandSelectionPrompt, 1);
		CardModel val2 = (await CardSelectCmd.FromCombatPile(ctx, PileTypeExtensions.GetPile((PileType)4, ((PotionModel)this).Owner), ((PotionModel)this).Owner, val)).FirstOrDefault();
		if (val2 != null)
		{
			val2.SetToFreeThisTurn();
			await CardPileCmd.Add(val2, (PileType)2, (CardPilePosition)1, (AbstractModel)null, false);
		}
	}
}
