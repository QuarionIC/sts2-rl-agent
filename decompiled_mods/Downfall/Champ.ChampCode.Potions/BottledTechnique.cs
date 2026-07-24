using System.Threading.Tasks;
using BaseLib.Utils;
using Champ.ChampCode.Core;
using Champ.ChampCode.CustomEnums;
using Champ.ChampCode.Extensions;
using Champ.ChampCode.Stance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace Champ.ChampCode.Potions;

[Pool(typeof(ChampPotionPool))]
public class BottledTechnique : ChampPotionModel
{
	public override bool PassesCustomUsabilityCheck
	{
		get
		{
			if (!CombatManager.Instance.IsInProgress || !(((PotionModel)this).Owner.RunState.CurrentRoom is CombatRoom))
			{
				return false;
			}
			return !(((PotionModel)this).Owner.ChampStance() is ChampNoStance);
		}
	}

	public BottledTechnique()
		: base((PotionRarity)2, (PotionUsage)1, (TargetType)1)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		WithRepeat(5);
		WithTip(ChampKeyword.TriggerSkillBonus);
		WithTip(ChampTip.Stance);
	}

	protected override async Task OnUse(PlayerChoiceContext ctx, Creature? target)
	{
		ChampStanceModel a = ChampModel.GetStanceModel(((PotionModel)this).Owner);
		for (int i = 0; i < ((DynamicVar)((PotionModel)this).DynamicVars.Repeat).IntValue; i++)
		{
			await a.SkillBonus(ctx);
		}
	}
}
