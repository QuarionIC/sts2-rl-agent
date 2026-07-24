using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Utils;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Extensions;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Random;

namespace Guardian.GuardianCode.Potions;

[Pool(typeof(GuardianPotionPool))]
public class QuantumElixir : GuardianPotionModel
{
	public QuantumElixir()
		: base((PotionRarity)3, (PotionUsage)1, (TargetType)1)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		WithCards(3);
		WithTip(GuardianTip.Stasis);
	}

	protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		Rng rng = ((PotionModel)this).Owner.RunState.Rng.CombatCardSelection;
		List<CardModel> cards = (from c in ((PotionModel)this).Owner.Character.CardPool.GetUnlockedCards(((PotionModel)this).Owner.UnlockState, ((PotionModel)this).Owner.RunState.CardMultiplayerConstraint)
			where (int)c.Rarity != 7
			select c).ToList();
		while (GuardianCmd.CanPutIntoStasis(((PotionModel)this).Owner, null, silent: true))
		{
			int countBefore = ((PotionModel)this).Owner.GetStasis().Count;
			List<CardModel> list = CardFactory.GetDistinctForCombat(((PotionModel)this).Owner, (IEnumerable<CardModel>)cards, ((DynamicVar)((PotionModel)this).DynamicVars.Cards).IntValue, rng).ToList();
			CardModel val = await CardSelectCmd.FromChooseACardScreen(ctx, (IReadOnlyList<CardModel>)list, ((PotionModel)this).Owner, false);
			if (val == null)
			{
				break;
			}
			await GuardianCmd.PutIntoStasis(val, ctx, (AbstractModel)(object)this);
			if (((PotionModel)this).Owner.GetStasis().Count < countBefore + 1)
			{
				break;
			}
		}
	}
}
