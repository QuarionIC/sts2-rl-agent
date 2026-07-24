using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Vfx;

namespace ActsFromThePast.Powers;

public sealed class FadingPower : CustomPowerModel
{
	public override PowerType Type => (PowerType)2;

	public override PowerStackType StackType => (PowerStackType)1;

	public override async Task BeforeSideTurnEndEarly(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (side != ((PowerModel)this).Owner.Side)
		{
			return;
		}
		if (((PowerModel)this).Amount <= 1)
		{
			if (((PowerModel)this).Owner.IsDead)
			{
				return;
			}
			((PowerModel)this).Flash();
			NCombatRoom instance = NCombatRoom.Instance;
			NCreature creatureNode = ((instance != null) ? instance.GetCreatureNode(((PowerModel)this).Owner) : null);
			if (creatureNode != null)
			{
				NCombatRoom instance2 = NCombatRoom.Instance;
				if (instance2 != null)
				{
					GodotTreeExtensions.AddChildSafely((Node)(object)instance2.CombatVfxContainer, (Node)(object)NFireSmokePuffVfx.Create(((PowerModel)this).Owner));
				}
			}
			await Cmd.Wait(0.1f, false);
			await CreatureCmd.Kill(((PowerModel)this).Owner, false);
		}
		else
		{
			((PowerModel)this).Flash();
			await PowerCmd.Decrement((PowerModel)(object)this);
		}
	}
}
