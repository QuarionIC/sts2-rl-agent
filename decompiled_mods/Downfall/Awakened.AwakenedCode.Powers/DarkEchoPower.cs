using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Awakened.AwakenedCode.Core;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Vfx;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.ValueProps;

namespace Awakened.AwakenedCode.Powers;

public class DarkEchoPower : AwakenedPowerModel, IHasSecondAmount
{
	public string GetSecondAmount()
	{
		return $"{((PowerModel)this).Owner.GetPowerAmount<StrengthPower>() + 4}";
	}

	public override async Task BeforeSideTurnEnd(PlayerChoiceContext ctx, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((PowerModel)this).Owner.Side)
		{
			return;
		}
		int damageAmount = ((PowerModel)this).Owner.GetPowerAmount<StrengthPower>() + 4;
		if (damageAmount <= 0)
		{
			return;
		}
		SfxPlayer.PlaySfx("res://Awakened/audio/awakened_one_3.ogg");
		for (int i = 0; i < ((PowerModel)this).Amount; i++)
		{
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature val = ((instance != null) ? instance.GetCreatureNode(((PowerModel)this).Owner) : null);
			if (val != null)
			{
				Vector2 spawnPos = val.VfxSpawnPosition;
				NShockWaveVfx nShockWaveVfx = NShockWaveVfx.Create(spawnPos, new Color(0.1f, 0f, 0.2f, 1f));
				NCombatRoom instance2 = NCombatRoom.Instance;
				if (instance2 != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance2.CombatVfxContainer, (Node)(object)nShockWaveVfx);
				}
				await Cmd.Wait(0.1f, false);
				NShockWaveVfx nShockWaveVfx2 = NShockWaveVfx.Create(spawnPos, new Color(0.3f, 0.2f, 0.4f, 1f));
				NCombatRoom instance3 = NCombatRoom.Instance;
				if (instance3 != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance3.CombatVfxContainer, (Node)(object)nShockWaveVfx2);
				}
			}
			await Cmd.Wait(0.5f, false);
			List<Creature> source = ((PowerModel)this).CombatState.Enemies.ToList();
			foreach (Creature item in source.Where((Creature e) => e.IsAlive))
			{
				await CreatureCmd.Damage(ctx, item, (decimal)damageAmount, (ValueProp)4, ((PowerModel)this).Owner);
			}
		}
	}

	public override Task AfterPowerAmountChanged(PlayerChoiceContext ctx, PowerModel power, decimal amount, Creature? applier, CardModel? cardSource)
	{
		if (power is StrengthPower && power.Owner == ((PowerModel)this).Owner)
		{
			((PowerModel)this).InvokeDisplayAmountChanged();
		}
		return Task.CompletedTask;
	}

	public DarkEchoPower()
		: base((PowerType)1, (PowerStackType)1)
	{
	}
}
