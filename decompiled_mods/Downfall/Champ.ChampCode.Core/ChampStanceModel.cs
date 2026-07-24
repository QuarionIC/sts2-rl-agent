using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Extensions;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace Champ.ChampCode.Core;

public abstract class ChampStanceModel : AbstractModel
{
	private DynamicVarSet? _dynamicVars;

	private Player? _player;

	public int Charges;

	public DynamicVarSet DynamicVars
	{
		get
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Expected O, but got Unknown
			if (_dynamicVars != null)
			{
				return _dynamicVars;
			}
			_dynamicVars = new DynamicVarSet(CanonicalVars);
			_dynamicVars.InitializeWithOwner((AbstractModel)(object)this);
			return _dynamicVars;
		}
	}

	public IHoverTip HoverTip
	{
		get
		{
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Expected O, but got Unknown
			//IL_0068: Expected O, but got Unknown
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			LocString val = new LocString("champ_stances", TypePrefix.GetPrefix(((object)this).GetType()) + ((AbstractModel)this).Id.Entry + ".title");
			LocString val2 = new LocString("champ_stances", TypePrefix.GetPrefix(((object)this).GetType()) + ((AbstractModel)this).Id.Entry + ".description");
			DynamicVars.AddTo(val2);
			return (IHoverTip)(object)new HoverTip(val, val2, (Texture2D)null);
		}
	}

	protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

	public abstract bool HasFinisher { get; }

	public virtual string? ChargeIconPath => null;

	public Player Owner => _player ?? throw new InvalidOperationException("Not a mutable instance");

	public ICombatState CombatState => Owner.Creature.CombatState ?? throw new InvalidOperationException("Combat state not initialized");

	protected override void DeepCloneFields()
	{
		_dynamicVars = DynamicVars.Clone((AbstractModel)(object)this);
	}

	public ChampStanceModel ToMutable(Player player)
	{
		ChampStanceModel obj = (ChampStanceModel)(object)((AbstractModel)this).MutableClone();
		obj._player = player;
		return obj;
	}

	public void ResetCharges()
	{
		Charges = 3;
		ChampModel.RefreshDisplay(Owner);
	}

	public Task OnEnter(PlayerChoiceContext ctx)
	{
		ResetCharges();
		return Task.CompletedTask;
	}

	public Task OnExit(PlayerChoiceContext ctx)
	{
		Charges = 0;
		return Task.CompletedTask;
	}

	public virtual Task SkillBonus(PlayerChoiceContext ctx)
	{
		return Task.CompletedTask;
	}

	public virtual Task Finisher(PlayerChoiceContext ctx)
	{
		return Task.CompletedTask;
	}
}
