using System;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using Downfall.DownfallCode.Vfx;
using Godot;
using Hexaghost.HexaghostCode.Events;
using Hexaghost.HexaghostCode.Vfx;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace Hexaghost.HexaghostCode.Core;

public abstract class GhostflameModel : AbstractModel, ICustomModel
{
	private GhostflameModel? _canonicalInstance;

	private Player? _owner;

	public override bool ShouldReceiveCombatHooks => true;

	public abstract AbstractIntent Intent { get; }

	public bool IsActive => HexaghostCmd.GetCurrentFlame(Owner) == this;

	public bool IsIgnited { get; set; }

	private int IgnitionProgress { get; set; }

	protected abstract int IgnitionRequirement { get; }

	public LocString Title => new LocString("ghostflames", ((AbstractModel)this).Id.Entry + ".title");

	public LocString Description => new LocString("ghostflames", ((AbstractModel)this).Id.Entry + ".description");

	public abstract FireColor FireColor { get; }

	protected ICombatState CombatState => Owner.Creature.CombatState;

	public HoverTip HoverTip
	{
		get
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			HoverTip result = default(HoverTip);
			((HoverTip)(ref result))._002Ector(Title, Description, (Texture2D)null);
			((HoverTip)(ref result)).SetCanonicalModel((AbstractModel)(object)CanonicalInstance);
			return result;
		}
	}

	protected int Intensity => HexaghostHook.ModifyGhostflameEffectAdditive(Owner.Creature.CombatState, Owner, this);

	private int FlameIndex => Array.IndexOf(HexaghostCmd.GetWheel(Owner), this);

	protected Player Owner
	{
		get
		{
			((AbstractModel)this).AssertMutable();
			return _owner;
		}
		private set
		{
			((AbstractModel)this).AssertMutable();
			if (_owner != null && _owner != value)
			{
				throw new InvalidOperationException("Cannot move ghostflame " + ((AbstractModel)this).Id.Entry + " from one owner to another");
			}
			_owner = value;
		}
	}

	private GhostflameModel CanonicalInstance
	{
		get
		{
			if (((AbstractModel)this).IsMutable)
			{
				return _canonicalInstance;
			}
			return this;
		}
		set
		{
			((AbstractModel)this).AssertMutable();
			_canonicalInstance = value;
		}
	}

	protected int Repeat(GhostflameRepeatType repeatType)
	{
		return HexaghostHook.ModifyGhostflameRepeatAdditive(Owner.Creature.CombatState, Owner, repeatType, this);
	}

	public void UpdateVisuals()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (IsActive)
		{
			StatusBarHelper.SetStatus(Owner, IgnitionProgress, IgnitionRequirement, FireColor.ToColor());
		}
	}

	protected bool TryProgress(int amount = 1)
	{
		if (IsIgnited)
		{
			return false;
		}
		IgnitionProgress += amount;
		UpdateVisuals();
		return IgnitionProgress >= IgnitionRequirement;
	}

	public bool Extinguish()
	{
		if (!IsIgnited)
		{
			return false;
		}
		IsIgnited = false;
		IgnitionProgress = 0;
		UpdateVisuals();
		return true;
	}

	public abstract Task OnIgnite(PlayerChoiceContext ctx);

	public void SetIgniteProgress()
	{
		IgnitionProgress = IgnitionRequirement;
		UpdateVisuals();
	}

	protected async Task Ignite(PlayerChoiceContext ctx)
	{
		await HexaghostCmd.Ignite(ctx, Owner);
	}

	public GhostflameModel ToMutable(Player player)
	{
		((AbstractModel)this).AssertCanonical();
		GhostflameModel obj = (GhostflameModel)(object)((AbstractModel)this).MutableClone();
		obj.CanonicalInstance = this;
		obj.Owner = player;
		return obj;
	}

	protected void SpawnVfx(Creature target)
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		Vector2? val = HexaghostVisualsBridge.GetVisuals(Owner)?.GetFlameWorldPosition(FlameIndex);
		Vector2 val2 = 30f * Vector2.Up;
		Vector2? val3 = (val.HasValue ? new Vector2?(val.GetValueOrDefault() + val2) : ((Vector2?)null));
		_003F val4;
		if (!val3.HasValue)
		{
			NCreature creatureNode = Owner.Creature.GetCreatureNode();
			val4 = ((creatureNode != null) ? creatureNode.VfxSpawnPosition : Vector2.Zero);
		}
		else
		{
			val4 = val3.GetValueOrDefault();
		}
		NCreature creatureNode2 = target.GetCreatureNode();
		Vector2 target2 = ((creatureNode2 != null) ? creatureNode2.VfxSpawnPosition : Vector2.Zero);
		NFireballEffect nFireballEffect = NFireballEffect.Create((Vector2)val4, target2, FireColor.ToColor());
		NCombatRoom instance = NCombatRoom.Instance;
		if (instance != null)
		{
			GodotTreeExtensions.AddChildSafely((Node)(object)instance.CombatVfxContainer, (Node)(object)nFireballEffect);
		}
	}

	private async Task ExecuteWithContext(Func<PlayerChoiceContext, Task> action)
	{
		if (LocalContext.NetId.HasValue)
		{
			HookPlayerChoiceContext val = new HookPlayerChoiceContext(Owner, LocalContext.NetId.Value, (GameActionType)1);
			await val.AssignTaskAndWaitForPauseOrCompletion(action((PlayerChoiceContext)(object)val));
		}
	}

	public sealed override Task BeforeCardPlayed(CardPlay cardPlay)
	{
		return ExecuteWithContext((PlayerChoiceContext ctx) => BeforeCardPlayed(ctx, cardPlay));
	}

	protected virtual Task BeforeCardPlayed(PlayerChoiceContext ctx, CardPlay cardPlay)
	{
		return Task.CompletedTask;
	}

	public sealed override Task AfterEnergySpent(CardModel card, int amount)
	{
		return ExecuteWithContext((PlayerChoiceContext ctx) => AfterEnergySpent(ctx, card, amount));
	}

	protected virtual Task AfterEnergySpent(PlayerChoiceContext ctx, CardModel card, int amount)
	{
		return Task.CompletedTask;
	}
}
