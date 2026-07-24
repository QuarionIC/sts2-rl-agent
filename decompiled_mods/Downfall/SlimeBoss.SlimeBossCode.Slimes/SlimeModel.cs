using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using Downfall.DownfallCode.Compatibility;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using SlimeBoss.SlimeBossCode.DynamicVars;
using SlimeBoss.SlimeBossCode.Events;
using SlimeBoss.SlimeBossCode.Extensions;

namespace SlimeBoss.SlimeBossCode.Slimes;

public abstract class SlimeModel : CustomMonsterModel
{
	private DynamicVarSet? _dynamicVars;

	public override int MinInitialHp => 999999999;

	public override int MaxInitialHp => 999999999;

	public abstract SlimeType SlimeType { get; }

	public override string CustomVisualPath => ("combat/" + StringExtensions.RemovePrefix(((AbstractModel)this).Id.Entry).ToLowerInvariant() + ".tscn").SlimeScenePath();

	public override bool HasDeathSfx => false;

	public Creature PetOwner
	{
		get
		{
			Player petOwner = ((MonsterModel)this).Creature.PetOwner;
			return ((petOwner != null) ? petOwner.Creature : null) ?? throw new ArgumentNullException("PetOwner");
		}
	}

	protected virtual LocString Description => MonsterModel.L10NMonsterLookup(((AbstractModel)this).Id.Entry + ".description");

	private LocString SmartDescription
	{
		get
		{
			LocString description = Description;
			UpdatePreviewValues();
			DynamicVars.AddTo(description);
			return description;
		}
	}

	public HoverTip SlimeTip => new HoverTip(((MonsterModel)this).Title, SmartDescription, (Texture2D)null);

	public virtual IEnumerable<IHoverTip> ExtraTips => Array.Empty<IHoverTip>();

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

	protected virtual IEnumerable<DynamicVar> CanonicalVars => Array.Empty<DynamicVar>();

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		MoveState val = new MoveState("NOTHING_MOVE", (Func<IReadOnlyList<Creature>, Task>)((IReadOnlyList<Creature> _) => Task.CompletedTask), Array.Empty<AbstractIntent>());
		val.FollowUpState = (MonsterState)(object)val;
		return new MonsterMoveStateMachine((IEnumerable<MonsterState>)new _003C_003Ez__ReadOnlySingleElementList<MonsterState>((MonsterState)(object)val), (MonsterState)(object)val);
	}

	public abstract Task Command(PlayerChoiceContext ctx);

	protected virtual void UpdatePreviewValues()
	{
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		if (((AbstractModel)this).IsCanonical || ((MonsterModel)this)._creature == null)
		{
			return;
		}
		Creature creature = ((MonsterModel)this).Creature;
		if (creature == null || !creature.IsAlive)
		{
			return;
		}
		foreach (DynamicVar value in DynamicVars.Values)
		{
			DamageVar val = (DamageVar)(object)((value is DamageVar) ? value : null);
			if (val == null)
			{
				if (value is SlimeSecondaryVar slimeSecondaryVar)
				{
					((DynamicVar)slimeSecondaryVar).PreviewValue = SlimeBossHook.ModifySecondarySlimeEffects(((MonsterModel)this).CombatState, ((DynamicVar)slimeSecondaryVar).IntValue, out IEnumerable<IModifySecondarySlimeEffects> _, this);
				}
			}
			else
			{
				((DynamicVar)val).PreviewValue = CompatibilityHook.ModifyDamage(((MonsterModel)this).CombatState.RunState, ((MonsterModel)this).CombatState, null, ((MonsterModel)this).Creature, ((DynamicVar)val).BaseValue, val.Props, null, null, (ModifyDamageHookType)14, (CardPreviewMode)1, out IEnumerable<AbstractModel> _);
			}
		}
	}
}
