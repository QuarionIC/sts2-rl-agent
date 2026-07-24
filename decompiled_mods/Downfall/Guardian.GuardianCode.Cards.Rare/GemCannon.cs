using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using Godot;
using Guardian.GuardianCode.Core;
using Guardian.GuardianCode.CustomEnums;
using Guardian.GuardianCode.Gems;
using Guardian.GuardianCode.Vfx;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Guardian.GuardianCode.Cards.Rare;

[Pool(typeof(GuardianCardPool))]
public class GemCannon : GuardianCardModel
{
	public GemCannon()
		: base(2, (CardType)1, (CardRarity)4, (TargetType)2)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		((ConstructedCardModel)this).WithDamage(16, 4);
		((ConstructedCardModel)this).WithKeyword((CardKeyword)1, (UpgradeType)0);
		((ConstructedCardModel)this).WithTip(TooltipSource.op_Implicit(GuardianKeyword.Gem));
	}

	protected override async Task OnPlayInternal(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		List<GemModel> gems = (from g in ListExtensions.StableShuffle<GemModel>(GuardianCmd.GetAllCombatGems(((CardModel)this).Owner), ((CardModel)this).Owner.RunState.Rng.Shuffle)
			orderby g is OnyxGem
			select g).ToList();
		NCombatRoom instance = NCombatRoom.Instance;
		Vector2? obj;
		if (instance == null)
		{
			obj = null;
		}
		else
		{
			NCreature creatureNode = instance.GetCreatureNode(((CardModel)this).Owner.Creature);
			obj = ((creatureNode != null) ? new Vector2?(creatureNode.VfxSpawnPosition) : ((Vector2?)null));
		}
		Vector2 from = (Vector2)(((_003F?)obj) ?? Vector2.Zero);
		NCombatRoom instance2 = NCombatRoom.Instance;
		Vector2? obj2;
		if (instance2 == null)
		{
			obj2 = null;
		}
		else
		{
			NCreature creatureNode2 = instance2.GetCreatureNode(cardPlay.Target);
			obj2 = ((creatureNode2 != null) ? new Vector2?(creatureNode2.VfxSpawnPosition) : ((Vector2?)null));
		}
		Vector2 target = (Vector2)(((_003F?)obj2) ?? Vector2.Zero);
		await CommonActions.CardAttack((CardModel)(object)this, cardPlay, 1, (string)null, (string)null, (string)null).Execute(ctx);
		for (int num = 0; num < gems.Count; num++)
		{
			NGemShootEffect nGemShootEffect = NGemShootEffect.Create(gems[num], num, from, target, gems.Count);
			NCombatRoom instance3 = NCombatRoom.Instance;
			if (instance3 != null)
			{
				GodotTreeExtensions.AddChildSafely((Node)(object)instance3.CombatVfxContainer, (Node)(object)nGemShootEffect);
			}
		}
		foreach (GemModel gem in gems)
		{
			await Cmd.Wait(0.2f, false);
			await ((CardModifier)gem).OnPlay(ctx, cardPlay);
		}
	}
}
