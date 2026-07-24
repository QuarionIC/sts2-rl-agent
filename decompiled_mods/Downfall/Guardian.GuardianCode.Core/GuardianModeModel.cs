using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace Guardian.GuardianCode.Core;

public abstract class GuardianModeModel : AbstractModel
{
	private Player? _player;

	public Player Owner => _player ?? throw new InvalidOperationException("Not a mutable instance");

	protected ICombatState CombatState => Owner.Creature.CombatState ?? throw new InvalidOperationException("Combat state not initialized");

	public GuardianModeModel ToMutable(Player player)
	{
		GuardianModeModel obj = (GuardianModeModel)(object)((AbstractModel)this).MutableClone();
		obj._player = player;
		return obj;
	}

	public Task OnEnter()
	{
		return Task.CompletedTask;
	}

	public Task OnExit()
	{
		return Task.CompletedTask;
	}
}
